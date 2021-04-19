/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams
{
    internal class DelayedWriter : IWriter
    {
        internal class PendingOp
        {
            internal readonly ShadowBufferData buffer;
            internal readonly bool FlushRequested;
            private PendingOp(ShadowBufferData buffer, bool FlushRequested)
            {
                this.buffer = buffer;
                this.FlushRequested = FlushRequested;
            }
            internal static PendingOp CreateBufferWriteRequest(ShadowBufferData buffer)
            {
                return new PendingOp(buffer, false);
            }
            internal static PendingOp CreateFlushRequest()
            {
                return new PendingOp(null, true);
            }
        }

        private readonly Stream stream;
        private readonly BufferedStreamWriterConfig config;
        private readonly BlockingCollection<PendingOp> pendingOps;
        private readonly ConcurrentQueue<ShadowBufferData> availableBuffers;
        private readonly Task workerTask;
        private readonly CancellationTokenSource tokenSource;
        private readonly SemaphoreSlim writeGatekeeper;
        private readonly AsyncManualResetEvent flushBlockerEvent;

        public DelayedWriter(Stream stream, BufferedStreamWriterConfig config)
        {
            this.stream = stream;
            this.config = config;
            flushBlockerEvent = new AsyncManualResetEvent(false);
            writeGatekeeper = new SemaphoreSlim(config.TotalDelayedWriterBlocks);
            tokenSource = new CancellationTokenSource();
            pendingOps = new BlockingCollection<PendingOp>();
            availableBuffers = new ConcurrentQueue<ShadowBufferData>();
            workerTask = Task.Run(WorkerTaskEntrypoint);
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

        public void Flush()
        {
            flushBlockerEvent.Reset();
            AddFlushRequestOp();
            flushBlockerEvent.Wait(tokenSource.Token);
        }

        private void WorkerTaskEntrypoint()
        {
            var token = tokenSource.Token;
            while (!tokenSource.IsCancellationRequested)
            {
                PendingOp pendingOp;
                try
                {
                    pendingOp = pendingOps.Take(token);
                    if (pendingOp.FlushRequested)
                    {
                        stream.Flush();
                        flushBlockerEvent.Set();
                        continue;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (tokenSource.IsCancellationRequested)
                        break;
                    throw;
                }
                writeGatekeeper.Release();
                ShadowBufferData buffer = pendingOp.buffer;
                stream.Write(buffer.buffer, 0, buffer.byteCount);
                ReleaseBuffer(buffer);
            }
        }

        public async Task FlushAsync(CancellationToken token)
        {
            flushBlockerEvent.Reset();
            AddFlushRequestOp();
            await flushBlockerEvent.WaitAsync(token).ConfigureAwait(false);
        }

        public ShadowBufferData RequestBuffer()
        {
            if (availableBuffers.TryDequeue(out var res))
                return res;
            return new ShadowBufferData(config.ShadowBufferSize);
        }

        public void ReturnBufferAndWrite(ShadowBufferData sourceBuffer)
        {
            writeGatekeeper.Wait(tokenSource.Token);
            AddBufferWriteRequestOp(sourceBuffer);
        }

        public async Task ReturnBufferAndWriteAsync(ShadowBufferData sourceBuffer, CancellationToken token)
        {
            await writeGatekeeper.WaitAsync(token).ConfigureAwait(false);
            AddBufferWriteRequestOp(sourceBuffer);
        }

        private void ReleaseBuffer(ShadowBufferData buffer)
        {
            if (buffer == null)
                return;
            availableBuffers.Enqueue(buffer);
        }

        private void AddBufferWriteRequestOp(ShadowBufferData buffer)
        {
            pendingOps.Add(PendingOp.CreateBufferWriteRequest(buffer));
        }
        private void AddFlushRequestOp()
        {
            pendingOps.Add(PendingOp.CreateFlushRequest());
        }
    }
}