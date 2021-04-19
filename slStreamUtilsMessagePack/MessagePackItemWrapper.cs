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
    public class MessagePackItemWrapper<T> : ItemWrapper<T>
    {
        static MessagePackItemWrapper()
        {
            if (nameof(l).Length != 1)
                throw new Exception($"length of nameof({nameof(l)}) must == 1");
            if (nameof(t).Length != 1)
                throw new Exception($"length of nameof({nameof(t)}) must == 1");
        }
        public MessagePackItemWrapper(T item, int totalBytes) : base(item, totalBytes)
        { 
        }
        public MessagePackItemWrapper(T item) : base(item)
        {
        }
        public MessagePackItemWrapper() : base()
        {
        }
        public override int l { get; set; }
        public override T t { get; set; }
    }
}
