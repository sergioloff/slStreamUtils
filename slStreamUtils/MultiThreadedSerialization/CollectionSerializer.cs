/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils.FIFOWorker;
using slStreamUtils.MultiThreadedSerialization.Wrappers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.MultiThreadedSerialization
{
    public abstract class CollectionSerializer<T> : IDisposable, IAsyncDisposable
    {
        private readonly Stream stream;
        private bool disposedValue = false;
        private FIFOWorker<T, ArraySegment<byte>> fifow;

        public CollectionSerializer(Stream stream, FIFOWorkerConfig fifoConfig)
        {
            this.stream = stream;
            fifow = new FIFOWorker<T, ArraySegment<byte>>(fifoConfig, HandleWorkerOutputAsync);
        }

        protected abstract Task WriteBodyAsync(Stream stream, T t, CancellationToken token);
        protected abstract Task WriteHeaderAsync(Stream stream, int len, CancellationToken token);

        public async Task SerializeAsync(ItemWrapper<T> obj, CancellationToken token = default)
        {
            await foreach (var buf in fifow.AddWorkItemAsync(obj.t, token).ConfigureAwait(false))
                await BufferToStreamAsync(buf, token).ConfigureAwait(false);
        }

        public async Task FlushAsync(CancellationToken token = default)
        {
            await foreach (ArraySegment<byte> buf in fifow.FlushAsync(token).ConfigureAwait(false))
                await BufferToStreamAsync(buf, token).ConfigureAwait(false);
        }

        private async Task<ArraySegment<byte>> HandleWorkerOutputAsync(T t, CancellationToken token)
        {
            MemoryStream ms = new MemoryStream();
            await WriteBodyAsync(ms, t, token).ConfigureAwait(false);
            return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position);
        }

        private async Task BufferToStreamAsync(ArraySegment<byte> buf, CancellationToken token)
        {
            await WriteHeaderAsync(stream, buf.Count, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await stream.WriteAsync(buf.Array, 0, buf.Count, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;
            if (disposing)
            {
                FlushAsync().Wait();
                fifow.Dispose();
            }
            disposedValue = true;
            fifow = null;
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
        protected virtual async ValueTask DisposeAsyncCore()
        {
            await FlushAsync().ConfigureAwait(false);
            await fifow.DisposeAsync().ConfigureAwait(false);
        }
    }
}
