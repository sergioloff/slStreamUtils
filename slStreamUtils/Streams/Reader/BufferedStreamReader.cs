/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams.Reader
{
    public class BufferedStreamReader : Stream, IDisposable
    {
        private ShadowBufferData shadowBuffer;
        private readonly BufferedStreamReaderConfig config;
        private readonly Stream stream;
        private IReader reader;
        private readonly long innerStreamStartPos;
        private int currentShadowBufferSize;
        private int currShadowBufferIx;
        private long totBytesRead;
        private bool disposed = false;

        public BufferedStreamReader(Stream stream)
            : this(stream, new BufferedStreamReaderConfig())
        {
        }

        public BufferedStreamReader(Stream stream, BufferedStreamReaderConfig config)
        {
            this.stream = stream;
            this.config = config;
            innerStreamStartPos = stream.CanSeek ? stream.Position : 0;
            currentShadowBufferSize = config.ShadowBufferSize;
            totBytesRead = 0;
            if (config.UsePreFetch)
            {
                reader = new PreFetchReader(stream, config);
            }
            else
            {
                reader = new SynchronousReader(stream, config);
            }
            shadowBuffer = null;
            FillShadowBuffer();
        }

        public override int ReadByte()
        {
            if (IsEOF)
                return -1;
            int res = shadowBuffer.buffer[currShadowBufferIx++];
            if (currShadowBufferIx == currentShadowBufferSize)
            {
                FillShadowBuffer();
            }
            return res;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (ReadPartialBufferHotPath(buffer, offset, count, out int bytesRead))
                return bytesRead;
            int totBytesCopied = 0;
            int newIx = currShadowBufferIx + count;
            while (newIx >= currentShadowBufferSize)
            {
                ReadWithBufferRefill(buffer, ref offset, ref totBytesCopied, ref newIx);
                await FillShadowBufferAsync(token).ConfigureAwait(false);
                if (IsEOF)
                    return totBytesCopied;
            }
            return ReadPartialBuffer(buffer, offset, totBytesCopied, newIx);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (ReadPartialBufferHotPath(buffer, offset, count, out int bytesRead))
                return bytesRead;
            int totBytesCopied = 0;
            int newIx = currShadowBufferIx + count;
            while (newIx >= currentShadowBufferSize)
            {
                ReadWithBufferRefill(buffer, ref offset, ref totBytesCopied, ref newIx);
                FillShadowBuffer();
                if (IsEOF)
                    return totBytesCopied;
            }
            return ReadPartialBuffer(buffer, offset, totBytesCopied, newIx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReadPartialBufferHotPath(byte[] buffer, int offset, int count, out int bytesRead)
        {
            if (count < currentShadowBufferSize - currShadowBufferIx)
            {
                // hot path
                CopyFromShadow(buffer, offset, count);
                currShadowBufferIx += count;
                bytesRead = count;
                return true;
            }
            bytesRead = 0;
            return IsEOF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadWithBufferRefill(byte[] buffer, ref int offset, ref int totBytesCopied, ref int newIx)
        {
            int availableLength = currentShadowBufferSize - currShadowBufferIx;
            CopyFromShadowBulk(buffer, offset, availableLength);
            totBytesCopied += availableLength;
            newIx -= currentShadowBufferSize;
            offset += availableLength;
            currShadowBufferIx = currentShadowBufferSize;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReadPartialBuffer(byte[] buffer, int offset, int totBytesCopied, int newIx)
        {
            int partialBlockLength = newIx - currShadowBufferIx;
            if (partialBlockLength > 0)
            {
                CopyFromShadowBulk(buffer, offset, partialBlockLength);
                totBytesCopied += partialBlockLength;
                currShadowBufferIx = newIx;
            }

            return totBytesCopied;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] CopyFromShadow(byte[] buffer, int offset, int count)
        {
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    Sparrow.Memory.Copy(bufferPtr + offset, shadowBuffer.buffer_ptr + currShadowBufferIx, count);
                }
            }
            return buffer;
        }
        private byte[] CopyFromShadowBulk(byte[] buffer, int offset, int count)
        {
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    Sparrow.Memory.BulkCopy(bufferPtr + offset, shadowBuffer.buffer_ptr + currShadowBufferIx, count);
                }
            }
            return buffer;
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanSeek => false;

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        private bool IsEOF
        {
            get
            {
                return currentShadowBufferSize == 0;
            }
        }

        private async Task FillShadowBufferAsync(CancellationToken token)
        {
            totBytesRead += currShadowBufferIx;
            currShadowBufferIx = 0;
            if (shadowBuffer != null)
                await reader.ReturnBufferAsync(shadowBuffer, token).ConfigureAwait(false);
            shadowBuffer = await reader.RequestNewBufferAsync(token).ConfigureAwait(false);
            currentShadowBufferSize = shadowBuffer?.byteCount ?? 0;
        }

        private void FillShadowBuffer()
        {
            totBytesRead += currShadowBufferIx;
            currShadowBufferIx = 0;
            if (shadowBuffer != null)
                reader.ReturnBuffer(shadowBuffer);
            shadowBuffer = reader.RequestNewBuffer();
            currentShadowBufferSize = shadowBuffer?.byteCount ?? 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    if (disposing)
                    {
                        bool repositionStream = stream?.CanSeek ?? false;
                        if (reader != null)
                            if (!reader.AbortAsync().Wait(config.ReaderAbortTimeoutMs))
                                repositionStream = false;
                        if (repositionStream)
                            stream.Position = innerStreamStartPos + totBytesRead + currShadowBufferIx;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                    disposed = true;
                }
            }
            shadowBuffer = null;
            reader = null;
        }
    }
}
