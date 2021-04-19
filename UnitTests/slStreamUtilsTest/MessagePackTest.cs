/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using NUnit.Framework;
using slStreamUtils.FIFOWorker;
using slStreamUtils.MultiThreadedSerialization.MessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [MessagePackObject]
    public class TestItemMP
    {
        [Key(0)]
        public byte f;
    }

    [MessagePackObject]
    public class TestItemMPLarge
    {
        [Key(0)]
        public int f;
        [Key(1)]
        public string st;
    }

    [TestFixture]
    public class MessagePackTest
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
            MessagePackItemWrapper<TestItemMP>[] originalArray = new MessagePackItemWrapper<TestItemMP>[totItems];
            List<TestItemMP> deserializedArray = new List<TestItemMP>();
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new MessagePackItemWrapper<TestItemMP>(new TestItemMP() { f = (byte)(f % 32) });
            MemoryStream ms = new MemoryStream();

            await using (var ser = new CollectionSerializer<TestItemMP>(ms, cfg, MessagePackSerializerOptions.Standard))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(item, CancellationToken.None);

            byte[] originalArraySerialized = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray();
            using (var streamReader = new MessagePackStreamReader(new MemoryStream(originalArraySerialized)))
                while (await streamReader.ReadAsync(CancellationToken.None) is ReadOnlySequence<byte> msgpack)
                    deserializedArray.Add(
                        MessagePackSerializer.Deserialize<MessagePackItemWrapper<TestItemMP>>(msgpack, cancellationToken: CancellationToken.None).t);
            Assert.AreEqual(originalArray.Select(f => f.t.f).ToArray(), deserializedArray.Select(t => t.f).ToArray());
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task DeserializeAsync_ResultBufferMatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            MessagePackItemWrapper<TestItemMP>[] originalArray = new MessagePackItemWrapper<TestItemMP>[totItems];
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new MessagePackItemWrapper<TestItemMP>(new TestItemMP() {  f = (byte)(f % 32) }, 2);
            List<byte> originalArraySerialized = new List<byte>();
            for (int i = 0; i < originalArray.Length; i++)
            {
                MessagePackItemWrapper<TestItemMP> item = originalArray[i];
                originalArraySerialized.AddRange(MessagePackSerializer.Serialize(item, cancellationToken: CancellationToken.None));
            }
            List<TestItemMP> newArray = new List<TestItemMP>();
            MemoryStream ms = new MemoryStream(originalArraySerialized.ToArray());

            using (var deser = new CollectionDeserializer<TestItemMP>(cfg, MessagePackSerializerOptions.Standard))
                await foreach (var item in deser.DeserializeAsync(ms))
                    newArray.Add(item.t);

            Assert.AreEqual(originalArray.Select(f => f.t.f).ToArray(), newArray.Select(t => t.f).ToArray());
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
            TestItemMPLarge[] originalArray = new TestItemMPLarge[totItems];
            List<TestItemMPLarge> newArray = new List<TestItemMPLarge>();
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemMPLarge() { f = 1 << f % 32, st = string.Join(",", Enumerable.Range(0, f % 16)) };
            MemoryStream ms = new MemoryStream();

            await using (var ser = new CollectionSerializer<TestItemMPLarge>(ms, cfg, MessagePackSerializerOptions.Standard))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(new MessagePackItemWrapper<TestItemMPLarge>(item), CancellationToken.None);
            byte[] originalArraySerialized = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray();
            ms = new MemoryStream(originalArraySerialized.ToArray());
            using (var deser = new CollectionDeserializer<TestItemMPLarge>(cfg, MessagePackSerializerOptions.Standard))
                await foreach (var item in deser.DeserializeAsync(ms))
                    newArray.Add(item.t);

            Assert.AreEqual(originalArray.Select(t => t.f).ToArray(), newArray.Select(t => t.f).ToArray());
            Assert.AreEqual(originalArray.Select(t => t.st).ToArray(), newArray.Select(t => t.st).ToArray());
        }

    }
}