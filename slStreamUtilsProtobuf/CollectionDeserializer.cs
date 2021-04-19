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

namespace slStreamUtils.MultiThreadedSerialization.Protobuf
{
    public class CollectionDeserializer<T> : MultiThreadedSerialization.CollectionDeserializer<T>
    {
        private readonly byte[] b1;
        public CollectionDeserializer(FIFOWorkerConfig config)
            : base(config)
        {
            b1 = new byte[1];
            ProtoBuf.Serializer.PrepareSerializer<T>();
        }

        protected override Task<T> ReadBodyAsync(Stream s, CancellationToken token)
        {
            // Can't use DeserializeWithLengthPrefix, since we can't use the same stream instance within different threads
            return Task.FromResult(ProtoBuf.Serializer.Deserialize<T>(s));
        }

        protected override async Task<Tuple<bool, int>> ReadHeaderAsync(Stream s, CancellationToken token)
        {
            // doing an async for just 1 byte may seem petty, but even reading one byte can be slow if the underlying stream has a greedy read-ahead algo
            int ib = await s.ReadAsync(b1, 0, 1, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (ib == 0)
                return new Tuple<bool, int>(false, 0);
            byte b = b1[0];
            if (b != ProtobufConsts.protoRepeatedTag1)
                throw new Exception($"invalid proto element found: {b}, expected {ProtobufConsts.protoRepeatedTag1}");
            // TryReadLengthPrefix will likely be fast since the stream is already primed
            bool res = ProtoBuf.Serializer.TryReadLengthPrefix(s, ProtoBuf.PrefixStyle.Base128, out int availableLength);
            return new Tuple<bool, int>(res, availableLength);
        }
        protected override ItemWrapper<T> GetWrapper(T t, int l)
        {
            return new ProtobufItemWrapper<T>(t, l);
        }
    }
}
