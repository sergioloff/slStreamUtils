/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using BenchmarkDotNet.Attributes;
using MessagePack;
using ProtoBuf;
using slStreamUtils.FIFOWorker;
using slStreamUtils.MultiThreadedSerialization.MessagePack;
using slStreamUtils.MultiThreadedSerialization.Protobuf;
using slStreamUtils.MultiThreadedSerialization.Wrappers;
using slStreamUtils.Streams.Reader;
using slStreamUtils.Streams.Writer;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsBenchmark
{

    public class Benchmark_Small_Config
    {
        public const int blockSize = 128;
        public const int totBlocks = 1024 * 16 * 4 * 30;
    }
    public class Benchmark_Small_Read : BenchmarkLogic<TestClassSmall1>
    {
        public Benchmark_Small_Read() : base(blockSize: Benchmark_Small_Config.blockSize, totBlocks: Benchmark_Small_Config.totBlocks) { }

        [Params(-1, 0, 2, 4)]
        public int TotalPreFetchBlocks { get; set; }

        [Benchmark]
        public async Task<int> ReadMsgPackAsync()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return await ReadMsgPackAsync(config_r);
        }

        [Benchmark]
        public int ReadProtobuf()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return ReadProtobuf(config_r);
        }
    }

    public class Benchmark_Small_MT_Read : BenchmarkLogic<TestClassSmall1>
    {
        public Benchmark_Small_MT_Read() : base(blockSize: Benchmark_Small_Config.blockSize, totBlocks: Benchmark_Small_Config.totBlocks) { }

        [Params(0, 1, 2, 3, 4)]

        public int TotWorkerThreads { get; set; }

        [Params(-1, 0, 2, 4)]
        public int TotalPreFetchBlocks { get; set; }

        [Benchmark]
        public async Task<int> ReadMsgPackAsync()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return await ReadMsgPackMTAsync(TotWorkerThreads, config_r);
        }
        [Benchmark]
        public async Task<int> ReadProtobufAsync()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return await ReadProtobufMTAsync(TotWorkerThreads, config_r);
        }
    }

    public class Benchmark_Small_Write : BenchmarkLogic<TestClassSmall1>
    {
        public Benchmark_Small_Write() : base(blockSize: Benchmark_Small_Config.blockSize, totBlocks: Benchmark_Small_Config.totBlocks) { }

        [Params(-1, 0, 2, 4, 8)]
        public int TotalDelayedWriterBlocks { get; set; }

        [Benchmark]
        public async Task WriteMsgPackAsync()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            await WriteMsgPackAsync(sw_cfg);
        }

        [Benchmark]
        public void WriteProtobuf()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            WriteProtobuf(sw_cfg);
        }
    }

    public class Benchmark_Small_MT_Write : BenchmarkLogic<TestClassSmall1>
    {
        public Benchmark_Small_MT_Write() : base(blockSize: Benchmark_Small_Config.blockSize, totBlocks: Benchmark_Small_Config.totBlocks) { }

        [Params(0, 1, 2, 3, 4)]
        public int TotWorkerThreads { get; set; }

        [Params(-1, 0, 2, 4, 8)]
        public int TotalDelayedWriterBlocks { get; set; }

        [Benchmark]
        public async Task WriteMsgPackAsync()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            await WriteMsgPackMTAsync(TotWorkerThreads, sw_cfg);
        }

        [Benchmark]
        public async Task WriteProtobufAsync()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            await WriteProtobufMTAsync(TotWorkerThreads, sw_cfg);
        }
    }



    public class Benchmark_Large_Config
    {
        public const int blockSize = 128;
        public const int totBlocks = 1024 * 16 * 4;
    }
    public class Benchmark_Large_Read : BenchmarkLogic<TestClassLarge1>
    {
        public Benchmark_Large_Read() : base(blockSize: Benchmark_Large_Config.blockSize, totBlocks: Benchmark_Large_Config.totBlocks) { }

        [Params(-1, 0, 2, 4)]
        public int TotalPreFetchBlocks { get; set; }

        [Benchmark]
        public async Task<int> ReadMsgPackAsync()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return await ReadMsgPackAsync(config_r);
        }

        [Benchmark]
        public int ReadProtobuf()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return ReadProtobuf(config_r);
        }
    }

    public class Benchmark_Large_MT_Read : BenchmarkLogic<TestClassLarge1>
    {
        public Benchmark_Large_MT_Read() : base(blockSize: Benchmark_Large_Config.blockSize, totBlocks: Benchmark_Large_Config.totBlocks) { }

        [Params(0, 1, 2, 3, 4)]

        public int TotWorkerThreads { get; set; }

        [Params(-1, 0, 2, 4)]
        public int TotalPreFetchBlocks { get; set; }

        [Benchmark]
        public async Task<int> ReadMsgPackAsync()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return await ReadMsgPackMTAsync(TotWorkerThreads, config_r);
        }
        [Benchmark]
        public async Task<int> ReadProtobufAsync()
        {
            BufferedStreamReaderConfig config_r = TotalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: TotalPreFetchBlocks) : null;
            return await ReadProtobufMTAsync(TotWorkerThreads, config_r);
        }
    }

    public class Benchmark_Large_Write : BenchmarkLogic<TestClassLarge1>
    {
        public Benchmark_Large_Write() : base(blockSize: Benchmark_Large_Config.blockSize, totBlocks: Benchmark_Large_Config.totBlocks) { }

        [Params(-1, 0, 2, 4, 8)]
        public int TotalDelayedWriterBlocks { get; set; }

        [Benchmark]
        public async Task WriteMsgPackAsync()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            await WriteMsgPackAsync(sw_cfg);
        }

        [Benchmark]
        public void WriteProtobuf()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            WriteProtobuf(sw_cfg);
        }
    }

    public class Benchmark_Large_MT_Write : BenchmarkLogic<TestClassLarge1>
    {
        public Benchmark_Large_MT_Write() : base(blockSize: Benchmark_Large_Config.blockSize, totBlocks: Benchmark_Large_Config.totBlocks) { }

        [Params(0, 1, 2, 3, 4)]
        public int TotWorkerThreads { get; set; }

        [Params(-1, 0, 2, 4, 8)]
        public int TotalDelayedWriterBlocks { get; set; }

        [Benchmark]
        public async Task WriteMsgPackAsync()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            await WriteMsgPackMTAsync(TotWorkerThreads, sw_cfg);
        }

        [Benchmark]
        public async Task WriteProtobufAsync()
        {
            BufferedStreamWriterConfig sw_cfg = TotalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: TotalDelayedWriterBlocks) : null;
            await WriteProtobufMTAsync(TotWorkerThreads, sw_cfg);
        }
    }

    public class BenchmarkLogic<T> where T : IDoStuff, IAmRandomInstantiable<T>, IMeasureSizeWithAllignmentPadding, new()
    {
        public int BlockSize { get; private set; }
        public int TotBlocks { get; private set; }
        MessagePackSerializerOptions mp_r_opts;
        MessagePackSerializerOptions mp_w_opts;
        Func<int, slStreamUtils.MultiThreadedSerialization.CollectionDeserializer<T>> deserializerFactory_MP, deserializerFactory_PB;
        Func<int, Stream, slStreamUtils.MultiThreadedSerialization.CollectionSerializer<T>> serializerFactory_MP, serializerFactory_PB;
        public T[] objArr { get; private set; }
        public string tmpFilename_MP;
        public string tmpFilename_PB;

        public BenchmarkLogic(int blockSize, int totBlocks)
        {
            BlockSize = blockSize;
            TotBlocks = totBlocks;
        }

        public long MP_size { get; protected set; }
        public long PB_size { get; protected set; }
        public long Raw_size_padded { get; protected set; }
        public long Raw_size_padded_SingleItem { get; protected set; }

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            objArr = GetRandInstanceArr(BlockSize, TotBlocks);
            tmpFilename_MP = Path.Combine(Path.GetTempPath(), "tmp_MP.dat");
            tmpFilename_PB = Path.Combine(Path.GetTempPath(), "tmp_PB.dat");

            DelTempFiles();

            mp_w_opts = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.None);
            serializerFactory_MP = (totWorkerThreads, s_w) => new slStreamUtils.MultiThreadedSerialization.MessagePack.CollectionSerializer<T>(s_w, new FIFOWorkerConfig(totWorkerThreads), mp_w_opts);
            serializerFactory_PB = (totWorkerThreads, s_w) => new slStreamUtils.MultiThreadedSerialization.Protobuf.CollectionSerializer<T>(s_w, new FIFOWorkerConfig(totWorkerThreads));

            mp_r_opts = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.None);
            deserializerFactory_MP = (totWorkerThreads) => new slStreamUtils.MultiThreadedSerialization.MessagePack.CollectionDeserializer<T>(new FIFOWorkerConfig(totWorkerThreads), mp_r_opts);
            deserializerFactory_PB = (totWorkerThreads) => new slStreamUtils.MultiThreadedSerialization.Protobuf.CollectionDeserializer<T>(new FIFOWorkerConfig(totWorkerThreads));

            // generate binary files to work with
            MP_size = await SerializeInitialFileAsync(tmpFilename_MP, serializerFactory_MP, (t) => new MessagePackItemWrapper<T>(t));
            PB_size = await SerializeInitialFileAsync(tmpFilename_PB, serializerFactory_PB, (t) => new ProtobufItemWrapper<T>(t));
            Raw_size_padded = objArr.Sum(f => f.GetSize()) + objArr.Length * IntPtr.Size;
            Raw_size_padded_SingleItem = objArr.First().GetSize();
            Console.WriteLine($"MessagePack file length = {MP_size / ((double)1024 * 1024):f2} MB");
            Console.WriteLine($"Protobuf file length = {PB_size / ((double)1024 * 1024):f2} MB");
            Console.WriteLine($"Memory size (padded) = {Raw_size_padded / ((double)1024 * 1024):f2} MB");
            Console.WriteLine($"Memory size (padded) for 1 item = {Raw_size_padded_SingleItem / (double)1024:f2} KB");
        }

        [GlobalCleanup]

        public void GlobalCleanup()
        {
            DelTempFiles();
            objArr = null;
        }

        protected async Task<int> ReadMsgPackAsync(BufferedStreamReaderConfig config_r)
        {
            int res = 0;
            using (StreamChain sc1 = new StreamChain())
            {
                Stream s = sc1.ComposeChain(File.Open(tmpFilename_MP, FileMode.Open, FileAccess.Read, FileShare.None), config_r);
                using (var streamReader = new MessagePackStreamReader(s))
                    while (await streamReader.ReadAsync(CancellationToken.None) is ReadOnlySequence<byte> msgpack)
                    {
                        var obj = MessagePackSerializer.Deserialize<MessagePackItemWrapper<T>>(msgpack, cancellationToken: CancellationToken.None).t;
                        res ^= obj.DoStuff();
                    }
            }
            return res;
        }

        protected int ReadProtobuf(BufferedStreamReaderConfig config_r)
        {
            int res = 0;
            using (StreamChain sc1 = new StreamChain())
            {
                Stream s = sc1.ComposeChain(File.Open(tmpFilename_PB, FileMode.Open, FileAccess.Read, FileShare.None), config_r);
                do
                {
                    T obj = Serializer.DeserializeWithLengthPrefix<T>(s, PrefixStyle.Base128, 1);
                    if (obj == null)
                        break;
                    res ^= obj.DoStuff();
                } while (true);
            }
            return res;
        }

        protected async Task<int> ReadMsgPackMTAsync(int totWorkerThreads, BufferedStreamReaderConfig config_r)
        {
            int res = 0;
            using (StreamChain sc1 = new StreamChain())
            {
                Stream s = sc1.ComposeChain(File.Open(tmpFilename_MP, FileMode.Open, FileAccess.Read, FileShare.None), config_r);
                using (var dmp = deserializerFactory_MP(totWorkerThreads))
                    await foreach (var item in dmp.DeserializeAsync(s))
                        res ^= item.t.DoStuff();
            }
            return res;
        }


        protected async Task<int> ReadProtobufMTAsync(int totWorkerThreads, BufferedStreamReaderConfig config_r)
        {
            int res = 0;
            using (StreamChain sc = new StreamChain())
            {
                Stream s = sc.ComposeChain(File.Open(tmpFilename_PB, FileMode.Open, FileAccess.Read, FileShare.None), config_r);
                using (var dmp = deserializerFactory_PB(totWorkerThreads))
                    await foreach (var item in dmp.DeserializeAsync(s))
                        res ^= item.t.DoStuff();
            }
            return res;
        }

        protected async Task WriteMsgPackAsync(BufferedStreamWriterConfig sw_cfg)
        {
            using (StreamChain sc = new StreamChain())
            {
                Stream s = sc.ComposeChain(File.Open(tmpFilename_MP, FileMode.Create, FileAccess.Write, FileShare.None), sw_cfg);
                for (int ix = 0; ix < objArr.Length; ix++)
                {
                    T obj = objArr[ix];
                    await MessagePackSerializer.SerializeAsync(s, new MessagePackItemWrapper<T>(obj));
                }
            }
        }

        protected void WriteProtobuf(BufferedStreamWriterConfig sw_cfg)
        {
            using (StreamChain sc = new StreamChain())
            {
                Stream s = sc.ComposeChain(File.Open(tmpFilename_PB, FileMode.Create, FileAccess.Write, FileShare.None), sw_cfg);
                foreach (T obj in objArr)
                    Serializer.SerializeWithLengthPrefix(s, obj, PrefixStyle.Base128, 1);
            }
        }

        protected async Task WriteMsgPackMTAsync(int totWorkerThreads, BufferedStreamWriterConfig sw_cfg)
        {
            using (StreamChain sc = new StreamChain())
            {
                Stream s = sc.ComposeChain(File.Open(tmpFilename_MP, FileMode.Create, FileAccess.Write, FileShare.None), sw_cfg);
                using (slStreamUtils.MultiThreadedSerialization.CollectionSerializer<T> smp = serializerFactory_MP(totWorkerThreads, s))
                    foreach (var obj in objArr)
                        await smp.SerializeAsync(new MessagePackItemWrapper<T>(obj));
            }
        }

        protected async Task WriteProtobufMTAsync(int totWorkerThreads, BufferedStreamWriterConfig sw_cfg)
        {
            using (StreamChain sc = new StreamChain())
            {
                Stream s = sc.ComposeChain(File.Open(tmpFilename_PB, FileMode.Create, FileAccess.Write, FileShare.None), sw_cfg);
                using (slStreamUtils.MultiThreadedSerialization.CollectionSerializer<T> smp = serializerFactory_PB(totWorkerThreads, s))
                    foreach (var obj in objArr)
                        await smp.SerializeAsync(new ProtobufItemWrapper<T>(obj));
            }
        }

        #region helper methods
        private void DelTempFiles()
        {
            if (File.Exists(tmpFilename_MP))
                File.Delete(tmpFilename_MP);
            if (File.Exists(tmpFilename_PB))
                File.Delete(tmpFilename_PB);
        }

        private T[] GetRandInstanceArr(int len1, int arrayLen)
        {
            RandHelper helper = new RandHelper();
            T[] res = new T[arrayLen];
            for (int f = 0; f < arrayLen; f++)
                res[f] = new T().GetRandInstance(helper, len1);
            return res;
        }

        private async Task<long> SerializeInitialFileAsync(string tmpFileName,
            Func<int, Stream, slStreamUtils.MultiThreadedSerialization.CollectionSerializer<T>> serializerFactory,
            Func<T, ItemWrapper<T>> itemWrapperFactory)
        {
            using (Stream s_w = File.Open(tmpFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            using (slStreamUtils.MultiThreadedSerialization.CollectionSerializer<T> smp = serializerFactory(4, s_w))
                foreach (var obj in objArr)
                    await smp.SerializeAsync(itemWrapperFactory(obj));
            return new FileInfo(tmpFileName).Length;
        }

        #endregion
    }

    public sealed class StreamChain : IDisposable
    {
        List<IDisposable> itemsToDispose = new List<IDisposable>();
        private bool disposedValue;


        public Stream ComposeChain(Stream s, BufferedStreamReaderConfig sr_cfg)
        {
            itemsToDispose.Add(s);
            if (sr_cfg != null)
                itemsToDispose.Add(s = new BufferedStreamReader(s, sr_cfg));
            return s;
        }

        public Stream ComposeChain(Stream s, BufferedStreamWriterConfig sw_cfg)
        {
            itemsToDispose.Add(s);
            if (sw_cfg != null)
                itemsToDispose.Add(s = new BufferedStreamWriter(s, sw_cfg));
            return s;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    for (int f = itemsToDispose.Count - 1; f >= 0; f--)
                        itemsToDispose[f]?.Dispose();
                    itemsToDispose.Clear();
                }
                disposedValue = true;
            }
            itemsToDispose = null;
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}