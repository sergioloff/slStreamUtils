/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams
{
    public interface IWriter
    {
        void ReturnBufferAndWrite(ShadowBufferData sourceBuffer);
        Task ReturnBufferAndWriteAsync(ShadowBufferData sourceBuffer, CancellationToken token);
        void Flush();
        Task FlushAsync(CancellationToken token);
        void Abort();
        Task AbortAsync();
        ShadowBufferData RequestBuffer();
    }
}
