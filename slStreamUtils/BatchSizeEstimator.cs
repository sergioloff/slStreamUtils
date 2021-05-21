/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;

namespace slStreamUtils
{
    // while not thread-safe per-se, it can be used by multiple threads, producing variable and less accurate results.
    // in other words, multithreaded use will introduce small random perturbations on the result, but for most cases
    // these can be ignored.
    public class BatchSizeEstimator
    {
        public BatchSizeEstimatorConfig Config { get; private set; }
        private float alpha;
        private float EMA_FrameSize_bytes = float.MinValue;
        private float EMAm1 = 0;
        private int iters = 0;
        private int maxElements;
        private int minSamplesRequired;
        private int recomendedBatchSize;

        public BatchSizeEstimator(BatchSizeEstimatorConfig config)
        {
            Config = config;
            maxElements = Config.MaxAllowedElementsInBatch ?? int.MaxValue;
            minSamplesRequired = Config.MinSamplesRequired ?? Config.EmaInterval;
            if (Config.HintAvgFrameSize_bytes.HasValue)
            {
                EMAm1 = Config.HintAvgFrameSize_bytes.Value;
                iters = Config.EmaInterval;
                CalcEstimatedElementsInBatch();
            }
            else
            {
                iters = 0;
                EMAm1 = 0;
                recomendedBatchSize = 1;
            }
        }

        public int AverageElementSizeBytes
        {
            get
            {
                return (int)EMAm1;
            }
        }

        public int RecomendedBatchSize
        {
            get
            {
                return recomendedBatchSize;
            }
        }

        public void UpdateEstimate(float observedFrameSize_bytes)
        {
            float v = observedFrameSize_bytes;
            alpha = 1.0f - 2.0f / (1.0f + (float)Math.Min(iters + 1, Config.EmaInterval));
            EMA_FrameSize_bytes = (1 - alpha) * v + alpha * EMAm1;
            EMAm1 = EMA_FrameSize_bytes;
            CalcEstimatedElementsInBatch();
            iters++;
        }

        private void CalcEstimatedElementsInBatch()
        {
            if (iters + 1 < minSamplesRequired)
                recomendedBatchSize = 1;
            else
                recomendedBatchSize = Math.Min(maxElements, Math.Max(1, (int)(Config.DesiredBatchSize_bytes / EMA_FrameSize_bytes)));
        }
    }
}
