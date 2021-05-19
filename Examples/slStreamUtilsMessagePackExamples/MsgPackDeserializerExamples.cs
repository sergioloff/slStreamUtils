/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using slStreamUtilsMessagePack;

namespace slStreamUtilsMessagePackExamples
{
    public static class MsgPackDeserializerExamples
    {
        public static async IAsyncEnumerable<X> Original_UnknownLengthArray_ReadAsync(string fileName)
        {
            using var s = File.OpenRead(fileName);
            using var sr = new MessagePackStreamReader(s);
            while (await sr.ReadAsync(CancellationToken.None) is ReadOnlySequence<byte> msgpack)
                yield return MessagePackSerializer.Deserialize<Frame<X>>(msgpack);
        }
        public static async IAsyncEnumerable<X> New_UnknownLengthArray_ReadAsync(string fileName)
        {
            using var s = File.OpenRead(fileName);
            using var ds = new CollectionDeserializerAsync<X>(maxConcurrentTasks: 2);
            await foreach (var item in ds.DeserializeAsync(s))
                yield return item;
        }

        public static async Task<ArrayX> Original_KnownLengthArray_ReadAsync(string fileName)
        {
            var opts = MessagePackSerializerOptions.Standard;
            using var s = File.OpenRead(fileName);
            return await MessagePackSerializer.DeserializeAsync<ArrayX>(s, opts);
        }

        public static async Task<ArrayX> New_KnownLengthArray_ReadAsync(string fileName)
        {
            int totWorkerThreads = 2;
            var opts = new FrameParallelOptions(totWorkerThreads, MessagePackSerializerOptions.Standard.WithResolver(FrameResolverPlusStandarResolver.Instance));
            using var s = File.OpenRead(fileName);
            return await MessagePackSerializer.DeserializeAsync<ArrayX>(s, opts);
        }

    }
}
