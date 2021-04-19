/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;

namespace slStreamUtils
{
    public sealed class FIFOWorkerConfig
    {
        public int MaxConcurrentTasks { get; internal set; }
        public int MaxInputSortedQueuedItems { get; internal set; }
        public int MaxOutputSortedQueuedItems { get; internal set; }
        public int MaxOutputUnsortedQueuedItems { get; internal set; }
        public int MaxQueuedItems => MaxInputSortedQueuedItems + MaxOutputSortedQueuedItems + MaxOutputUnsortedQueuedItems;

        public FIFOWorkerConfig(int maxConcurrentTasks, int? maxInputQueuedItems = null, int? maxOutputQueuedItems = null, int? maxOutputUnsortedQueuedItems = null)
        {
            MaxConcurrentTasks = maxConcurrentTasks;
            MaxInputSortedQueuedItems = maxInputQueuedItems == null ? Math.Max(1, maxConcurrentTasks * 2) : maxInputQueuedItems.Value;
            MaxOutputSortedQueuedItems = maxOutputQueuedItems == null ? Math.Max(1, maxConcurrentTasks * 2) : maxOutputQueuedItems.Value;
            MaxOutputUnsortedQueuedItems = maxOutputUnsortedQueuedItems == null ? MaxOutputSortedQueuedItems : maxOutputUnsortedQueuedItems.Value;

            if (MaxInputSortedQueuedItems <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxInputSortedQueuedItems));
            if (MaxOutputSortedQueuedItems <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxOutputSortedQueuedItems));
            if (MaxOutputUnsortedQueuedItems <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxOutputUnsortedQueuedItems));
            if (this.MaxConcurrentTasks < 1)
                throw new ArgumentOutOfRangeException(nameof(FIFOWorkerConfig.MaxConcurrentTasks));
            if (this.MaxConcurrentTasks > MaxInputSortedQueuedItems)
                throw new ArgumentOutOfRangeException(nameof(FIFOWorkerConfig.MaxConcurrentTasks), $"{nameof(FIFOWorkerConfig.MaxConcurrentTasks)} mustn't exceed {nameof(MaxInputSortedQueuedItems)}");
            if (this.MaxConcurrentTasks > MaxOutputSortedQueuedItems)
                throw new ArgumentOutOfRangeException(nameof(FIFOWorkerConfig.MaxConcurrentTasks), $"{nameof(FIFOWorkerConfig.MaxConcurrentTasks)} mustn't exceed {nameof(MaxOutputSortedQueuedItems)}");
            if (this.MaxConcurrentTasks > MaxOutputUnsortedQueuedItems)
                throw new ArgumentOutOfRangeException(nameof(FIFOWorkerConfig.MaxConcurrentTasks), $"{nameof(FIFOWorkerConfig.MaxConcurrentTasks)} mustn't exceed {nameof(MaxOutputUnsortedQueuedItems)}");
        }
    }
}
