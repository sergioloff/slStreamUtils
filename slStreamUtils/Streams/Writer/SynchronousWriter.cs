/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams
{
    public class SynchronousWriter : IWriter
    {
        private readonly Stream stream;
        private readonly ShadowBufferData shadowBuffer;

        public SynchronousWriter(Stream stream, BufferedStreamWriterConfig config)
        {
            this.stream = stream;
            shadowBuffer = new ShadowBufferData(config.ShadowBufferSize);
        }

        public void ReturnBufferAndWrite(ShadowBufferData sourceBuffer)
        {
            stream.Write(sourceBuffer.buffer, 0, sourceBuffer.byteCount);
        }

        public async Task ReturnBufferAndWriteAsync(ShadowBufferData sourceBuffer, CancellationToken token)
        {
            await stream.WriteAsync(sourceBuffer.buffer, 0, sourceBuffer.byteCount, token).ConfigureAwait(false);
        }

        public void Abort() { }
        public Task AbortAsync()
        {
            return Task.CompletedTask;
        }

        public void Flush()
        {
            stream.Flush();
        }
        public async Task FlushAsync(CancellationToken token)
        {
            await stream.FlushAsync(token).ConfigureAwait(false);
        }

        public ShadowBufferData RequestBuffer()
        {
            return shadowBuffer;
        }

    }


}
