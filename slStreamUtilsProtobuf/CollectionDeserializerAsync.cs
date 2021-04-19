/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsProtobuf
{
    public class CollectionDeserializerAsync<T> : IDisposable
    {
        private readonly ArrayPool<byte> ap;
        private readonly FIFOWorker<Tuple<byte[], int>, Tuple<T, int>> fifow;

        public CollectionDeserializerAsync(FIFOWorkerConfig fifoConfig)
        {
            ap = ArrayPool<byte>.Shared;
            fifow = new FIFOWorker<Tuple<byte[], int>, Tuple<T, int>>(fifoConfig, HandleWorkerOutput);
            ProtoBuf.Serializer.PrepareSerializer<T>();
        }

        public async IAsyncEnumerable<Frame<T>> DeserializeAsync(Stream stream, [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                Tuple<bool, int> header = ReadHeader(stream);
                if (!header.Item1)
                    break;
                int i = header.Item2;
                byte[] buf = await BufferFromStreamAsync(stream, i, token).ConfigureAwait(false);
                foreach (var f in fifow.AddWorkItem(new Tuple<byte[], int>(buf, i), token))
                    yield return new Frame<T>(f.Item2, f.Item1);
            }
            foreach (var f in fifow.Flush(token))
                yield return new Frame<T>(f.Item2, f.Item1);
        }

        protected virtual Tuple<bool, int> ReadHeader(Stream s)
        {
            int ib = s.ReadByte();
            if (ib == -1)
                return new Tuple<bool, int>(false, 0);
            byte b = (byte)ib;
            if (b != ProtobufConsts.protoRepeatedTag1)
                throw new StreamSerializationException($"invalid proto element found: {b}, expected {ProtobufConsts.protoRepeatedTag1}");
            bool res = ProtoBuf.Serializer.TryReadLengthPrefix(s, ProtoBuf.PrefixStyle.Base128, out int availableLength);
            return new Tuple<bool, int>(res, availableLength);
        }

        private Tuple<T, int> HandleWorkerOutput(Tuple<byte[], int> tuple, CancellationToken token)
        {
            byte[] buffer = tuple.Item1;
            int availableLength = tuple.Item2;
            T res;
            try
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(buffer, 0, availableLength);
                res = ReadBody(span);
                token.ThrowIfCancellationRequested();
            }
            finally
            {
                ap.Return(buffer);
            }
            return new Tuple<T, int>(res, availableLength);
        }
        protected virtual T ReadBody(ReadOnlySpan<byte> span)
        {
            return ProtoBuf.Serializer.Deserialize<T>(span);
        }
        private async Task<byte[]> BufferFromStreamAsync(Stream stream, int length, CancellationToken token)
        {
            byte[] buf = ap.Rent(length);
            int totRead = await stream.ReadAsync(buf, 0, length, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (totRead != length)
                throw new StreamSerializationException($"Unexpected length read while deserializing body. Expected {length}, got {totRead}");
            return buf;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                fifow.Dispose();
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
