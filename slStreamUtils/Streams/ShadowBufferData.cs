/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.Runtime.InteropServices;

namespace slStreamUtils.Streams
{
    public unsafe class ShadowBufferData : IDisposable
    {
        internal int byteCount;
        internal readonly byte[] buffer;
        internal readonly GCHandle buffer_handle;
        internal readonly byte* buffer_ptr;
        private bool disposedValue;

        public ShadowBufferData(int shadowBufferSize, int count = 0)
        {
            byteCount = count;
            buffer = new byte[shadowBufferSize];
            buffer_handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            buffer_ptr = (byte*)buffer_handle.AddrOfPinnedObject().ToPointer();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                buffer_handle.Free();
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
