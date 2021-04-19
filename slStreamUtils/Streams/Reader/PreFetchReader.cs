/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Nito.AsyncEx;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams.Reader
{
    internal class PreFetchReader : IReader
    {
        private readonly AsyncProducerConsumerQueue<ShadowBufferData> idleBuffers;
        private readonly AsyncProducerConsumerQueue<ShadowBufferData> finishedBuffers;
        private readonly Task workerTask;
        private readonly CancellationTokenSource tokenSource;
        private readonly Stream stream;
        private readonly int shadowBufferSize;
        private readonly long stopPrefetchAfterXBytes;
        private bool finished;
        private long totalBytesProcessed;

        internal PreFetchReader(Stream stream, BufferedStreamReaderConfig config)
        {
            stopPrefetchAfterXBytes = config.StopPrefetchAfterXBytes;
            shadowBufferSize = config.ShadowBufferSize;
            finished = false;
            tokenSource = new CancellationTokenSource();
            totalBytesProcessed = 0;
            this.stream = stream;
            finishedBuffers = new AsyncProducerConsumerQueue<ShadowBufferData>();
            idleBuffers = new AsyncProducerConsumerQueue<ShadowBufferData>();
            for (int f = 0; f < config.TotalPreFetchBlocks; f++)
                idleBuffers.Enqueue(new ShadowBufferData(shadowBufferSize));
            workerTask = Task.Run(WorkerThreadEntrypoint);
        }

        public void ReturnBuffer(ShadowBufferData newBuffer)
        {
            idleBuffers.Enqueue(newBuffer);
        }

        public async Task ReturnBufferAsync(ShadowBufferData newBuffer, CancellationToken token)
        {
            await idleBuffers.EnqueueAsync(newBuffer, token).ConfigureAwait(false);
        }

        public ShadowBufferData RequestNewBuffer()
        {
            if (finished)
                return null;
            if (!finishedBuffers.OutputAvailable())
            {
                finished = true;
                return null;
            }
            ShadowBufferData newBuffer = finishedBuffers.Dequeue();
            if (newBuffer.byteCount == 0)
            {
                finished = true;
                return null;
            }
            return newBuffer;
        }

        public async Task<ShadowBufferData> RequestNewBufferAsync(CancellationToken token)
        {
            if (finished)
                return null;
            if (!await finishedBuffers.OutputAvailableAsync(token).ConfigureAwait(false))
            {
                finished = true;
                return null;
            }
            ShadowBufferData newBuffer = await finishedBuffers.DequeueAsync(token).ConfigureAwait(false);
            if (newBuffer.byteCount == 0)
            {
                finished = true;
                return null;
            }
            return newBuffer;
        }

        public async Task AbortAsync()
        {
            tokenSource.Cancel();
            await workerTask.ConfigureAwait(false);
        }

        public void Abort()
        {
            tokenSource.Cancel();
            workerTask.Wait();
        }

        private unsafe void WorkerThreadEntrypoint()
        {
            try
            {
                var token = tokenSource.Token;
                while (!tokenSource.IsCancellationRequested)
                {
                    ShadowBufferData newBuffer;
                    try
                    {
                        newBuffer = idleBuffers.Dequeue(token);
                    }
                    catch (OperationCanceledException)
                    {
                        if (tokenSource.IsCancellationRequested)
                            break;
                        throw;
                    }
                    newBuffer.byteCount = shadowBufferSize;
                    long maxBytesRemaining = stopPrefetchAfterXBytes - totalBytesProcessed;
                    if (maxBytesRemaining < shadowBufferSize)
                        newBuffer.byteCount = (int)maxBytesRemaining;
                    if (newBuffer.byteCount == 0)
                    {
                        break;
                    }
                    newBuffer.byteCount = stream.Read(newBuffer.buffer, 0, newBuffer.byteCount);
                    totalBytesProcessed += newBuffer.byteCount;
                    finishedBuffers.Enqueue(newBuffer);
                }
            }
            finally
            {
                finishedBuffers.CompleteAdding();
            }
        }

    }
}
