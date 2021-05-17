/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using System.Collections.Generic;

namespace slStreamUtilsProtobuf
{
    [ProtoContract]
    public class ListWrapper<T>
    {
        public ListWrapper(List<T> items)
        {
            List = items;
        }

        public ListWrapper()
        {
            List = default;
        }

        [ProtoMember(1, IsPacked = false)]
        public List<T> List { get; set; }
    }

}
