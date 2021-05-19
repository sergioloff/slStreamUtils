/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;

namespace slStreamUtils.ObjectPoolPolicy
{
    public class ListObjectPoolPolicy<T> : IPooledObjectPolicy<List<T>>
    {
        private int batchSizeEstimate;
        public ListObjectPoolPolicy(int batchSizeEstimate)
        {
            this.batchSizeEstimate = batchSizeEstimate;
        }
        public List<T> Create()
        {
            return new List<T>(batchSizeEstimate);
        }
        public bool Return(List<T> obj)
        {
            obj.Clear();
            return true;
        }
    }
}
