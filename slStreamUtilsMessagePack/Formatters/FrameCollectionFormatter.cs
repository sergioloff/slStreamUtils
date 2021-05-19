/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack.Formatters;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.HighPerformance.Buffers;
using slStreamUtils;
using slStreamUtils.ObjectPoolPolicy;
using System;
using System.Collections.Generic;

namespace slStreamUtilsMessagePack.Formatters
{
    internal abstract partial class FrameCollectionFormatter<TFrameList, T> : IMessagePackFormatter<TFrameList> where TFrameList : IList<Frame<T>>
    {
        private DefaultObjectPool<ArrayPoolBufferWriter<byte>> objPoolBufferWriterBodies;
        private DefaultObjectPool<ArrayPoolBufferWriter<int>> objPoolBufferWriterBodyLengths;

        public abstract class ListFrameWrapper
        {
            public abstract Frame<T>[] AsFrameArray();
            public abstract TFrameList AsFrameList();
        }

        public abstract ListFrameWrapper GetTFrameListWrapper(TFrameList source);
        public abstract ListFrameWrapper GetTFrameListWrapper(int count);


        private struct BatchWithBufferWritersAndElementOffset
        {
            public BatchWithBufferWriters buffers;
            public int offset;
        }

        private struct BatchWithBufferWriters
        {
            public ArrayPoolBufferWriter<int> lengths;
            public ArrayPoolBufferWriter<byte> concatenatedBodies;
        }

        public FrameCollectionFormatter()
        {
            int defaultMaxQueuedItems = new FIFOWorkerConfig(Environment.ProcessorCount * 2).MaxQueuedItems;
            objPoolBufferWriterBodies = new DefaultObjectPool<ArrayPoolBufferWriter<byte>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<byte>(1024 * 64), defaultMaxQueuedItems);
            objPoolBufferWriterBodyLengths = new DefaultObjectPool<ArrayPoolBufferWriter<int>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<int>(1024), defaultMaxQueuedItems);
        }


    }
}
