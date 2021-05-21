/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using System.Collections.Generic;

namespace slStreamUtilsProtobuf
{
    [ProtoContract]
    public class ParallelServices_ArrayWrapper<T>
    {
        public ParallelServices_ArrayWrapper(T[] items)
        {
            Array = items;
        }

        public ParallelServices_ArrayWrapper()
        {
            Array = default;
        }

        [ProtoMember(1)]
        public T[] Array { get; set; }
    }

}
