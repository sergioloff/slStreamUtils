/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;

namespace slStreamUtils
{
    public sealed class MultiThreadedWorkerConfig
    {
        public int MaxConcurrentTasks { get; internal set; }
        public int MaxQueuedItems { get; internal set; }

        public MultiThreadedWorkerConfig(int totThreads, int? maxQueuedItems = null)
        {
            MaxConcurrentTasks = totThreads;
            MaxQueuedItems = maxQueuedItems == null ? Math.Max(1, totThreads * 2) : maxQueuedItems.Value;
        }
    }
}
