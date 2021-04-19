/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.IO;
using System.Threading.Tasks;
using slStreamUtils.Streams;

namespace slStreamUtilsExamples
{
    public static class PreFetchExamples
    {
        public static async Task<byte[]> New_ReadAsync(string fileName)
        {
            byte[] buffer = new byte[10];
            using (var s = File.OpenRead(fileName))
            using (var sr = new BufferedStreamReader(s, new BufferedStreamReaderConfig(totalPreFetchBlocks: 2)))
                await sr.ReadAsync(buffer);
            return buffer;
        }
    }
}
