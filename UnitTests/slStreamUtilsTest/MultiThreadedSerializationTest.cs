/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using NUnit.Framework;
using slStreamUtils.FIFOWorker;
using slStreamUtils.MultiThreadedSerialization;
using slStreamUtils.MultiThreadedSerialization.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{

    [TestFixture]
    public class MultiThreadedSerializationTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private FIFOWorkerConfig GetConfig(int totThreads, int maxQueuedItems)
        {
            return new FIFOWorkerConfig(totThreads, maxQueuedItems);
        }

        public class MockItemWrapper<T> : ItemWrapper<T>
        {
            public MockItemWrapper(T t, int l) : base(t, l) { }
            public MockItemWrapper(T t) : base(t) { }
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

        public class MockException : Exception
        { }

        public class MockMultiThreadedDeserializer<T> : CollectionDeserializer<T>
        {
            public MockMultiThreadedDeserializer(FIFOWorkerConfig fifoConfig) : base(fifoConfig) { }

            protected override ItemWrapper<T> GetWrapper(T t, int l)
            {
                return new MockItemWrapper<T>(t, l);
            }

            protected override async Task<T> ReadBodyAsync(Stream s, CancellationToken token)
            {
                byte[] buf = new byte[sizeof(uint)];
                if (s.Length != sizeof(uint))
                    throw new InvalidDataException();
                if (s.Position != 0)
                    throw new InvalidDataException();
                int i = await s.ReadAsync(buf, 0, sizeof(uint), token);
                if (i != sizeof(uint))
                    throw new InvalidDataException();
                SerializedItem res = new SerializedItem();
                res.i = (uint)buf[0] << 0 | (uint)buf[1] << 8 | (uint)buf[2] << 16 | (uint)buf[3] << 24;
                return (T)(object)res;
            }

            protected override async Task<Tuple<bool, int>> ReadHeaderAsync(Stream s, CancellationToken token)
            {
                byte[] buf = new byte[1];
                int i = await s.ReadAsync(buf, 0, 1, token);
                if (i == -1)
                    return new Tuple<bool, int>(false, 0);
                return new Tuple<bool, int>(true, buf[0]);
            }
        }
        public class MockMultiThreadedSerializer<T> : CollectionSerializer<T>
        {
            public MockMultiThreadedSerializer(Stream stream, FIFOWorkerConfig fifoConfig) : base(stream, fifoConfig) { }

            protected override async Task WriteBodyAsync(Stream stream, T t, CancellationToken token)
            {
                uint i = (t as SerializedItem).i;
                await stream.WriteAsync(new byte[] { (byte)(i & 255), (byte)(i >> 8 & 255), (byte)(i >> 16 & 255), (byte)(i >> 24 & 255) }, 0, sizeof(uint), token);
            }

            protected override async Task WriteHeaderAsync(Stream stream, int len, CancellationToken token)
            {
                await stream.WriteAsync(new byte[] { (byte)len }, 0, 1, token);
            }
        }

        public static byte[] GenerateMockBinaryBuffer(int totItems)
        {
            List<byte> arr = new List<byte>();
            for (int i = 0; i < totItems; i++)
            {
                uint item = PseudoRand(i);
                arr.Add(sizeof(uint));
                arr.Add((byte)(item >> 0 & 255));
                arr.Add((byte)(item >> 8 & 255));
                arr.Add((byte)(item >> 16 & 255));
                arr.Add((byte)(item >> 24 & 255));
            }
            return arr.ToArray();
        }
        public static SerializedItem[] GenerateMockObjectArray(int totItems)
        {
            var arr = new List<SerializedItem>();
            for (int i = 0; i < totItems; i++)
            {
                uint item = PseudoRand(i);
                arr.Add(new SerializedItem() { i = item });
            }
            return arr.ToArray();
        }

        public static uint PseudoRand(int ix)
        {
            ulong i = (ulong)ix;
            return (uint)(i * i * i % uint.MaxValue);
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task DeserializeAsync_MatchesExpected(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            CancellationTokenSource ts = new CancellationTokenSource();
            var originalItems = GenerateMockBinaryBuffer(totItems);
            var expectedItems = GenerateMockObjectArray(totItems);
            MemoryStream ms = new MemoryStream(originalItems);

            List<SerializedItem> finalItems = new List<SerializedItem>();
            using (var mockDeser = new MockMultiThreadedDeserializer<SerializedItem>(cfg))
                await foreach (var item in mockDeser.DeserializeAsync(ms, ts.Token))
                    finalItems.Add(item.t);

            Assert.AreEqual(expectedItems, finalItems);
        }
        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task SerializeAsync_ItemsProcessed(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            CancellationTokenSource ts = new CancellationTokenSource();
            var originalItems = GenerateMockObjectArray(totItems);
            var expectedItems = GenerateMockBinaryBuffer(totItems);

            MemoryStream ms = new MemoryStream();
            await using (var mockSer = new MockMultiThreadedSerializer<SerializedItem>(ms, cfg))
                foreach (var item in originalItems)
                    await mockSer.SerializeAsync(new MockItemWrapper<SerializedItem>(item), ts.Token);

            Assert.AreEqual(expectedItems, ms.ToArray());
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task CombinedStackAsync_ResultMatchesInput(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            CancellationTokenSource ts = new CancellationTokenSource();
            var originalItems = GenerateMockObjectArray(totItems);
            MemoryStream ms = new MemoryStream();
            await using (var mockSer = new MockMultiThreadedSerializer<SerializedItem>(ms, cfg))
                foreach (SerializedItem item in originalItems)
                    await mockSer.SerializeAsync(new MockItemWrapper<SerializedItem>(item), ts.Token);

            ms.Position = 0;

            List<SerializedItem> finalItems = new List<SerializedItem>();
            using (var mockDeser = new MockMultiThreadedDeserializer<SerializedItem>(cfg))
                await foreach (var item in mockDeser.DeserializeAsync(ms, ts.Token))
                    finalItems.Add(item.t);

            Assert.AreEqual(originalItems, finalItems);
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 2)]
        [TestCase(10, 1, 2)]
        [TestCase(100, 2, 2)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task CombinedStack_SyncDispose_ResultMatchesInput(int totItems, int totThreads, int maxQueuedItems)
        {
            var cfg = GetConfig(totThreads, maxQueuedItems);
            CancellationTokenSource ts = new CancellationTokenSource();
            var originalItems = GenerateMockObjectArray(totItems);
            MemoryStream ms = new MemoryStream();
            using (var mockSer = new MockMultiThreadedSerializer<SerializedItem>(ms, cfg))
                foreach (var item in originalItems)
                    await mockSer.SerializeAsync(new MockItemWrapper<SerializedItem>(item), ts.Token);

            ms.Position = 0;

            List<SerializedItem> finalItems = new List<SerializedItem>();
            using (var mockDeser = new MockMultiThreadedDeserializer<SerializedItem>(cfg))
                await foreach (var item in mockDeser.DeserializeAsync(ms, ts.Token))
                    finalItems.Add(item.t);

            Assert.AreEqual(originalItems, finalItems);
        }
    }
}