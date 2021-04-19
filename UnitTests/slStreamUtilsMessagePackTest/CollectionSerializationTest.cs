/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using NUnit.Framework;
using slStreamUtils;
using slStreamUtilsMessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsMessagePackTest
{
    [TestFixture]
    public class CollectionSerializationTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private FIFOWorkerConfig GetConfig(int totThreads, int maxQueuedItems)
        {
            return new FIFOWorkerConfig(totThreads, maxQueuedItems);
        }


        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task SerializeAsync_ResultBufferMatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            TestItemMP[] originalArray = new TestItemMP[totItems];
            List<TestItemMP> asyncArray = new List<TestItemMP>();
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemMP() { f = (byte)(1 + f % 32) };
            MemoryStream ms = new MemoryStream();

            await using (var ser = new CollectionSerializerAsync<TestItemMP>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(item, CancellationToken.None);

            byte[] originalArraySerialized = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray();
            using (var streamReader = new MessagePackStreamReader(new MemoryStream(originalArraySerialized)))
                while (await streamReader.ReadAsync(CancellationToken.None) is ReadOnlySequence<byte> msgpack)
                    asyncArray.Add(MessagePackSerializer.Deserialize<Frame<TestItemMP>>(msgpack, cancellationToken: CancellationToken.None));
            Assert.AreEqual(originalArray.Select(f => f.f).ToArray(), asyncArray.Select(t => t.f).ToArray());
        }



        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task SerializeAsync_FrameHeadersAreCorrect(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            TestItemMP[] originalArray = new TestItemMP[totItems];
            List<byte> syncSerializedWithFrameData = new List<byte>();
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemMP() { f = (byte)(1 + f % 32) };
            MemoryStream ms = new MemoryStream();
            foreach (var item in originalArray)
            {
                int bodylen = MessagePackSerializer.Serialize(item, MessagePackSerializerOptions.Standard).Length;
                //bodylen = bodylen | (1 << 24); // (A) trick to force writing Int32
                Frame<TestItemMP> framedItem = new Frame<TestItemMP>(bodylen, item);
                syncSerializedWithFrameData.AddRange(MessagePackSerializer.Serialize(framedItem, cancellationToken: CancellationToken.None));
            }
            //for (int i = 0; i < syncSerializedWithFrameData.Count - 1; i++)
            //{
            //    // undo (A)
            //    if (syncSerializedWithFrameData[i] == MessagePackCode.UInt32 && syncSerializedWithFrameData[i + 1] == 1)
            //        syncSerializedWithFrameData[i + 1] = 0;
            //}

            await using (var ser = new CollectionSerializerAsync<TestItemMP>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(item, CancellationToken.None);

            byte[] originalArraySerialized = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray();
            Assert.AreEqual(syncSerializedWithFrameData.ToArray(), originalArraySerialized);
        }

        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task DeserializeAsync_ResultBufferMatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            Frame<TestItemMP>[] originalArray = new Frame<TestItemMP>[totItems];
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new Frame<TestItemMP>(2, new TestItemMP() { f = (byte)(f % 32) });
            List<byte> originalArraySerialized = new List<byte>();
            for (int i = 0; i < originalArray.Length; i++)
            {
                Frame<TestItemMP> item = originalArray[i];
                originalArraySerialized.AddRange(MessagePackSerializer.Serialize(item, cancellationToken: CancellationToken.None));
            }
            List<TestItemMP> newArray = new List<TestItemMP>();
            MemoryStream ms = new MemoryStream(originalArraySerialized.ToArray());

            using (var deser = new CollectionDeserializerAsync<TestItemMP>(cfg))
                await foreach (var item in deser.DeserializeAsync(ms))
                    newArray.Add(item.Item);

            Assert.AreEqual(originalArray.Select(f => f.Item.f).ToArray(), newArray.Select(t => t.f).ToArray());
        }



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

            await using (var ser = new CollectionSerializerAsync<TestItemMPLarge>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(new Frame<TestItemMPLarge>(item), CancellationToken.None);
            ms = new MemoryStream(new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray());
            using (var deser = new CollectionDeserializerAsync<TestItemMPLarge>(cfg))
                await foreach (var item in deser.DeserializeAsync(ms))
                    newArray.Add(item.Item);

            Assert.AreEqual(originalArray.Select(t => t.f).ToArray(), newArray.Select(t => t.f).ToArray());
            Assert.AreEqual(originalArray.Select(t => t.st).ToArray(), newArray.Select(t => t.st).ToArray());
        }



        public class SerializedItem : IEquatable<SerializedItem>
        {
            public uint i;
            public bool Equals(SerializedItem other)
            {
                if (other == null)
                    return false;
                return other.i == i;
            }
        }

        [MessagePackObject]
        public class SerializedItemVarLength : IEquatable<SerializedItemVarLength>
        {
            [Key(0)]
            public uint[] arr;

            public bool Equals(SerializedItemVarLength other)
            {
                if (other == null)
                    return false;
                if ((other.arr == null) != (arr == null))
                    return false;
                if (arr == null)
                    return true;
                if (arr.Length != other.arr.Length)
                    return false;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] != other.arr[i])
                        return false;
                return true;
            }
            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (!(obj is SerializedItemVarLength))
                    return false;
                return Equals((SerializedItemVarLength)obj);
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(arr);
            }
        }


        public static SerializedItemVarLength[] GenerateObjectArrayVariableSize(int totItems, int innerLength)
        {
            var res = new List<SerializedItemVarLength>();
            for (int i = 0; i < totItems; i++)
            {
                res.Add(new SerializedItemVarLength() { arr = Enumerable.Range(0, innerLength).Select(f => PseudoRand(i * 1000 + f)).ToArray() });
            }
            return res.ToArray();
        }

        public static uint PseudoRand(int ix)
        {
            ulong i = (ulong)ix;
            return (uint)(i * i * i % uint.MaxValue);
        }


        [TestCase(1, 1, 2, 10)]
        [TestCase(10, 2, 2, 10)]
        [TestCase(1, 1, 2, 20)]
        [TestCase(10, 2, 2, 20)]
        [TestCase(1, 1, 2, 50)]
        [TestCase(10, 2, 2, 50)]
        [TestCase(1, 1, 2, 100)]
        [TestCase(10, 2, 2, 100)]
        [TestCase(1, 1, 2, 200)]
        [TestCase(10, 2, 2, 200)]
        [TestCase(1, 1, 2, 10000)]
        [TestCase(10, 2, 2, 10000)]
        [TestCase(1, 1, 2, 66000)]
        [TestCase(10, 2, 2, 66000)]
        public async Task FullPipeline_MultipleSizedObjects_ResultMatchesInput(int totItems, int totThreads, int maxQueuedItems, int innerLength)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            List<SerializedItemVarLength> newArray = new List<SerializedItemVarLength>();
            SerializedItemVarLength[] originalArray = GenerateObjectArrayVariableSize(totItems, innerLength);
            MemoryStream ms = new MemoryStream();

            await using (var ser = new CollectionSerializerAsync<SerializedItemVarLength>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(new Frame<SerializedItemVarLength>(item), CancellationToken.None);
            byte[] originalArraySerialized = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Position).ToArray();
            ms = new MemoryStream(originalArraySerialized.ToArray());
            using (var deser = new CollectionDeserializerAsync<SerializedItemVarLength>(cfg))
                await foreach (var item in deser.DeserializeAsync(ms))
                    newArray.Add(item.Item);

            Assert.AreEqual(originalArray, newArray);
        }


    }
}