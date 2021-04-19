/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Collections.Generic;
using System.IO;
using slStreamUtils.FIFOWorker;
using slProto = slStreamUtils.MultiThreadedSerialization.Protobuf;

namespace BlogExamples
{
    public static class ProtobufCollectionDeserializerExamples
    {
        public static IEnumerable<X> Original_UnknownLengthArray_Read(string fileName)
        {
            using var s = File.OpenRead(fileName);
            while (true)
            {
                X obj = ProtoBuf.Serializer.DeserializeWithLengthPrefix<X>(s, ProtoBuf.PrefixStyle.Base128, 1);
                if (obj is null)
                    break;
                yield return obj;
            }
        }
        public static async IAsyncEnumerable<X> New_UnknownLengthArray_ReadAsync(string fileName)
        {
            using var s = File.OpenRead(fileName);
            using var ds = new slProto.CollectionDeserializer<X>(new FIFOWorkerConfig(totThreads: 2));
            await foreach (var item in ds.DeserializeAsync(s))
                yield return item.t;
        }
    }
}
