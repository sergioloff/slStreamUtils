/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using slStreamUtils.FIFOWorker;
using slProto = slStreamUtils.MultiThreadedSerialization.Protobuf;

namespace BlogExamples
{
    public static class ProtobufCollectionSerializerExamples
    {
        private static IEnumerable<X> GetSampleArray()
        {
            return Enumerable.Range(0, 10).Select(f => new X() { b1 = f % 2 == 0, i1 = f, l1 = f % 3 });
        }
        public static void Original_UnknownLengthArray_Write(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);
            IEnumerable<X> arr = GetSampleArray();
            using var s = File.Create(fileName);
            foreach (var obj in arr)
                ProtoBuf.Serializer.SerializeWithLengthPrefix(s, obj, ProtoBuf.PrefixStyle.Base128, 1);
        }
        public static async Task New_UnknownLengthArray_WriteAsync(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);
            IEnumerable<X> arr = GetSampleArray();
            using var s = File.Create(fileName);
            await using var ser = new slProto.CollectionSerializer<X>(s, new FIFOWorkerConfig(totThreads: 2));
            foreach (var item in arr)
                await ser.SerializeAsync(new slProto.ProtobufItemWrapper<X>(item));
        }
    }
}
