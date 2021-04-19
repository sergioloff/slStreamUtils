/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Moq;
using NUnit.Framework;
using slStreamUtils.Streams.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [TestFixture]
    public class BufferedStreamReaderTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private BufferedStreamReaderConfig GetConfig(int shadowBufferSize, bool preFetch = false)
        {
            return new BufferedStreamReaderConfig(totalPreFetchBlocks: preFetch ? 2 : 0, shadowBufferSize: shadowBufferSize);
        }

        [TestCase(1, 1024 * 128, 1024 * 2, 1024 * 2)]
        [TestCase(1, 1024 * 128, 1024 * 2, 1024 * 4)]
        [TestCase(1, 1024 * 128, 1024 * 4, 1024 * 2)]
        [TestCase(2, 1024 * 128, 1024 * 2, 1024 * 2)]
        [TestCase(2, 1024 * 128, 1024 * 2, 1024 * 4)]
        [TestCase(2, 1024 * 128, 1024 * 4, 1024 * 2)]
        public void ReadWithPreFetcher_CorrectBytesAreReturned(int totalPreFetchBlocks, int totBytes, int readBlockLength, int shadowBufferSize)
        {
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            MemoryStream memStream = new MemoryStream(originalBuffer);
            byte[] finalBuffer = new byte[totBytes];

            using (BufferedStreamReader s = new BufferedStreamReader(memStream, new BufferedStreamReaderConfig(
                totalPreFetchBlocks: totalPreFetchBlocks,
                shadowBufferSize: shadowBufferSize
                )))
            {
                for (int f = 0, ix = 0; f < totBytes / readBlockLength; f++, ix += readBlockLength)

                    s.Read(finalBuffer, ix, readBlockLength);
            }

            Assert.AreEqual(originalBuffer, finalBuffer);
        }

        [TestCase(1, 1024 * 128, 1024 * 2, 1024 * 2)]
        [TestCase(1, 1024 * 128, 1024 * 2, 1024 * 4)]
        [TestCase(1, 1024 * 128, 1024 * 4, 1024 * 2)]
        [TestCase(2, 1024 * 128, 1024 * 2, 1024 * 2)]
        [TestCase(2, 1024 * 128, 1024 * 2, 1024 * 4)]
        [TestCase(2, 1024 * 128, 1024 * 4, 1024 * 2)]
        public async Task ReadAsyncWithPreFetcher_CorrectBytesAreReturned(int totalPreFetchBlocks, int totBytes, int readBlockLength, int shadowBufferSize)
        {
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            MemoryStream memStream = new MemoryStream(originalBuffer);
            byte[] finalBuffer = new byte[totBytes];

            using (BufferedStreamReader s = new BufferedStreamReader(memStream, new BufferedStreamReaderConfig(
                totalPreFetchBlocks: totalPreFetchBlocks,
                shadowBufferSize: shadowBufferSize
                )))
            {
                for (int f = 0, ix = 0; f < totBytes / readBlockLength; f++, ix += readBlockLength)

                    await s.ReadAsync(finalBuffer, ix, readBlockLength);
            }

            Assert.AreEqual(originalBuffer, finalBuffer);
        }

        [Test]
        public void Dispose_PreFetcherAbortsThreads()
        {
            int totalPreFetchBlocks = 2;
            int totBytes = 1024 * 128;
            int readBlockLength = 1024 * 2;
            int shadowBufferSize = 1024 * 2;
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            MemoryStream memStream = new MemoryStream(originalBuffer);
            byte[] finalBuffer = new byte[totBytes];

            using (BufferedStreamReader s = new BufferedStreamReader(memStream, new BufferedStreamReaderConfig(
                totalPreFetchBlocks: totalPreFetchBlocks,
                shadowBufferSize: shadowBufferSize
            )))
            {
                s.Read(finalBuffer, 0, readBlockLength);
            }

            Assert.AreEqual(originalBuffer.Take(readBlockLength).ToArray(), finalBuffer.Take(readBlockLength).ToArray());
        }

        [TestCase(1, 4, 3)]
        [TestCase(2, 4, 3)]
        [TestCase(3, 4, 3)]
        [TestCase(4, 4, 3)]
        public void Read_CorrectBytesAreReturned(int shadowBufferSize, int readBlockLength, int loopCount)
        {
            int totBytes = loopCount * readBlockLength;
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            MemoryStream memStream = new MemoryStream(originalBuffer);
            byte[] finalBuffer = new byte[totBytes];

            BufferedStreamReader s = new BufferedStreamReader(memStream, GetConfig(shadowBufferSize));
            for (int f = 0, ix = 0; f < loopCount; f++, ix += readBlockLength)
                s.Read(finalBuffer, ix, readBlockLength);

            Assert.AreEqual(originalBuffer, finalBuffer);
        }

        [TestCase(1, 4, 3)]
        [TestCase(2, 4, 3)]
        [TestCase(3, 4, 3)]
        [TestCase(4, 4, 3)]
        public async Task ReadAsync_CorrectBytesAreReturned(int shadowBufferSize, int readBlockLength, int loopCount)
        {
            int totBytes = loopCount * readBlockLength;
            byte[] originalBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            MemoryStream memStream = new MemoryStream(originalBuffer);
            byte[] finalBuffer = new byte[totBytes];

            BufferedStreamReader s = new BufferedStreamReader(memStream, GetConfig(shadowBufferSize));
            for (int f = 0, ix = 0; f < loopCount; f++, ix += readBlockLength)
                await s.ReadAsync(finalBuffer, ix, readBlockLength);

            Assert.AreEqual(originalBuffer, finalBuffer);
        }

        [TestCaseSource(nameof(Seek_Throws_Cases))]
        public void Seek_Throws(SeekOrigin origin, long seekOffset)
        {
            byte[] originalBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            MemoryStream memStream = new MemoryStream(originalBuffer);

            BufferedStreamReader s = new BufferedStreamReader(memStream, GetConfig(4));

            Assert.Throws<NotSupportedException>(() => s.Seek(seekOffset, origin));
        }

        static IEnumerable<object> Seek_Throws_Cases()
        {
            foreach (var origin in new SeekOrigin[] { SeekOrigin.Begin, SeekOrigin.Current, SeekOrigin.End })
                for (long seekOffset = -1; seekOffset <= 1; seekOffset++)
                    if (!(seekOffset < 0 && origin == SeekOrigin.Begin || seekOffset > 0 && origin == SeekOrigin.End))
                        yield return new object[] { origin, seekOffset };
        }

        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(4, 1)]
        [TestCase(4, 2)]
        [TestCase(4, 4)]
        public void Dispose_InnerStreamPositionAdjusted(int shadowBufferSize, int count)
        {
            int initialOffset = 1;
            long expectedOffset = count + initialOffset;
            var stream = new MemoryStream(Enumerable.Range(1, count + initialOffset).Select(f => (byte)f).ToArray());
            stream.Position = initialOffset;
            byte[] finalBuffer = new byte[count];

            long pos;
            var cfg = GetConfig(shadowBufferSize, preFetch: true);
            using (BufferedStreamReader s = new BufferedStreamReader(stream, cfg))
            {
                s.Read(finalBuffer, 0, count);
            }
            pos = stream.Position;

            Assert.AreEqual(expectedOffset, pos);
        }

        public void Dispose_InnerStreamDoesntDispose()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);
            byte[] finalBuffer = new byte[1];

            using (BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1)))
            {
                s.Read(finalBuffer, 0, 1);
            }

            streamMock.Verify(f => f.Close(), Times.Never());
        }

        [Test]
        public void Write_Throws()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.Throws<NotSupportedException>(() => s.Write(null, 0, 0));
            Assert.Throws<NotSupportedException>(() => s.WriteByte(0));
        }

        [Test]
        public void SetLength_Throws()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.Throws<NotSupportedException>(() => s.SetLength(1L));
        }

        [Test]
        public void Length_get_Throws()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.That(() => s.Length, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void Position_get_Throws()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.That(() => s.Position, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void Position_set_Throws()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.That(() => s.Position = It.IsAny<long>(), Throws.InstanceOf<NotSupportedException>());
        }


        [Test]
        public void CanRead_ReturnsTrue()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.AreEqual(true, s.CanRead);
        }

        [Test]
        public void CanWrite_ReturnsFalse()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.AreEqual(false, s.CanWrite);
        }

        [Test]
        public void CanSeek_ReturnsFalse()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));

            Assert.AreEqual(false, s.CanSeek);
        }

        [TestCase(0, 0)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        public void Read_InnerStreamCountBufferedBytes(int shadowBufferSize, int expectedBytesRead)
        {
            int bytesRead = 0;
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((a, b, c) => bytesRead += c)
                .Returns<byte[], int, int>((a, b, c) => c);
            byte[] tmparr = new byte[3];

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(shadowBufferSize));
            s.Read(tmparr, 0, 1);
            s.Read(tmparr, 1, 1);
            s.Read(tmparr, 2, 1);

            Assert.AreEqual(expectedBytesRead, bytesRead);
            streamMock.Verify(f => f.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.Is<int>(f => f != shadowBufferSize)), Times.Never());
        }

        [TestCase(0, 0)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        public async Task ReadAsync_InnerStreamCountBufferedBytes(int shadowBufferSize, int expectedBytesRead)
        {
            int bytesRead = 0;
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<byte[], int, int, CancellationToken>((a, b, c, d) => bytesRead += c)
                .ReturnsAsync((byte[] a, int b, int c, CancellationToken d) => c);
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((a, b, c) => bytesRead += c)
                .Returns<byte[], int, int>((a, b, c) => c);
            byte[] tmparr = new byte[3];

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(shadowBufferSize));
            await s.ReadAsync(tmparr, 0, 1);
            await s.ReadAsync(tmparr, 1, 1);
            await s.ReadAsync(tmparr, 2, 1);

            Assert.AreEqual(expectedBytesRead, bytesRead);
            streamMock.Verify(f => f.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.Is<int>(f => f != shadowBufferSize), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public void Read_CheckEOF1()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => 0);
            byte[] tmparr = new byte[1];

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));
            var r1 = s.Read(tmparr, 0, 1);

            Assert.AreEqual(0, r1);
        }

        [Test]
        public async Task ReadAsync_CheckEOF1()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => 0);
            streamMock.Setup(p => p.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] a, int b, int c, CancellationToken d) => 0);
            byte[] tmparr = new byte[1];

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));
            var r1 = await s.ReadAsync(tmparr, 0, 1);

            Assert.AreEqual(0, r1);
        }

        [Test]
        public void Read_CheckEOF2()
        {
            var streamMock = new Mock<Stream>();
            int i = 1;
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => i--);
            byte[] tmparr = new byte[2];

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));
            var r1 = s.Read(tmparr, 0, 1);
            var r2 = s.Read(tmparr, 1, 1);

            Assert.AreEqual(1, r1);
            Assert.AreEqual(0, r2);
        }

        [Test]
        public async Task ReadAsync_CheckEOF2()
        {
            var streamMock = new Mock<Stream>();
            int i = 1;
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => i--);
            streamMock.Setup(p => p.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] a, int b, int c, CancellationToken d) => i--);
            byte[] tmparr = new byte[2];

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));
            var r1 = await s.ReadAsync(tmparr, 0, 1);
            var r2 = await s.ReadAsync(tmparr, 1, 1);

            Assert.AreEqual(1, r1);
            Assert.AreEqual(0, r2);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Read_ReadByteNotCalledOnInnerStream(int shadowBufferSize)
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);
            byte[] tmparr = new byte[3];

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(shadowBufferSize));
            s.Read(tmparr, 0, 1);
            s.Read(tmparr, 1, 1);
            s.Read(tmparr, 2, 1);

            streamMock.Verify(f => f.ReadByte(), Times.Never());
        }

        [Test]
        public void ReadByte_ReadByteNotCalledOnInnerStream()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));
            var b1 = s.ReadByte();

            streamMock.Verify(f => f.ReadByte(), Times.Never());
            Assert.AreNotEqual(-1, b1);
        }

        [TestCase(0, 0)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        public void ReadByte_InnerStreamCountBufferedBytes(int shadowBufferSize, int expectedBytesRead)
        {
            int bytesRead = 0;
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((a, b, c) => bytesRead += c)
                .Returns<byte[], int, int>((a, b, c) => c);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(shadowBufferSize));
            var rb1 = s.ReadByte();
            var rb2 = s.ReadByte();
            var rb3 = s.ReadByte();

            Assert.AreEqual(expectedBytesRead, bytesRead);
            streamMock.Verify(f => f.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.Is<int>(f => f != shadowBufferSize)), Times.Never());
        }

        [Test]
        public void ReadByte_CheckEOF1()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => 0);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));
            var r1 = s.ReadByte();

            Assert.AreEqual(-1, r1);
        }

        [Test]
        public void ReadByte_CheckEOF2()
        {
            var streamMock = new Mock<Stream>();
            int i = 1;
            streamMock.Setup(p => p.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((a, b, c) => i--);

            BufferedStreamReader s = new BufferedStreamReader(streamMock.Object, GetConfig(1));
            var rb1 = s.ReadByte();
            var rb2 = s.ReadByte();

            Assert.AreNotEqual(-1, rb1);
            Assert.AreEqual(-1, rb2);
        }

        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public void ReadByte_CorrectBytesAreReturned(int shadowBufferSize, int totBytes)
        {
            byte[] inputBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            int[] expectedBuffer = inputBuffer.Select(f => (int)f).Append(-1).ToArray();
            MemoryStream memStream = new MemoryStream(inputBuffer);
            int[] finalBuffer = new int[totBytes + 1];

            BufferedStreamReader s = new BufferedStreamReader(memStream, GetConfig(shadowBufferSize));
            for (int f = 0; f < totBytes + 1; f++)
                finalBuffer[f] = s.ReadByte();

            Assert.AreEqual(expectedBuffer, finalBuffer);
        }
    }
}