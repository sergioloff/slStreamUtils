/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils.Streams;
using System;
using System.Collections.Generic;
using System.IO;

namespace slStreamUtilsProtobufBenchmark
{

    public sealed class StreamChain : IDisposable
    {
        List<IDisposable> itemsToDispose = new List<IDisposable>();

        public Stream ComposeChain(Stream s, BufferedStreamReaderConfig sr_cfg)
        {
            if (!(s is MemoryStream))
                itemsToDispose.Add(s);
            if (sr_cfg != null)
                itemsToDispose.Add(s = new BufferedStreamReader(s, sr_cfg));
            return s;
        }

        public Stream ComposeChain(Stream s, BufferedStreamWriterConfig sw_cfg)
        {
            if (!(s is MemoryStream))
                itemsToDispose.Add(s);
            if (sw_cfg != null)
                itemsToDispose.Add(s = new BufferedStreamWriter(s, sw_cfg));
            return s;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int f = itemsToDispose.Count - 1; f >= 0; f--)
                    itemsToDispose[f]?.Dispose();
                itemsToDispose.Clear();
            }
            itemsToDispose = null;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

