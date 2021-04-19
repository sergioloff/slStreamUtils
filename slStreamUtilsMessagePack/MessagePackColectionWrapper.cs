/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using slStreamUtils.MultiThreadedSerialization.Wrappers;
using System;

namespace slStreamUtils.MultiThreadedSerialization.MessagePack
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class MessagePackColectionWrapper<T> : ColectionWrapper<T>
    {
        static MessagePackColectionWrapper()
        {
            if (nameof(a).Length != 1)
                throw new Exception($"length of nameof({nameof(a)}) must == 1");
        }

        public MessagePackColectionWrapper(T[] items) : base(items)
        {
        }

        public MessagePackColectionWrapper() : base()
        {
        }

        public override T[] a { get; set; }
    }
}
