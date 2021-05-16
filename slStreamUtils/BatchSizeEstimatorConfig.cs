/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;

namespace slStreamUtils
{
    public class BatchSizeEstimatorConfig
    {
        public int EmaInterval { get; private set; }
        public int DesiredBatchSize_bytes { get; private set; }
        public int? HintAvgFrameSize_bytes { get; private set; }
        public int? MaxAllowedElementsInBatch { get; private set; }
        public int? MinSamplesRequired { get; private set; }

        public BatchSizeEstimatorConfig(int emaInterval = 10, int desiredBatchSize_bytes = 64 * 1024,
            int? hintAvgFrameSize_bytes = null, int? maxAllowedElementsInBatch = null, int? minSamplesRequired = null)
        {
            if (emaInterval <= 0)
                throw new ArgumentException("invalid EMA interval, must be > 0", nameof(emaInterval));
            if (desiredBatchSize_bytes <= 0)
                throw new ArgumentException("invalid desired batch size (bytes), must be > 0", nameof(desiredBatchSize_bytes));
            if (minSamplesRequired.HasValue && minSamplesRequired.Value > EmaInterval)
                throw new ArgumentException($"invalid minimum number of samples required for estimation, must be <= {nameof(EmaInterval)}", nameof(minSamplesRequired));
            EmaInterval = emaInterval;
            DesiredBatchSize_bytes = desiredBatchSize_bytes;
            HintAvgFrameSize_bytes = hintAvgFrameSize_bytes;
            MaxAllowedElementsInBatch = maxAllowedElementsInBatch;
            MinSamplesRequired = minSamplesRequired;
        }
    }



}
