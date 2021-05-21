/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Collections.Generic;
using System.IO;

namespace slStreamUtils.ObjectPoolPolicy
{
    public class ArrayPoolBufferWriterObjectPoolPolicy<T> : IPooledObjectPolicy<ArrayPoolBufferWriter<T>>
    {
        private int batchSizeEstimate;
        public ArrayPoolBufferWriterObjectPoolPolicy(int batchSizeEstimate)
        {
            this.batchSizeEstimate = batchSizeEstimate;
        }
        public ArrayPoolBufferWriter<T> Create()
        {
            return new ArrayPoolBufferWriter<T>(batchSizeEstimate);
        }
        public bool Return(ArrayPoolBufferWriter<T> obj)
        {
            obj.Clear();
            return true;
        }
    }
}
