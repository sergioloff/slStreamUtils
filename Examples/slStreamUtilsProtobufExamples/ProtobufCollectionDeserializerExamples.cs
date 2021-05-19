/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Collections.Generic;
using System.IO;
using slStreamUtils;
using slStreamUtilsProtobuf;

namespace slStreamUtilsProtobufExamples
{
    public static class ProtobufCollectionDeserializerExamples
    {
        public static IEnumerable<X> Original_UnknownLengthArray_Read(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            X obj;
            while ((obj = ProtoBuf.Serializer.DeserializeWithLengthPrefix<X>(stream, ProtoBuf.PrefixStyle.Base128, 1)) != null)
                yield return obj;
        }
        public static async IAsyncEnumerable<X> New_UnknownLengthArray_ReadAsync(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            using var ds = new CollectionDeserializerAsync<X>(new FIFOWorkerConfig(maxConcurrentTasks: 2));
            await foreach (var item in ds.DeserializeAsync(stream))
                yield return item.Item;
        }
    }
}
