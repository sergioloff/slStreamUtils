/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using NUnit.Framework;
using ProtoBuf;
using slStreamUtils;
using slStreamUtilsProtobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsProtobufTest
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
            Frame<TestItemPB>[] originalArray = new Frame<TestItemPB>[totItems];
            List<TestItemPB> deserializedArray = new List<TestItemPB>();
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new Frame<TestItemPB>(new TestItemPB() { f = (byte)(1 + f % 32) });
            MemoryStream ms = new MemoryStream();

            await using (var ser = new CollectionSerializerAsync<TestItemPB>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(item, CancellationToken.None);

            using (var msPB = new MemoryStream(ms.ToArray()))
            {
                while (true)
                {
                    var obj = ProtoBuf.Serializer.DeserializeWithLengthPrefix<TestItemPB>(msPB, ProtoBuf.PrefixStyle.Base128, 1);
                    if (obj is null)
                        break;
                    deserializedArray.Add(obj);
                }
            }
            Assert.AreEqual(originalArray.Select(f => f.Item.f).ToArray(), deserializedArray.Select(t => t.f).ToArray());
        }

        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task DeserializeAsync_ResultBufferMatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            TestItemPB[] originalArray = new TestItemPB[totItems];
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemPB() { f = (byte)(f % 32) };
            MemoryStream msOrig = new MemoryStream();
            for (int i = 0; i < originalArray.Length; i++)
            {
                TestItemPB item = originalArray[i];
                Serializer.SerializeWithLengthPrefix(msOrig, item, ProtoBuf.PrefixStyle.Base128, 1);
            }
            List<TestItemPB> newArray = new List<TestItemPB>();
            var msPB = new MemoryStream(msOrig.ToArray());

            using (var ds = new CollectionDeserializerAsync<TestItemPB>(cfg))
                await foreach (TestItemPB item in ds.DeserializeAsync(msPB))
                    newArray.Add(item);

            Assert.AreEqual(originalArray.Select(t => t.f).ToArray(), newArray.Select(t => t.f).ToArray());
        }

        [Test]
        public void DeserializeAsync_InvalidHeaderThrows()
        {
            var cfg = GetConfig(1, 1);
            byte[] bytes = new byte[] { 255 };
            var msPB = new MemoryStream(bytes);

            Assert.ThrowsAsync<EndOfStreamException>(async () =>
            {
                using (var ds = new CollectionDeserializerAsync<TestItemPB>(cfg))
                    await foreach (TestItemPB item in ds.DeserializeAsync(msPB)) ;
            });
        }

        [Test]
        public void DeserializeAsync_InvalidBodyLengthThrows()
        {
            var cfg = GetConfig(1, 1);
            byte[] bytes = new byte[] { 1, 1 };
            var msPB = new MemoryStream(bytes);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                using (var ds = new CollectionDeserializerAsync<TestItemPB>(cfg))
                    await foreach (TestItemPB item in ds.DeserializeAsync(msPB)) ;
            });
        }

        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task FullPipeline_ResultArraysMatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            TestItemPBLarge[] originalArray = new TestItemPBLarge[totItems];
            List<TestItemPBLarge> newArray = new List<TestItemPBLarge>();
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemPBLarge() { f = 1 << f % 32, st = string.Join(",", Enumerable.Range(0, f % 16)) };

            MemoryStream msOrig = new MemoryStream();
            await using (var ser = new CollectionSerializerAsync<TestItemPBLarge>(msOrig, cfg))
                foreach (TestItemPBLarge item in originalArray)
                    await ser.SerializeAsync(item, CancellationToken.None);
            var msPB = new MemoryStream(msOrig.ToArray());
            using (var ds = new CollectionDeserializerAsync<TestItemPBLarge>(cfg))
                await foreach (Frame<TestItemPBLarge> item in ds.DeserializeAsync(msPB))
                    newArray.Add(item);

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
        public async Task CombinedStackAsync_MultipleSizedObjects_ResultMatchesInput(int totItems, int totThreads, int maxQueuedItems, int innerLength)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            List<SerializedItemVarLength> newArray = new List<SerializedItemVarLength>();
            SerializedItemVarLength[] originalArray = GenerateObjectArrayVariableSize(totItems, innerLength);

            MemoryStream msOrig = new MemoryStream();
            await using (var ser = new CollectionSerializerAsync<SerializedItemVarLength>(msOrig, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(item, CancellationToken.None);
            var msPB = new MemoryStream(msOrig.ToArray());
            using (var ds = new CollectionDeserializerAsync<SerializedItemVarLength>(cfg))
                await foreach (var item in ds.DeserializeAsync(msPB))
                    newArray.Add(item.Item);

            Assert.AreEqual(originalArray, newArray);
        }

        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task SerializeAsync_DeserializesAsValidArrayWrapperInstance(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            TestItemProto[] originalArray = new TestItemProto[totItems];
            for (int f = 0; f < totItems; f++)
                originalArray[f] = new TestItemProto() { f = (byte)(f + 10) };
            MemoryStream ms = new MemoryStream();

            using (var ser = new CollectionSerializerAsync<TestItemProto>(ms, cfg))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(new Frame<TestItemProto>(item), CancellationToken.None);

            byte[] originalArraySerialized = ms.ToArray();
            var deserializedArrayWrapper = Serializer.Deserialize<ParallelServices_ArrayWrapper<TestItemProto>>(new MemoryStream(originalArraySerialized));
            Assert.AreEqual(originalArray.Select(t => t.f).ToArray(), deserializedArrayWrapper.Array.Select(t => t.f).ToArray());
        }

        [ProtoContract]
        public class SerializedItemVarLength : IEquatable<SerializedItemVarLength>
        {
            [ProtoMember(1)]
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
                return arr.Select(f => f.GetHashCode()).Aggregate((f, g) => f ^ g);
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
    }

}