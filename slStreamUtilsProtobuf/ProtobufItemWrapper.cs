/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using slStreamUtils.MultiThreadedSerialization.Wrappers;

namespace slStreamUtils.MultiThreadedSerialization.Protobuf
{
    [ProtoContract]
    public class ProtobufItemWrapper<T> : ItemWrapper<T>
    {
        public ProtobufItemWrapper(T item, int totalBytes) : base(item, totalBytes)
        {
        }
        public ProtobufItemWrapper(T item) : base(item)
        {
        }
        public ProtobufItemWrapper() : base()
        {
        }

        [ProtoMember(1)]
        public override int l { get; set; }
        [ProtoMember(2)]
        public override T t { get; set; }
    }
}
