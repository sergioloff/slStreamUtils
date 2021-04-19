/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams
{
    internal class SynchronousReader : IReader
    {
        private Stream stream;
        private long stopPrefetchAfterXBytes;
        private long totalBytesProcessed;
        private ShadowBufferData shadowBuffer;
        private readonly int shadowBufferSize;

        internal SynchronousReader(Stream stream, BufferedStreamReaderConfig config)
        {
            this.stream = stream;
            stopPrefetchAfterXBytes = config.StopPrefetchAfterXBytes;
            shadowBufferSize = config.ShadowBufferSize;
            totalBytesProcessed = 0;
            shadowBuffer = new ShadowBufferData(shadowBufferSize);
        }

        public void ReturnBuffer(ShadowBufferData newBuffer)
        {
        }

        public Task ReturnBufferAsync(ShadowBufferData newBuffer, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public ShadowBufferData RequestNewBuffer()
        {
            long maxBytesRemaining = stopPrefetchAfterXBytes - totalBytesProcessed;
            shadowBuffer.byteCount = shadowBufferSize;
            if (maxBytesRemaining < shadowBuffer.byteCount)
                shadowBuffer.byteCount = (int)maxBytesRemaining;
            if (shadowBuffer.byteCount == 0)
                return null;
            shadowBuffer.byteCount = stream.Read(shadowBuffer.buffer, 0, shadowBuffer.byteCount);
            totalBytesProcessed += shadowBuffer.byteCount;
            return shadowBuffer;
        }

        public async Task<ShadowBufferData> RequestNewBufferAsync(CancellationToken token)
        {
            long maxBytesRemaining = stopPrefetchAfterXBytes - totalBytesProcessed;
            shadowBuffer.byteCount = shadowBufferSize;
            if (maxBytesRemaining < shadowBuffer.byteCount)
                shadowBuffer.byteCount = (int)maxBytesRemaining;
            if (shadowBuffer.byteCount == 0)
                return null;
            shadowBuffer.byteCount = await stream.ReadAsync(shadowBuffer.buffer, 0, shadowBuffer.byteCount, token).ConfigureAwait(false);
            totalBytesProcessed += shadowBuffer.byteCount;
            return shadowBuffer;
        }

        public Task AbortAsync()
        {
            Abort();
            return Task.CompletedTask;
        }

        public void Abort()
        {
            shadowBuffer?.Dispose();
            shadowBuffer = null;
        }

    }


}
