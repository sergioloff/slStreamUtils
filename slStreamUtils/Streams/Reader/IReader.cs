/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.Streams.Reader
{
    public unsafe interface IReader
    {
        Task ReturnBufferAsync(ShadowBufferData newBuffer, CancellationToken token);
        void ReturnBuffer(ShadowBufferData newBuffer);
        Task<ShadowBufferData> RequestNewBufferAsync(CancellationToken token);
        ShadowBufferData RequestNewBuffer();
        Task AbortAsync();
        void Abort();
    }
}
