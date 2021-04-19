/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using slStreamUtils.MultiThreadedSerialization.Wrappers;

namespace slStreamUtils.MultiThreadedSerialization.Protobuf
{
    [ProtoContract]
    public class ProtobufColectionWrapper<T> : ColectionWrapper<T>
    {
        public ProtobufColectionWrapper(T[] items) : base(items)
        {
        }

        public ProtobufColectionWrapper() : base()
        {
        }

        [ProtoMember(1)]
        public override T[] a { get; set; }
    }
}
