/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams.Writer
{
    public class BufferedStreamWriter : Stream, IDisposable, IAsyncDisposable
    {
        private readonly int shadowBufferSize;
        private readonly Stream stream;
        private readonly IWriter writer;
        private ShadowBufferData shadowBuffer;
        private int currShadowBufferIx;
        private bool disposedValue = false;

        public BufferedStreamWriter(Stream stream)
            : this(stream, new BufferedStreamWriterConfig())
        {
        }

        public BufferedStreamWriter(Stream stream, BufferedStreamWriterConfig config)
            : this(stream, config, MakeWriter(stream, config))
        {
        }

        internal BufferedStreamWriter(Stream stream, BufferedStreamWriterConfig config, IWriter writer)
        {
            this.stream = stream;
            this.writer = writer;
            shadowBufferSize = config.ShadowBufferSize;
            currShadowBufferIx = 0;
            shadowBuffer = writer.RequestBuffer();
        }

        private static IWriter MakeWriter(Stream stream, BufferedStreamWriterConfig config)
        {
            if (config.UseDelayedWrite)
                return new DelayedWriter(stream, config);
            else
                return new SynchronousWriter(stream, config);
        }


        public override int ReadByte()
        {
            throw new NotSupportedException();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte value)
        {
            shadowBuffer.buffer[currShadowBufferIx++] = value;
            if (currShadowBufferIx == shadowBufferSize)
            {
                FlushShadowBuffer();
            }
        }

        public unsafe override void Write(byte[] buffer, int offset, int count)
        {
            if (WritePartialBufferHotPath(buffer, offset, count))
                return;
            int newIx = currShadowBufferIx + count;
            while (newIx >= shadowBufferSize)
            {
                WriteFullBuffer(buffer, ref offset, ref newIx);
                FlushShadowBuffer();
            }
            WritePartialBuffer(buffer, offset, newIx);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (WritePartialBufferHotPath(buffer, offset, count))
                return;
            int newIx = currShadowBufferIx + count;
            while (newIx >= shadowBufferSize)
            {
                WriteFullBuffer(buffer, ref offset, ref newIx);
                await FlushShadowBufferAsync(token).ConfigureAwait(false);
            }
            WritePartialBuffer(buffer, offset, newIx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool WritePartialBufferHotPath(byte[] buffer, int offset, int count)
        {
            if (count < shadowBufferSize - currShadowBufferIx)
            {
                ShadowCopy(buffer, offset, count);
                currShadowBufferIx += count;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WritePartialBuffer(byte[] buffer, int offset, int newIx)
        {
            int partialBlockLength = newIx - currShadowBufferIx;
            if (partialBlockLength > 0)
            {
                ShadowCopy(buffer, offset, partialBlockLength);
                currShadowBufferIx = newIx;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteFullBuffer(byte[] buffer, ref int offset, ref int newIx)
        {
            int availableLength = shadowBufferSize - currShadowBufferIx;
            ShadowCopy(buffer, offset, availableLength);
            currShadowBufferIx += availableLength;
            offset += availableLength;
            newIx -= shadowBufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] ShadowCopy(byte[] buffer, int offset, int count)
        {
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    ThirdParty.Sparrow.Memory.Copy(shadowBuffer.buffer_ptr + currShadowBufferIx, bufferPtr + offset, count);
                }
            }
            return buffer;
        }

        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override bool CanSeek => false;

        public override void Flush()
        {
            FlushShadowBuffer();
            writer.Flush();
        }
        public override async Task FlushAsync(CancellationToken token)
        {
            await FlushShadowBufferAsync(token).ConfigureAwait(false);
            await writer.FlushAsync(token).ConfigureAwait(false);
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        private void FlushShadowBuffer()
        {
            if (currShadowBufferIx > 0)
            {
                shadowBuffer.byteCount = currShadowBufferIx;
                writer.ReturnBufferAndWrite(shadowBuffer);
                shadowBuffer = writer.RequestBuffer();
                currShadowBufferIx = 0;
            }
        }

        private async Task FlushShadowBufferAsync(CancellationToken token)
        {
            if (currShadowBufferIx > 0)
            {
                shadowBuffer.byteCount = currShadowBufferIx;
                await writer.ReturnBufferAndWriteAsync(shadowBuffer, token).ConfigureAwait(false);
                shadowBuffer = writer.RequestBuffer();
                currShadowBufferIx = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposedValue)
                return;
            try
            {
                if (disposing)
                {
                    try
                    {
                        Flush();
                    }
                    finally
                    {
                        writer?.Abort();
                    }
                }
            }
            finally
            {
                shadowBuffer = null;
                base.Dispose(disposing);
                disposedValue = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await FlushAsync(CancellationToken.None).ConfigureAwait(false);
            await (writer?.AbortAsync()).ConfigureAwait(false);
        }

    }
}
