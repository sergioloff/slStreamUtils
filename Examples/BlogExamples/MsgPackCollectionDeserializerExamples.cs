/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MessagePack;
using slStreamUtils.FIFOWorker;
using slMsgPack = slStreamUtils.MultiThreadedSerialization.MessagePack;

namespace BlogExamples
{
    public static class MsgPackCollectionDeserializerExamples
    {
        public static async IAsyncEnumerable<X> Original_UnknownLengthArray_ReadAsync(string fileName)
        {
            using var s = File.OpenRead(fileName);
            using var sr = new MessagePackStreamReader(s);
            while (await sr.ReadAsync(CancellationToken.None) is ReadOnlySequence<byte> msgpack)
                yield return MessagePackSerializer.Deserialize<slMsgPack.MessagePackItemWrapper<X>>(msgpack).t; // discard the length field
        }
        public static async IAsyncEnumerable<X> New_UnknownLengthArray_ReadAsync(string fileName)
        {
            using var s = File.OpenRead(fileName);
            using var ds = new slMsgPack.CollectionDeserializer<X>(new FIFOWorkerConfig(totThreads: 2),
                MessagePackSerializerOptions.Standard);
            await foreach (var item in ds.DeserializeAsync(s))
                yield return item.t;
        }
    }
}
