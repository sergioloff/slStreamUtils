/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using slStreamUtils.FIFOWorker;
using slMsgPack = slStreamUtils.MultiThreadedSerialization.MessagePack;

namespace BlogExamples
{
    public static class MsgPackCollectionSerializerExamples
    {
        private static IEnumerable<X> GetSampleArray()
        {
            return Enumerable.Range(0, 10).Select(f => new X() { b1 = f % 2 == 0, i1 = f, l1 = f % 3 });
        }
        public static async Task Original_UnknownLengthArray_WriteAsync(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);

            IEnumerable<X> arr = GetSampleArray();
            using var s = File.Create(fileName);
            foreach (var obj in arr)
            {
                // In this implementation we end up serializing twice, 1st to get the length of the buffer, 
                // and later for the buffer itself. 
                // There are other more efficient ways of achieving the same result, but this will do as a 
                // simple example.
                // For a fair comparison, in the benchmarks we omitted this part and used a constant value.
                var sizeofObj = MessagePackSerializer.Serialize(obj).Length;
                var wrappedObj = new slMsgPack.MessagePackItemWrapper<X>(obj, sizeofObj);
                await MessagePackSerializer.SerializeAsync(s, wrappedObj, MessagePackSerializerOptions.Standard);
            }
        }
        public static async Task New_UnknownLengthArray_WriteAsync(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);

            IEnumerable<X> arr = GetSampleArray();
            using var s = File.Create(fileName);
            await using var ser = new slMsgPack.CollectionSerializer<X>(s, new FIFOWorkerConfig(totThreads: 2),
                MessagePackSerializerOptions.Standard);
            foreach (var item in arr)
                await ser.SerializeAsync(new slMsgPack.MessagePackItemWrapper<X>(item));
        }
    }
}
