/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;

namespace slStreamUtilsMessagePack
{
    public interface IFrameParallelOptions
    {
        FrameFormatterSerializationOptions FrameOptions { get; }
    }

    public class FrameParallelOptions : MessagePackSerializerOptions, IFrameParallelOptions
    {
        public FrameFormatterSerializationOptions FrameOptions { get; private set; }

        public FrameParallelOptions(
            int totWorkerThreads,
            MessagePackSerializerOptions baseOptions)
            : base(baseOptions)
        {
            FrameOptions = new FrameFormatterSerializationOptions(totWorkerThreads);
        }

        public FrameParallelOptions(
            FrameFormatterSerializationOptions frameOptions,
            MessagePackSerializerOptions baseOptions)
            : base(baseOptions)
        {
            FrameOptions = frameOptions;
        }

        protected override MessagePackSerializerOptions Clone() =>
            new FrameParallelOptions(FrameOptions.Clone(), this);
    }
    internal static class FrameOptionsHelper
    {
        internal static FrameFormatterSerializationOptions GetOptionParams(this MessagePackSerializerOptions options)
        {
            if (options is IFrameParallelOptions awOptions)
            {
                return awOptions.FrameOptions;
            }
            else
            {
                return FrameFormatterSerializationOptions.Default;
            }
        }

    }
}
