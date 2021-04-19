/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using NUnit.Framework;
using ProtoBuf;
using slStreamUtils.FIFOWorker;
using slStreamUtils.MultiThreadedSerialization.Protobuf;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [ProtoContract]
    public class TestItemProto
    {
        [ProtoMember(1)]
        public byte f;
    }

    [ProtoContract]
    public class TestItemLargeProto
    {
        [ProtoMember(1)]
        public int f;
        [ProtoMember(2)]
        public string st;
    }

    [TestFixture]
    public class ProtobufNetTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private FIFOWorkerConfig GetConfig(int totThreads, int maxQueuedItems)
        {
            return new FIFOWorkerConfig(totThreads, maxQueuedItems);
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task SerializeAsync_ResultBufferMatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            TestItemProto[] originalArray = new TestItemProto[totItems];
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemProto() { f = (byte)(f + 10) };
            MemoryStream ms = new MemoryStream();

            await using (var ser = new CollectionSerializer<TestItemProto>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(new ProtobufItemWrapper<TestItemProto>(item), CancellationToken.None);

            byte[] originalArraySerialized = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray();
            var deserializedArrayWrapper = Serializer.Deserialize<ProtobufColectionWrapper<TestItemProto>>(new MemoryStream(originalArraySerialized));
            Assert.AreEqual(originalArray.Select(t => t.f).ToArray(), deserializedArrayWrapper.a.Select(t => t.f).ToArray());
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task FullPipeline_ResultArraysMatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            TestItemLargeProto[] originalArray = new TestItemLargeProto[totItems];
            List<TestItemLargeProto> newArray = new List<TestItemLargeProto>();
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemLargeProto() { f = 1 << f % 32, st = string.Join(",", Enumerable.Range(0, f % 16)) };
            MemoryStream ms = new MemoryStream();

            await using (var ser = new CollectionSerializer<TestItemLargeProto>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(new ProtobufItemWrapper<TestItemLargeProto>(item), CancellationToken.None);
            byte[] originalArraySerialized = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray();
            ms = new MemoryStream(originalArraySerialized.ToArray());
            using (var deser = new CollectionDeserializer<TestItemLargeProto>(cfg))
                await foreach (var item in deser.DeserializeAsync(ms))
                    newArray.Add(item.t);

            Assert.AreEqual(originalArray.Select(t => t.f).ToArray(), newArray.Select(t => t.f).ToArray());
            Assert.AreEqual(originalArray.Select(t => t.st).ToArray(), newArray.Select(t => t.st).ToArray());
        }

    }
}