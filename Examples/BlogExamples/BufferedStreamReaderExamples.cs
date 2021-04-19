/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.IO;
using System.Threading.Tasks;
using slStreamUtils.Streams.Reader;
namespace BlogExamples
{
    public static class BufferedStreamReaderExamples
    {
        public static async Task<byte[]> Original_Simple_ReadAsync(string fileName)
        {
            byte[] buffer = new byte[10];
            using (var s = File.OpenRead(fileName))
                await s.ReadAsync(buffer);
            return buffer;
        }
        public static async Task<byte[]> New_Simple_ReadAsync(string fileName)
        {
            byte[] buffer = new byte[10];
            using (var s = File.OpenRead(fileName))
            using (var sr = new BufferedStreamReader(s))
                await sr.ReadAsync(buffer);
            return buffer;
        }
    }
}
