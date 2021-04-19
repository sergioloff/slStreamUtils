/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsProtobuf
{
    public class CollectionSerializerAsync<T> : IDisposable, IAsyncDisposable
    {
        private readonly Stream stream;
        private FIFOWorker<T, MemoryStream> fifow;

        public CollectionSerializerAsync(Stream stream, FIFOWorkerConfig config)
        {
            this.stream = stream;
            fifow = new FIFOWorker<T, MemoryStream>(config, HandleWorkerOutput);
            ProtoBuf.Serializer.PrepareSerializer<T>();
        }

        protected virtual void WriteHeaderAndBody(Stream stream, T t, CancellationToken token)
        {
            ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, t, ProtoBuf.PrefixStyle.Base128, 1);
        }

        public async Task SerializeAsync(Frame<T> obj, CancellationToken token = default)
        {
            foreach (var ms in fifow.AddWorkItem(obj.Item, token))
                await BufferToStreamAsync(ms, token).ConfigureAwait(false);
        }

        public async Task FlushAsync(CancellationToken token = default)
        {
            foreach (var ms in fifow.Flush(token))
                await BufferToStreamAsync(ms, token).ConfigureAwait(false);
        }

        private MemoryStream HandleWorkerOutput(T t, CancellationToken token)
        {
            MemoryStream ms = new MemoryStream();
            WriteHeaderAndBody(ms, t, token);
            return ms;
        }

        private async Task BufferToStreamAsync(MemoryStream ms, CancellationToken token)
        {
            await stream.WriteAsync(ms.GetBuffer(), 0, (int)ms.Position, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FlushAsync().Wait();
                fifow.Dispose();
            }
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
            fifow.Dispose();
        }
    }
}
