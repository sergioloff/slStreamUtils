/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using NUnit.Framework;
using slStreamUtils.Streams;
using slStreamUtils.Streams.Writer;
using System;
using System.IO;
using System.Linq;

namespace slStreamUtilsTest
{
    [TestFixture]
    public class DelayedWriterTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private BufferedStreamWriterConfig GetConfig(int shadowBufferSize, bool delayedWriter = false)
        {
            return new BufferedStreamWriterConfig(totalDelayedWriterBlocks: delayedWriter ? 2 : 0, shadowBufferSize: shadowBufferSize);
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        public void Write_UpdatesBuffer(int shadowBufferSize, int repCount)
        {
            int totBytes = repCount * shadowBufferSize;
            var config = GetConfig(shadowBufferSize, delayedWriter: true);
            byte[] sourceBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            byte[] destBuffer = new byte[totBytes];
            IWriter writer = new DelayedWriter(new MemoryStream(destBuffer), config);

            for (int ix = 0, f = 0; f < repCount; ix += shadowBufferSize, f++)
            {
                using (ShadowBufferData buf = writer.RequestBuffer())
                {
                    buf.byteCount = shadowBufferSize;
                    Buffer.BlockCopy(sourceBuffer, ix, buf.buffer, 0, shadowBufferSize);
                    writer.ReturnBufferAndWrite(buf);
                }
            }
            writer.Flush();

            Assert.AreEqual(sourceBuffer, destBuffer);
        }
    }
}