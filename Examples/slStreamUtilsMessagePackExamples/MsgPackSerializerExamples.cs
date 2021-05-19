/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using slStreamUtils;
using slStreamUtilsMessagePack;

namespace slStreamUtilsMessagePackExamples
{
    public static class MsgPackSerializerExamples
    {
        private static X[] GetSampleArray()
        {
            return Enumerable.Range(0, 10).Select(f => new X() { b1 = f % 2 == 0, i1 = f, l1 = f % 3 }).ToArray();
        }
        private static ArrayX GetArrayX()
        {
            return new ArrayX() { arr = GetSampleArray().Select(f => (Frame<X>)f).ToArray() };
        }
        public static async Task Original_UnknownLengthArray_WriteAsync(string fileName)
        {
            var arr = GetSampleArray();
            using var s = File.Create(fileName);
            foreach (var obj in arr)
            {
                await MessagePackSerializer.SerializeAsync<Frame<X>>(s, obj, MessagePackSerializerOptions.Standard);
            }
        }
        public static async Task New_UnknownLengthArray_WriteAsync(string fileName)
        {
            var arr = GetSampleArray();
            using var s = File.Create(fileName);
            await using var ser = new CollectionSerializerAsync<X>(s, new FIFOWorkerConfig(maxConcurrentTasks: 2));
            foreach (var item in arr)
                await ser.SerializeAsync(item);
        }

        public static async Task Original_KnownLengthArray_WriteAsync(string fileName)
        {
            var opts = MessagePackSerializerOptions.Standard;
            ArrayX obj = GetArrayX();
            using var s = File.Create(fileName);
            await MessagePackSerializer.SerializeAsync(s, obj, opts);
        }

        public static async Task New_KnownLengthArray_WriteAsync(string fileName)
        {
            int totWorkerThreads = 2;
            var opts = new FrameParallelOptions(totWorkerThreads, MessagePackSerializerOptions.Standard.WithResolver(FrameResolverPlusStandarResolver.Instance));
            ArrayX obj = GetArrayX();
            using var s = File.Create(fileName);
            await MessagePackSerializer.SerializeAsync(s, obj, opts);
        }
    }
}
