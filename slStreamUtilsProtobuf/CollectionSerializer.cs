/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils.FIFOWorker;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.MultiThreadedSerialization.Protobuf
{
    public class CollectionSerializer<T> : MultiThreadedSerialization.CollectionSerializer<T>
    {
        public CollectionSerializer(Stream stream, FIFOWorkerConfig config)
            : base(stream, config)
        {
            ProtoBuf.Serializer.PrepareSerializer<T>();
        }

        protected override Task WriteBodyAsync(Stream stream, T t, CancellationToken token)
        {
            ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, t, ProtoBuf.PrefixStyle.Base128, 1);
            return Task.CompletedTask;
        }

        protected override Task WriteHeaderAsync(Stream stream, int len, CancellationToken token)
        {
            return Task.CompletedTask;
        }

    }
}
