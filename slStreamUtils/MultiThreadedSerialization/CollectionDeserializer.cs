/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils.FIFOWorker;
using slStreamUtils.MultiThreadedSerialization.Wrappers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.MultiThreadedSerialization
{
    public abstract class CollectionDeserializer<T> : IDisposable
    {
        private bool disposedValue = false;
        private readonly ArrayPool<byte> ap;
        private readonly FIFOWorker<Tuple<byte[], int>, Tuple<T, int>> fifow;

        public CollectionDeserializer(FIFOWorkerConfig fifoConfig)
        {
            ap = ArrayPool<byte>.Shared;
            fifow = new FIFOWorker<Tuple<byte[], int>, Tuple<T, int>>(fifoConfig, HandleWorkerOutputAsync);
        }

        protected abstract Task<T> ReadBodyAsync(Stream s, CancellationToken token);
        protected abstract Task<Tuple<bool, int>> ReadHeaderAsync(Stream s, CancellationToken token);
        protected abstract ItemWrapper<T> GetWrapper(T t, int l);


        public async IAsyncEnumerable<ItemWrapper<T>> DeserializeAsync(Stream stream, [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                Tuple<bool, int> header = await ReadHeaderAsync(stream, token).ConfigureAwait(false);
                if (!header.Item1)
                    break;
                int i = header.Item2;
                if (i == 0)
                    break;
                byte[] buf = await BufferFromStreamAsync(stream, i, token).ConfigureAwait(false);
                await foreach (var f in fifow.AddWorkItemAsync(new Tuple<byte[], int>(buf, i), token).ConfigureAwait(false))
                    yield return GetWrapper(f.Item1, f.Item2);
            }
            await foreach (var f in fifow.FlushAsync(token).ConfigureAwait(false))
                yield return GetWrapper(f.Item1, f.Item2);
        }

        private async Task<Tuple<T, int>> HandleWorkerOutputAsync(Tuple<byte[], int> tuple, CancellationToken token)
        {
            byte[] buffer = tuple.Item1;
            int availableLength = tuple.Item2;
            T res;
            try
            {
                res = await ReadBodyAsync(new MemoryStream(buffer, 0, availableLength), token).ConfigureAwait(false);
            }
            finally
            {
                ap.Return(buffer);
            }
            return new Tuple<T, int>(res, availableLength);
        }

        private async Task<byte[]> BufferFromStreamAsync(Stream stream, int length, CancellationToken token)
        {
            byte[] buf = ap.Rent(length);
            int totRead = await stream.ReadAsync(buf, 0, length, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (totRead != length)
                throw new Exception($"Unexpected length read while deserializing body. Expected {length}, got {totRead}");
            return buf;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    fifow.Dispose();
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
