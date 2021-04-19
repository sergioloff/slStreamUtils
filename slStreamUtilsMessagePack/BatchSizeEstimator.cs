/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;

namespace slStreamUtilsMessagePack
{
    public class BatchSizeEstimator
    {
        public BatchSizeEstimatorConfig Config { get; private set; }
        private double alpha;
        private double EMA_FrameSize_bytes = double.MinValue;
        private double EMAm1 = 0;
        private int iters = 0;
        private int maxElements;
        private int minSamplesRequired;
        private int recomendedBatchSize;
        private readonly object synchRoot;

        public BatchSizeEstimator(BatchSizeEstimatorConfig config)
        {
            Config = config;
            maxElements = Config.MaxAllowedElementsInBatch ?? int.MaxValue;
            minSamplesRequired = Config.MinSamplesRequired ?? Config.EmaInterval;
            synchRoot = new object();
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
                lock (synchRoot)
                    return (int)EMAm1;
            }
        }

        public int RecomendedBatchSize
        {
            get
            {
                lock (synchRoot)
                    return recomendedBatchSize;
            }
        }

        public void UpdateEstimate(int observedFrameSize_bytes)
        {
            lock (synchRoot)
            {
                double v = observedFrameSize_bytes;
                alpha = 1 - 2.0 / (1.0 + (double)Math.Min(iters + 1, Config.EmaInterval));
                EMA_FrameSize_bytes = (1 - alpha) * v + alpha * EMAm1;
                EMAm1 = EMA_FrameSize_bytes;
                CalcEstimatedElementsInBatch();
                iters++;
            }
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
