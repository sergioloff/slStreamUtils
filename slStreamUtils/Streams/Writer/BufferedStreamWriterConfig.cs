/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
namespace slStreamUtils.Streams
{
    public class BufferedStreamWriterConfig
    {
        public const int recomendedShadowBufferSize = 1024 * 64;

        public int ShadowBufferSize { get; set; }
        public int TotalDelayedWriterBlocks { get; set; }
        public bool UseDelayedWrite => TotalDelayedWriterBlocks > 0;


        public BufferedStreamWriterConfig(
            int totalDelayedWriterBlocks = 0,
            int shadowBufferSize = recomendedShadowBufferSize)
        {
            ShadowBufferSize = shadowBufferSize;
            TotalDelayedWriterBlocks = totalDelayedWriterBlocks;
        }
    }


}
