/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Moq;
using NUnit.Framework;
using slStreamUtils.Streams;
using slStreamUtils.Streams.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [TestFixture]
    public class BufferedStreamWriterTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private BufferedStreamWriterConfig GetConfig(int shadowBufferSize, bool delayedWriter = false)
        {
            return new BufferedStreamWriterConfig(totalDelayedWriterBlocks: delayedWriter ? 2 : 0, shadowBufferSize: shadowBufferSize);
        }

        [TestCase(2, 1, true)]
        [TestCase(4, 1, true)]
        [TestCase(6, 1, true)]
        [TestCase(2, 2, true)]
        [TestCase(4, 2, true)]
        [TestCase(6, 2, true)]
        [TestCase(2, 1, false)]
        [TestCase(4, 1, false)]
        [TestCase(6, 1, false)]
        [TestCase(2, 2, false)]
        [TestCase(4, 2, false)]
        [TestCase(6, 2, false)]
        public void RequestBuffer_BufferReleasedAfterWrite(int writeBlockLength, int repCount, bool delayedWriter)
        {
            int totBytes = repCount * writeBlockLength;
            int shadowBufferSize = 4;
            bool isRented = false;
            var config = GetConfig(shadowBufferSize, delayedWriter: delayedWriter);
            var writerMock = new Mock<IWriter>(MockBehavior.Loose);
            writerMock.Setup(p => p.RequestBuffer())
                .Returns(() =>
                {
                    if (isRented)
                        throw new InvalidOperationException();
                    isRented = true;
                    return new ShadowBufferData(shadowBufferSize);
                });
            writerMock.Setup(p => p.ReturnBufferAndWrite(It.IsAny<ShadowBufferData>()))
                .Callback<ShadowBufferData>((sourceBuffer) =>
                {
                    if (!isRented)
                        throw new InvalidOperationException();
                    isRented = false;
                });
            byte[] tmpBuf = new byte[writeBlockLength];
            byte[] destBuffer = new byte[totBytes];

            TestDelegate body = delegate ()
            {
                using (BufferedStreamWriter sw = new BufferedStreamWriter(new MemoryStream(destBuffer), config, writerMock.Object))
                {
                    for (int ix = 0, f = 0; f < repCount; ix += writeBlockLength, f++)
                        sw.Write(tmpBuf, 0, writeBlockLength);
                }
            };

            Assert.DoesNotThrow(body);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Dispose_WriterAborts(bool delayedWriter)
        {
            int shadowBufferSize = 4;
            var config = GetConfig(shadowBufferSize, delayedWriter);
            var writerMock = new Mock<IWriter>(MockBehavior.Loose);
            writerMock.Setup(p => p.RequestBuffer())
                .Returns(() => new ShadowBufferData(shadowBufferSize));

            using (BufferedStreamWriter sw = new BufferedStreamWriter(new MemoryStream(), config, writerMock.Object))
                sw.Write(new byte[] { 1, 2 }, 0, 2);

            writerMock.Verify(f => f.Abort(), Times.Once());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DisposeAsync_WriterAbortsAsync(bool delayedWriter)
        {
            int shadowBufferSize = 4;
            var config = GetConfig(shadowBufferSize, delayedWriter);
            var writerMock = new Mock<IWriter>(MockBehavior.Loose);
            writerMock.Setup(p => p.RequestBuffer())
                .Returns(() => new ShadowBufferData(shadowBufferSize));

            await using (BufferedStreamWriter sw = new BufferedStreamWriter(new MemoryStream(), config, writerMock.Object))
                await sw.WriteAsync(new byte[] { 1, 2 }, 0, 2);

            writerMock.Verify(f => f.AbortAsync(), Times.Once());
        }

        [TestCaseSource(nameof(Seek_Throws_Cases))]
        public void Seek_Throws(SeekOrigin origin, long seekOffset, bool delayedWriter)
        {
            int length = 10;
            byte[] finalBuffer = new byte[length];
            MemoryStream memStream = new MemoryStream(finalBuffer);

            BufferedStreamWriter s = new BufferedStreamWriter(memStream, GetConfig(4));

            Assert.Throws<NotSupportedException>(() => s.Seek(seekOffset, origin));
        }
        static IEnumerable<object> Seek_Throws_Cases()
        {
            foreach (bool delayedWriter in new bool[] { true, false })
                foreach (var origin in new SeekOrigin[] { SeekOrigin.Begin, SeekOrigin.Current, SeekOrigin.End })
                    for (long seekOffset = -3; seekOffset <= 3; seekOffset++)
                        if (!(seekOffset < 0 && origin == SeekOrigin.Begin || seekOffset > 0 && origin == SeekOrigin.End))
                            yield return new object[] { origin, seekOffset, delayedWriter };
        }


        [TestCase(0, 1, true)]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, true)]
        [TestCase(2, 1, true)]
        [TestCase(0, 1, false)]
        [TestCase(1, 1, false)]
        [TestCase(1, 2, false)]
        [TestCase(2, 1, false)]
        public void Dispose_PositionMatchesInnerStreams(int count, int shadowBufferSize, bool delayedWriter)
        {
            var originalBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            byte[] finalBuffer = new byte[originalBuffer.Length];
            var stream = new MemoryStream(finalBuffer);

            using (BufferedStreamWriter s = new BufferedStreamWriter(stream, GetConfig(shadowBufferSize, delayedWriter)))
            {
                s.Write(originalBuffer, 0, count);
            }

            Assert.AreEqual(count, stream.Position);
        }

        [TestCase(0, 1, true)]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, true)]
        [TestCase(2, 1, true)]
        [TestCase(0, 1, false)]
        [TestCase(1, 1, false)]
        [TestCase(1, 2, false)]
        [TestCase(2, 1, false)]
        public async Task DisposeAsync_PositionMatchesInnerStreams(int count, int shadowBufferSize, bool delayedWriter)
        {
            var originalBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            byte[] finalBuffer = new byte[originalBuffer.Length];
            var stream = new MemoryStream(finalBuffer);

            await using (BufferedStreamWriter s = new BufferedStreamWriter(stream, GetConfig(shadowBufferSize, delayedWriter)))
            {
                await s.WriteAsync(originalBuffer, 0, count);
            }

            Assert.AreEqual(count, stream.Position);
        }


        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void Dispose_InnerStreamDoesntDispose(bool canSeek, bool delayedWriter)
        {
            var streamMock = new Mock<Stream>(MockBehavior.Loose);
            streamMock.SetupGet(p => p.CanSeek)
                .Returns(canSeek);
            streamMock.SetupGet(p => p.Position)
                .Returns(0);
            streamMock.SetupGet(p => p.Length)
                .Returns(1);

            using (BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1, delayedWriter)))
            {
                s.Write(new byte[] { 1 }, 0, 1);
            }

            streamMock.Verify(f => f.Close(), Times.Never());
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public async Task Dispose_InnerStreamDoesntDisposeAsync(bool canSeek, bool delayedWriter)
        {
            var streamMock = new Mock<Stream>(MockBehavior.Loose);
            streamMock.SetupGet(p => p.CanSeek)
                .Returns(canSeek);
            streamMock.SetupGet(p => p.Position)
                .Returns(0);
            streamMock.SetupGet(p => p.Length)
                .Returns(1);

            using (BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1, delayedWriter)))
            {
                await s.WriteAsync(new byte[] { 1 }, 0, 1);
            }

            streamMock.Verify(f => f.Close(), Times.Never());
        }

        [Test]
        public void Read_Throws()
        {
            var streamMock = new Mock<Stream>(MockBehavior.Strict);

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.Throws<NotSupportedException>(() => s.Read(null, 0, 0));
            Assert.Throws<NotSupportedException>(() => s.ReadByte());
        }

        [Test]
        public void SetLength_Throws()
        {
            var streamMock = new Mock<Stream>(MockBehavior.Strict);

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.Throws<NotSupportedException>(() => s.SetLength(0L));
            Assert.Throws<NotSupportedException>(() => s.SetLength(1L));
        }

        [Test]
        public void Length_get_Throws()
        {
            var streamMock = new Mock<Stream>(MockBehavior.Strict);

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.That(() => s.Length, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void Position_get_Throws()
        {
            var streamMock = new Mock<Stream>(MockBehavior.Strict);

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.That(() => s.Position, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void Position_set_Throws()
        {
            var streamMock = new Mock<Stream>(MockBehavior.Strict);

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.That(() => s.Position = It.IsAny<long>(), Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void CanRead_ReturnsFalse()
        {
            var streamMock = new Mock<Stream>();

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.AreEqual(false, s.CanRead);
        }

        [Test]
        public void CanWrite_ReturnsTrue()
        {
            var streamMock = new Mock<Stream>();

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.AreEqual(true, s.CanWrite);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CanSeek_ReturnsFalse(bool innerCanSeek)
        {
            var streamMock = new Mock<Stream>();

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1));

            Assert.AreEqual(false, s.CanSeek);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Write_WriteByteNotCalledOnInnerStream(bool delayedWriter)
        {
            var streamMock = new Mock<Stream>();
            byte[] tmparr = new byte[1];

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1, delayedWriter));
            s.Write(tmparr, 0, 1);
            s.Write(tmparr, 0, 1);

            streamMock.Verify(f => f.WriteByte(It.IsAny<byte>()), Times.Never());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task WriteAsync_WriteByteNotCalledOnInnerStream(bool delayedWriter)
        {
            var streamMock = new Mock<Stream>();
            byte[] tmparr = new byte[1];

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1, delayedWriter));
            await s.WriteAsync(tmparr, 0, 1);
            await s.WriteAsync(tmparr, 0, 1);

            streamMock.Verify(f => f.WriteByte(It.IsAny<byte>()), Times.Never());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteByte_WriteByteNotCalledOnInnerStream(bool delayedWriter)
        {
            var streamMock = new Mock<Stream>();
            byte[] tmparr = new byte[1];

            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(1, delayedWriter));
            s.WriteByte(1);
            s.WriteByte(1);

            streamMock.Verify(f => f.WriteByte(It.IsAny<byte>()), Times.Never());
        }

        [TestCase(1, 1, false)]
        [TestCase(1, 2, false)]
        [TestCase(2, 1, false)]
        [TestCase(2, 2, false)]
        [TestCase(2, 3, false)]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, true)]
        [TestCase(2, 1, true)]
        [TestCase(2, 2, true)]
        [TestCase(2, 3, true)]
        public void WriteByte_CorrectBytesAreSaved(int shadowBufferSize, int totBytes, bool delayedWriter)
        {
            byte[] inputBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            byte[] destBuffer = new byte[totBytes];
            MemoryStream memStream = new MemoryStream(destBuffer);

            using (BufferedStreamWriter s = new BufferedStreamWriter(memStream, GetConfig(shadowBufferSize, delayedWriter)))
            {
                for (int f = 0; f < totBytes; f++)
                    s.WriteByte(inputBuffer[f]);
            }

            Assert.AreEqual(inputBuffer, destBuffer);
        }

        [TestCase(1, 1, 1, true)]
        [TestCase(1, 2, 1, true)]
        [TestCase(1, 2, 2, true)]
        [TestCase(1, 4, 2, true)]
        [TestCase(2, 1, 1, true)]
        [TestCase(2, 2, 1, true)]
        [TestCase(2, 2, 2, true)]
        [TestCase(2, 4, 2, true)]
        [TestCase(4, 4, 2, true)]
        [TestCase(1, 1, 1, false)]
        [TestCase(1, 2, 1, false)]
        [TestCase(1, 2, 2, false)]
        [TestCase(1, 4, 2, false)]
        [TestCase(2, 1, 1, false)]
        [TestCase(2, 2, 1, false)]
        [TestCase(2, 2, 2, false)]
        [TestCase(2, 4, 2, false)]
        [TestCase(4, 4, 2, false)]
        public void Write_CorrectBytesAreSaved(int shadowBufferSize, int totBytes, int writeBlockLength, bool delayedWriter)
        {
            byte[] inputBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            byte[] destBuffer = new byte[totBytes];
            MemoryStream destStream = new MemoryStream(destBuffer);

            using (BufferedStreamWriter s = new BufferedStreamWriter(destStream, GetConfig(shadowBufferSize, delayedWriter)))
            {
                for (int ix = 0; ix < totBytes; ix += writeBlockLength)
                    s.Write(inputBuffer, ix, Math.Min(totBytes - ix, writeBlockLength));
            }

            Assert.AreEqual(inputBuffer, destBuffer);
        }

        [TestCase(1, 1, 1, true)]
        [TestCase(1, 2, 1, true)]
        [TestCase(1, 2, 2, true)]
        [TestCase(1, 4, 2, true)]
        [TestCase(2, 1, 1, true)]
        [TestCase(2, 2, 1, true)]
        [TestCase(2, 2, 2, true)]
        [TestCase(2, 4, 2, true)]
        [TestCase(4, 4, 2, true)]
        [TestCase(1, 1, 1, false)]
        [TestCase(1, 2, 1, false)]
        [TestCase(1, 2, 2, false)]
        [TestCase(1, 4, 2, false)]
        [TestCase(2, 1, 1, false)]
        [TestCase(2, 2, 1, false)]
        [TestCase(2, 2, 2, false)]
        [TestCase(2, 4, 2, false)]
        [TestCase(4, 4, 2, false)]
        public void Flush_CorrectBytesAreSaved(int shadowBufferSize, int totBytes, int writeBlockLength, bool delayedWriter)
        {
            byte[] inputBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            byte[] destBuffer = new byte[totBytes];
            MemoryStream destStream = new MemoryStream(destBuffer);
            BufferedStreamWriter s = new BufferedStreamWriter(destStream, GetConfig(shadowBufferSize, delayedWriter));
            for (int ix = 0; ix < totBytes; ix += writeBlockLength)
                s.Write(inputBuffer, ix, Math.Min(totBytes - ix, writeBlockLength));

            s.Flush();

            Assert.AreEqual(inputBuffer, destBuffer);
        }

        [TestCase(1, 1, 1, true)]
        [TestCase(1, 2, 1, true)]
        [TestCase(1, 2, 2, true)]
        [TestCase(1, 4, 2, true)]
        [TestCase(2, 1, 1, true)]
        [TestCase(2, 2, 1, true)]
        [TestCase(2, 2, 2, true)]
        [TestCase(2, 4, 2, true)]
        [TestCase(4, 4, 2, true)]
        [TestCase(1, 1, 1, false)]
        [TestCase(1, 2, 1, false)]
        [TestCase(1, 2, 2, false)]
        [TestCase(1, 4, 2, false)]
        [TestCase(2, 1, 1, false)]
        [TestCase(2, 2, 1, false)]
        [TestCase(2, 2, 2, false)]
        [TestCase(2, 4, 2, false)]
        [TestCase(4, 4, 2, false)]
        public async Task WriteAsync_CorrectBytesAreSaved(int shadowBufferSize, int totBytes, int writeBlockLength, bool delayedWriter)
        {
            byte[] inputBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            byte[] destBuffer = new byte[totBytes];
            MemoryStream destStream = new MemoryStream(destBuffer);

            using (BufferedStreamWriter s = new BufferedStreamWriter(destStream, GetConfig(shadowBufferSize, delayedWriter)))
            {
                for (int ix = 0; ix < totBytes; ix += writeBlockLength)
                    await s.WriteAsync(inputBuffer, ix, Math.Min(totBytes - ix, writeBlockLength));
            }

            Assert.AreEqual(inputBuffer, destBuffer);
        }

        [TestCase(1, 1, 1, true)]
        [TestCase(1, 2, 1, true)]
        [TestCase(1, 2, 2, true)]
        [TestCase(1, 4, 2, true)]
        [TestCase(2, 1, 1, true)]
        [TestCase(2, 2, 1, true)]
        [TestCase(2, 2, 2, true)]
        [TestCase(2, 4, 2, true)]
        [TestCase(4, 4, 2, true)]
        [TestCase(1, 1, 1, false)]
        [TestCase(1, 2, 1, false)]
        [TestCase(1, 2, 2, false)]
        [TestCase(1, 4, 2, false)]
        [TestCase(2, 1, 1, false)]
        [TestCase(2, 2, 1, false)]
        [TestCase(2, 2, 2, false)]
        [TestCase(2, 4, 2, false)]
        [TestCase(4, 4, 2, false)]
        public async Task FlushAsync_CorrectBytesAreSaved(int shadowBufferSize, int totBytes, int writeBlockLength, bool delayedWriter)
        {
            byte[] inputBuffer = Enumerable.Range(1, totBytes).Select(f => (byte)f).ToArray();
            byte[] destBuffer = new byte[totBytes];
            MemoryStream destStream = new MemoryStream(destBuffer);
            BufferedStreamWriter s = new BufferedStreamWriter(destStream, GetConfig(shadowBufferSize, delayedWriter));
            for (int ix = 0; ix < totBytes; ix += writeBlockLength)
                await s.WriteAsync(inputBuffer, ix, Math.Min(totBytes - ix, writeBlockLength));

            await s.FlushAsync();

            Assert.AreEqual(inputBuffer, destBuffer);
        }

        [Test]
        public async Task WriteAsync_Cancel_ThrowsOpCancelled()
        {
            byte[] inputBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            var getBarAsyncReady1 = new TaskCompletionSource<object>();
            var getBarAsyncReady2 = new TaskCompletionSource<object>();
            var getBarAsyncContinue1 = new TaskCompletionSource<object>();
            bool triggered = false;
            var streamMock = new Mock<Stream>(MockBehavior.Strict);
            streamMock
                .Setup(p => p.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(async (byte[] buffer, int offset, int count, CancellationToken token) =>
                {
                    getBarAsyncReady1.SetResult(null);
                    await getBarAsyncContinue1.Task;
                    triggered = token.IsCancellationRequested;
                    getBarAsyncReady2.SetResult(null);
                    token.ThrowIfCancellationRequested();
                });
            CancellationTokenSource ts = new CancellationTokenSource();
            CancellationToken token = ts.Token;
            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(4));
            Task task = s.WriteAsync(inputBuffer, 0, inputBuffer.Length, token);
            await getBarAsyncReady1.Task;
            ts.Cancel();
            getBarAsyncContinue1.SetResult(null);
            await getBarAsyncReady2.Task;

            Assert.IsTrue(triggered);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        }

        [Test]
        public async Task FlushAsync_StreamFlushes()
        {
            byte[] inputBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            var streamMock = new Mock<Stream>(MockBehavior.Strict);
            streamMock
                .Setup(p => p.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] buffer, int offset, int count, CancellationToken token) => Task.CompletedTask);
            streamMock
                .Setup(p => p.FlushAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => Task.CompletedTask);
            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(4));
            await s.WriteAsync(inputBuffer, 0, inputBuffer.Length, CancellationToken.None);

            await s.FlushAsync(CancellationToken.None);

            streamMock.Verify(f => f.FlushAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task DisposeAsync_StreamFlushesAsync()
        {
            byte[] inputBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            var streamMock = new Mock<Stream>(MockBehavior.Strict);
            streamMock
                .Setup(p => p.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] buffer, int offset, int count, CancellationToken token) => Task.CompletedTask);
            streamMock
                .Setup(p => p.FlushAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => Task.CompletedTask);

            await using (BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(4)))
                await s.WriteAsync(inputBuffer, 0, inputBuffer.Length, CancellationToken.None);

            streamMock.Verify(f => f.FlushAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Dispose_StreamFlushes(bool delayedWriter)
        {
            byte[] inputBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            var streamMock = new Mock<Stream>(MockBehavior.Strict);
            streamMock
                .Setup(p => p.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
            streamMock
                .Setup(p => p.Flush());

            using (BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(4, delayedWriter)))
                s.Write(inputBuffer, 0, inputBuffer.Length);

            streamMock.Verify(f => f.Flush(), Times.Once());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Flush_StreamFlushes(bool delayedWriter)
        {
            byte[] inputBuffer = Enumerable.Range(1, 10).Select(f => (byte)f).ToArray();
            var streamMock = new Mock<Stream>(MockBehavior.Strict);
            streamMock
                .Setup(p => p.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
            streamMock
                .Setup(p => p.Flush());
            BufferedStreamWriter s = new BufferedStreamWriter(streamMock.Object, GetConfig(4, delayedWriter));
            s.Write(inputBuffer, 0, inputBuffer.Length);

            s.Flush();

            streamMock.Verify(f => f.Flush(), Times.Once());
        }

    }
}