/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils;
using System;

namespace slStreamUtilsMessagePack
{
    public class FrameFormatterSerializationOptions
    {
        public FIFOWorkerConfig FIFOWorkerConfig { get; private set; }
        public MultiThreadedWorkerConfig MthWorkerConfig { get; private set; }
        public BatchSizeEstimatorConfig BatchSizeEstimatorConfig { get; private set; }
        public bool ThrowOnUnnasignedFrameDeserialization { get; private set; }

        private static FrameFormatterSerializationOptions  _default = new FrameFormatterSerializationOptions();
        private static readonly object default_lock = new object();
        public static FrameFormatterSerializationOptions Default
        {
            get
            {
                lock (default_lock)
                    return _default;
            }
            set
            {
                lock (default_lock)
                    _default = value;
            }
        }

        public FrameFormatterSerializationOptions(
            BatchSizeEstimatorConfig batchSizeEstimatorConfig,
            FIFOWorkerConfig fIFOWorkerConfig,
            bool throwOnUnnasignedFrameDeserialization = true)
        {
            BatchSizeEstimatorConfig = batchSizeEstimatorConfig;
            FIFOWorkerConfig = fIFOWorkerConfig;
            ThrowOnUnnasignedFrameDeserialization = throwOnUnnasignedFrameDeserialization;
            MthWorkerConfig = new MultiThreadedWorkerConfig(Environment.ProcessorCount * 2);
        }

        public FrameFormatterSerializationOptions(
            BatchSizeEstimatorConfig batchSizeEstimatorConfig,
            MultiThreadedWorkerConfig mthWorkerConfig,
            bool throwOnUnnasignedFrameDeserialization = true)
        {
            BatchSizeEstimatorConfig = batchSizeEstimatorConfig;
            FIFOWorkerConfig = new FIFOWorkerConfig(Environment.ProcessorCount * 2);
            MthWorkerConfig = mthWorkerConfig;
            ThrowOnUnnasignedFrameDeserialization = throwOnUnnasignedFrameDeserialization;
        }
        public FrameFormatterSerializationOptions(
            BatchSizeEstimatorConfig batchSizeEstimatorConfig,
            FIFOWorkerConfig fIFOWorkerConfig,
            MultiThreadedWorkerConfig mthWorkerConfig,
            bool throwOnUnnasignedFrameDeserialization = true)
        {
            BatchSizeEstimatorConfig = batchSizeEstimatorConfig;
            FIFOWorkerConfig = fIFOWorkerConfig;
            MthWorkerConfig = mthWorkerConfig;
            ThrowOnUnnasignedFrameDeserialization = throwOnUnnasignedFrameDeserialization;
        }
        public FrameFormatterSerializationOptions(int totWorkerThreads, bool throwOnUnnasignedFrameDeserialization = true)
        {
            BatchSizeEstimatorConfig = new BatchSizeEstimatorConfig();
            FIFOWorkerConfig = new FIFOWorkerConfig(totWorkerThreads);
            MthWorkerConfig = new MultiThreadedWorkerConfig(totWorkerThreads);
            ThrowOnUnnasignedFrameDeserialization = throwOnUnnasignedFrameDeserialization;
        }

        public FrameFormatterSerializationOptions() : this(Environment.ProcessorCount * 2) { }

        public FrameFormatterSerializationOptions Clone()
        {
            return new FrameFormatterSerializationOptions(BatchSizeEstimatorConfig, FIFOWorkerConfig, ThrowOnUnnasignedFrameDeserialization);
        }
    }
}
