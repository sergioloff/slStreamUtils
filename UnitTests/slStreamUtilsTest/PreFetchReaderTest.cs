/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Moq;
using NUnit.Framework;
using slStreamUtils.Streams;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [TestFixture]
    public class PreFetchReaderTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private BufferedStreamReaderConfig GetConfig(int shadowBufferSize, int totalPreFetchBlocks, long stopPrefetchAfterXBytes = long.MaxValue)
        {
            return new BufferedStreamReaderConfig(totalPreFetchBlocks: totalPreFetchBlocks, shadowBufferSize: shadowBufferSize, stopPrefetchAfterXBytes: stopPrefetchAfterXBytes);
        }

        [TestCase(1, 1024 * 4, 1024 * 128)]
        [TestCase(1, 1024 * 2, 1024 * 128)]
        [TestCase(2, 1024 * 4, 1024 * 128)]
        [TestCase(2, 1024 * 2, 1024 * 128)]
        public void RequestNewBuffer_CorrectBytesAreReturned(int totalPreFetchBlocks, int shadowBufferSize, int totBytes)
        {
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            List<byte> resArray = new List<byte>();
            BufferedStreamReaderConfig config = GetConfig(shadowBufferSize, totalPreFetchBlocks);
            PreFetchReader pfh = new PreFetchReader(new MemoryStream(originalBuffer), config);

            while (true)
            {
                ShadowBufferData destBuffer = pfh.RequestNewBuffer();
                if (destBuffer == null || destBuffer.byteCount == 0)
                    break;
                resArray.AddRange(destBuffer.buffer.Take(destBuffer.byteCount));
                pfh.ReturnBuffer(destBuffer);
            }

            Assert.AreEqual(originalBuffer, resArray.ToArray());
            Assert.DoesNotThrow(pfh.Abort);
        }

        [TestCase(1, 1024 * 4, 1024 * 128)]
        [TestCase(1, 1024 * 2, 1024 * 128)]
        [TestCase(2, 1024 * 4, 1024 * 128)]
        [TestCase(2, 1024 * 2, 1024 * 128)]
        public async Task RequestNewBufferAsync_CorrectBytesAreReturned(int totalPreFetchBlocks, int shadowBufferSize, int totBytes)
        {
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            List<byte> resArray = new List<byte>();
            BufferedStreamReaderConfig config = GetConfig(shadowBufferSize, totalPreFetchBlocks);
            PreFetchReader pfh = new PreFetchReader(new MemoryStream(originalBuffer), config);

            while (true)
            {
                ShadowBufferData destBuffer = await pfh.RequestNewBufferAsync(CancellationToken.None);
                if (destBuffer == null || destBuffer.byteCount == 0)
                    break;
                resArray.AddRange(destBuffer.buffer.Take(destBuffer.byteCount));
                pfh.ReturnBuffer(destBuffer);
            }

            Assert.AreEqual(originalBuffer, resArray.ToArray());
            Assert.DoesNotThrowAsync(pfh.AbortAsync);
        }

        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        public void RequestNewBuffer_NeverRunsPastShadowBufferSize(int totalPreFetchBlocks, int shadowBufferSize)
        {
            int stopReadAt = 5;
            int curPos = 0;
            int forceStopAt = 100;
            var streamMock = new Mock<Stream>(MockBehavior.Loose);
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => { curPos += c; return c; });
            BufferedStreamReaderConfig config = GetConfig(shadowBufferSize, totalPreFetchBlocks, stopReadAt);
            List<byte> resArray = new List<byte>();
            PreFetchReader pfh = new PreFetchReader(streamMock.Object, config);

            int totRead = 0;
            int counter = 0;
            do
            {
                ShadowBufferData destBuffer = pfh.RequestNewBuffer();
                pfh.ReturnBuffer(destBuffer);
                totRead = destBuffer?.byteCount ?? 0;
                counter += totRead;
            }
            while (totRead != 0 && counter < forceStopAt);

            Assert.AreEqual(stopReadAt, counter);
            Assert.AreEqual(stopReadAt, curPos);
        }
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        public async Task RequestNewBufferAsync_NeverRunsPastShadowBufferSize(int totalPreFetchBlocks, int shadowBufferSize)
        {
            int stopReadAt = 5;
            int curPos = 0;
            int forceStopAt = 100;
            var streamMock = new Mock<Stream>(MockBehavior.Loose);
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => { curPos += c; return c; });
            BufferedStreamReaderConfig config = GetConfig(shadowBufferSize, totalPreFetchBlocks, stopReadAt);
            List<byte> resArray = new List<byte>();
            PreFetchReader pfh = new PreFetchReader(streamMock.Object, config);

            int totRead = 0;
            int counter = 0;
            do
            {
                ShadowBufferData destBuffer = await pfh.RequestNewBufferAsync(CancellationToken.None);
                pfh.ReturnBuffer(destBuffer);
                totRead = destBuffer?.byteCount ?? 0;
                counter += totRead;
            }
            while (totRead != 0 && counter < forceStopAt);

            Assert.AreEqual(stopReadAt, counter);
            Assert.AreEqual(stopReadAt, curPos);
        }

        [TestCase(1, 1024 * 4, 1024 * 128)]
        [TestCase(1, 1024 * 2, 1024 * 128)]
        [TestCase(2, 1024 * 4, 1024 * 128)]
        [TestCase(2, 1024 * 2, 1024 * 128)]
        public void Read_Returns0BytesAtEOF(int totalPreFetchBlocks, int shadowBufferSize, int totBytes)
        {
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            BufferedStreamReaderConfig config = GetConfig(shadowBufferSize, totalPreFetchBlocks);
            PreFetchReader pfh = new PreFetchReader(new MemoryStream(originalBuffer), config);

            while (true)
            {
                ShadowBufferData destBuffer = pfh.RequestNewBuffer();
                if (destBuffer == null || destBuffer.byteCount == 0)
                    break;
                pfh.ReturnBuffer(destBuffer);
            }
            ShadowBufferData lastDestBuffer = pfh.RequestNewBuffer();
            int lastRead = lastDestBuffer?.byteCount ?? 0;

            Assert.AreEqual(0, lastRead);
        }

        [TestCase(1, 1024 * 4, 1024 * 128)]
        [TestCase(1, 1024 * 2, 1024 * 128)]
        [TestCase(2, 1024 * 4, 1024 * 128)]
        [TestCase(2, 1024 * 2, 1024 * 128)]
        public async Task RequestNewBufferAsync_Returns0BytesAtEOF(int totalPreFetchBlocks, int shadowBufferSize, int totBytes)
        {
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            BufferedStreamReaderConfig config = GetConfig(shadowBufferSize, totalPreFetchBlocks);
            PreFetchReader pfh = new PreFetchReader(new MemoryStream(originalBuffer), config);

            while (true)
            {
                ShadowBufferData destBuffer = await pfh.RequestNewBufferAsync(CancellationToken.None);
                if (destBuffer == null || destBuffer.byteCount == 0)
                    break;
                pfh.ReturnBuffer(destBuffer);
            }
            ShadowBufferData lastDestBuffer = pfh.RequestNewBuffer();
            int lastRead = lastDestBuffer?.byteCount ?? 0;

            Assert.AreEqual(0, lastRead);
        }
    }
}