/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using slStreamUtils.Streams;

namespace slStreamUtilsExamples
{
    public static class BufferedStreamWriterExamples
    {
        public static async Task Original_WriteAsync(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);

            byte[] buffer = Enumerable.Range(0, 10).Select(f => (byte)f).ToArray();
            using (var s = File.Create(fileName))
                await s.WriteAsync(buffer);
        }
        public static async Task New_WriteAsync(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);

            byte[] buffer = Enumerable.Range(0, 10).Select(f => (byte)f).ToArray();
            using (var s = File.Create(fileName))
            using (var sr = new BufferedStreamWriter(s))
                await sr.WriteAsync(buffer);
        }
    }
}
