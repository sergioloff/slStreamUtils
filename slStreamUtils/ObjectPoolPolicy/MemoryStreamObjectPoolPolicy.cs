/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Microsoft.Extensions.ObjectPool;
using System.IO;

namespace slStreamUtils.ObjectPoolPolicy
{
    public class MemoryStreamObjectPoolPolicy : IPooledObjectPolicy<MemoryStream>
    {
        private int batchSizeEstimate;
        public MemoryStreamObjectPoolPolicy(int batchSizeEstimate)
        {
            this.batchSizeEstimate = batchSizeEstimate;
        }
        public MemoryStream Create()
        {
            return new MemoryStream(batchSizeEstimate);
        }
        public bool Return(MemoryStream obj)
        {
            obj.Position = 0;
            obj.SetLength(0);
            return true;
        }
    }
}
