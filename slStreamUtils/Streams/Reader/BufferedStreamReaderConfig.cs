/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
namespace slStreamUtils.Streams
{
    public class BufferedStreamReaderConfig
    {
        public const int recomendedShadowBufferSize = 81920;

        public int ShadowBufferSize { get; internal set; }
        public int TotalPreFetchBlocks { get; internal set; }
        public long StopPrefetchAfterXBytes { get; internal set; }
        public int ReaderAbortTimeoutMs { get; internal set; }
        public int ShadowFufferInitializationTimeout { get; internal set; }
        public bool UsePreFetch => TotalPreFetchBlocks > 0;


        public BufferedStreamReaderConfig(
            int totalPreFetchBlocks = 0,
            int shadowBufferSize = recomendedShadowBufferSize,
            long? stopPrefetchAfterXBytes = null,
            int readerAbortTimeoutMs = 5000,
            int shadowFufferInitializationTimeout = 5000)
        {
            ReaderAbortTimeoutMs = readerAbortTimeoutMs;
            ShadowBufferSize = shadowBufferSize;
            TotalPreFetchBlocks = totalPreFetchBlocks;
            StopPrefetchAfterXBytes = stopPrefetchAfterXBytes.HasValue ? stopPrefetchAfterXBytes.Value : long.MaxValue;
            ShadowFufferInitializationTimeout = shadowFufferInitializationTimeout;
        }
    }


}
