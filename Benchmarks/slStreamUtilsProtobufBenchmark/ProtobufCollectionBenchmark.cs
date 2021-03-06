/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using BenchmarkDotNet.Attributes;
using slStreamUtils.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using slStreamUtils;
using System.Linq;
using ProtoBuf;
using slStreamUtilsProtobuf;
using ProtoBuf.Meta;

namespace slStreamUtilsProtobufBenchmark.CollectionSerialization
{

    public class Benchmark_Small_Config
    {
        public const int blockSize = 128;
        public const int totBlocks = 64 * 16 * 4 * 30;
    }
    public class Benchmark_Large_Config
    {
        public const int blockSize = 128;
        public const int totBlocks = 64 * 16 * 4;
    }

    public class ProtobufCollectionBenchmark
    {
        BenchmarkLogic<TestClassSmall> logic_small;
        BenchmarkLogic<TestClassLarge> logic_large;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            logic_small = new BenchmarkLogic<TestClassSmall>
                (blockSize: Benchmark_Small_Config.blockSize, totBlocks: Benchmark_Small_Config.totBlocks);
            logic_large = new BenchmarkLogic<TestClassLarge>
                (blockSize: Benchmark_Large_Config.blockSize, totBlocks: Benchmark_Large_Config.totBlocks);
            await logic_small.SetupAsync();
            await logic_large.SetupAsync();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            logic_small.Cleanup();
            logic_large.Cleanup();
        }

        public int[] TotalPreFetchBlocks_Baseline_Choices = new int[] { -1 };
        public int[] TotalDelayedWriterBlocks_Baseline_Choices = new int[] { -1 };
        public int[] TotalPreFetchBlocks_Parallel_Choices = new int[] { -1 };
        public int[] TotalDelayedWriterBlocks_Parallel_Choices = new int[] { -1 };
        public int[] TotWorkerThreads_Choices = new int[] { 1, 2, 3, 4 };
        public bool[] UsingMemoryStream_Choices = new bool[] { true };
        public bool[] IsSmall_Choices = new bool[] { true, false };

        [Benchmark]
        [ArgumentsSource(nameof(BenchmarkArguments_Read_Baseline))]
        public int Read_Baseline(bool isSmall, int totalPreFetchBlocks, bool usingMemoryStream)
        {
            BufferedStreamReaderConfig config = totalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: totalPreFetchBlocks) : null;
            if (isSmall)
                return logic_small.Read_Baseline(config, usingMemoryStream);
            else
                return logic_large.Read_Baseline(config, usingMemoryStream);
        }
        public IEnumerable<object[]> BenchmarkArguments_Read_Baseline()
        {
            foreach (bool usingMemoryStream in UsingMemoryStream_Choices)
                foreach (bool isSmall in IsSmall_Choices)
                    foreach (int totalPreFetchBlocks in TotalPreFetchBlocks_Baseline_Choices)
                        yield return new object[] { isSmall, totalPreFetchBlocks, usingMemoryStream };
        }

        [Benchmark]
        [ArgumentsSource(nameof(BenchmarkArguments_Write_Baseline))]
        public void Write_Baseline(bool isSmall, int totalDelayedWriterBlocks, bool usingMemoryStream)
        {
            BufferedStreamWriterConfig config = totalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: totalDelayedWriterBlocks) : null;
            if (isSmall)
                logic_small.Write_Baseline(config, usingMemoryStream);
            else
                logic_large.Write_Baseline(config, usingMemoryStream);
        }
        public IEnumerable<object[]> BenchmarkArguments_Write_Baseline()
        {
            foreach (bool usingMemoryStream in UsingMemoryStream_Choices)
                foreach (bool isSmall in IsSmall_Choices)
                    foreach (int totalDelayedWriterBlocks in TotalDelayedWriterBlocks_Baseline_Choices)
                        yield return new object[] { isSmall, totalDelayedWriterBlocks, usingMemoryStream };
        }

        [Benchmark]
        [ArgumentsSource(nameof(BenchmarkArguments_Read_Parallel))]
        public async Task<int> ReadAsync_Parallel(bool isSmall, int totalPreFetchBlocks, int totWorkerThreads, bool usingMemoryStream)
        {
            BufferedStreamReaderConfig config = totalPreFetchBlocks >= 0 ? new BufferedStreamReaderConfig(totalPreFetchBlocks: totalPreFetchBlocks) : null;
            if (isSmall)
                return await logic_small.ReadAsync_Parallel(totWorkerThreads, config, usingMemoryStream);
            else
                return await logic_large.ReadAsync_Parallel(totWorkerThreads, config, usingMemoryStream);
        }
        public IEnumerable<object[]> BenchmarkArguments_Read_Parallel()
        {
            foreach (bool usingMemoryStream in UsingMemoryStream_Choices)
                foreach (bool isSmall in IsSmall_Choices)
                    foreach (int totalPreFetchBlocks in TotalPreFetchBlocks_Parallel_Choices)
                        foreach (int totWorkerThreads in TotWorkerThreads_Choices)
                            yield return new object[] { isSmall, totalPreFetchBlocks, totWorkerThreads, usingMemoryStream };
        }

        [Benchmark]
        [ArgumentsSource(nameof(BenchmarkArguments_Write_Parallel))]
        public async Task WriteAsync_Parallel(bool isSmall, int totalDelayedWriterBlocks, int totWorkerThreads, bool usingMemoryStream)
        {
            BufferedStreamWriterConfig config = totalDelayedWriterBlocks >= 0 ? new BufferedStreamWriterConfig(totalDelayedWriterBlocks: totalDelayedWriterBlocks) : null;
            if (isSmall)
                await logic_small.WriteAsync_Parallel(totWorkerThreads, config, usingMemoryStream);
            else
                await logic_large.WriteAsync_Parallel(totWorkerThreads, config, usingMemoryStream);
        }
        public IEnumerable<object[]> BenchmarkArguments_Write_Parallel()
        {
            foreach (bool usingMemoryStream in UsingMemoryStream_Choices)
                foreach (bool isSmall in IsSmall_Choices)
                    foreach (int totalDelayedWriterBlocks in TotalDelayedWriterBlocks_Parallel_Choices)
                        foreach (int totWorkerThreads in TotWorkerThreads_Choices)
                            yield return new object[] { isSmall, totalDelayedWriterBlocks, totWorkerThreads, usingMemoryStream };
        }
    }

    public class BenchmarkLogic<T>
        where T : IDoStuff, IAmRandomInstantiable<T>, IMeasureSizeWithAllignmentPadding, new()
    {
        public int BlockSize { get; private set; }
        public int TotBlocks { get; private set; }
        public T[] objArr { get; private set; }
        public string tmpFilename_baseline;
        public string tmpFilename_parallel;
        public string tmpFilesRoot;
        public MemoryStream ms_baseline;
        public MemoryStream ms_parallel;
        public TypeModel model;

        public BenchmarkLogic(int blockSize, int totBlocks)
        {
            BlockSize = blockSize;
            TotBlocks = totBlocks;
        }

        public async Task SetupAsync()
        {
            RuntimeTypeModel rtModel = RuntimeTypeModel.Create();
            rtModel.Add(typeof(TestStructSmall1));
            rtModel.Add(typeof(TestStructSmall2));
            rtModel.Add(typeof(TestStructLarge1));
            rtModel.Add(typeof(TestClassSmall));
            rtModel.Add(typeof(TestClassLarge));
            rtModel.RegisterParallelServices<TestClassSmall>();
            rtModel.RegisterParallelServices<TestClassLarge>();
            model = rtModel.Compile();

            tmpFilesRoot = Path.Combine(Path.GetTempPath(), "slStreamBenchmarks");
            if (!Directory.Exists(tmpFilesRoot))
                Directory.CreateDirectory(tmpFilesRoot);
            tmpFilename_baseline = Path.Combine(tmpFilesRoot, $"tmp_PB_PS_baseline_{typeof(T).Name}.dat");
            tmpFilename_parallel = Path.Combine(tmpFilesRoot, $"tmp_PB_PS_parallel_{typeof(T).Name}.dat");
            objArr = GetRandInstanceArr(BlockSize, TotBlocks);
            ms_baseline = new MemoryStream();
            ms_parallel = new MemoryStream();

            // generate binary files and memstreams to work with
            Write_Baseline(null, usingMemoryStream: true);
            await WriteAsync_Parallel(1, null, usingMemoryStream: true);
            Write_Baseline(null, usingMemoryStream: false);
            await WriteAsync_Parallel(1, null, usingMemoryStream: false);
            var File_size_baseline = new FileInfo(tmpFilename_baseline).Length;
            var File_size_parallel = new FileInfo(tmpFilename_parallel).Length;
            var Mem_size_padded = objArr.Sum(f => f.GetSize()) + objArr.Length * IntPtr.Size;
            var Mem_size_padded_SingleItem = objArr.First().GetSize();
            Console.WriteLine($"{nameof(T)}: {typeof(T)}");
            Console.WriteLine($"Memory size (padded) = {Mem_size_padded / ((double)1024 * 1024):f2} MB");
            Console.WriteLine($"Memory size (padded) for 1 item = {Mem_size_padded_SingleItem / (double)1024:f2} KB");
            Console.WriteLine($"Baseline file length (no framing data) = {File_size_baseline / ((double)1024 * 1024):f2} MB");
            Console.WriteLine($"File length (with framing data) = {File_size_parallel / ((double)1024 * 1024):f2} MB");
        }

        public void Cleanup()
        {
            DelTempFile();
            DelTempFile();
            objArr = null;
        }

        public int Read_Baseline(BufferedStreamReaderConfig config_r, bool usingMemoryStream)
        {
            if (!usingMemoryStream)
                FileHelper.FlushFileCache(tmpFilename_baseline);
            try
            {
                Type t = typeof(T);
                int res = 0;
                using (StreamChain sc1 = new StreamChain())
                {
                    Stream s = sc1.ComposeChain(usingMemoryStream ? ms_baseline :
                        File.Open(tmpFilename_baseline, FileMode.Open, FileAccess.Read, FileShare.None), config_r);
                    do
                    {
                        object obj = model.DeserializeWithLengthPrefix(s, null, t, PrefixStyle.Base128, 1);
                        if (obj is T objT)
                            res ^= objT.DoStuff();
                        else if (obj is null)
                            break;
                        else
                            throw new Exception("unexpected obj type deserialized");
                    } while (true);
                }
                return res;
            }
            finally
            {
                if (usingMemoryStream)
                {
                    ms_baseline.Flush();
                    ms_baseline.Position = 0;
                }
            }
        }

        public void Write_Baseline(BufferedStreamWriterConfig sw_cfg, bool usingMemoryStream)
        {
            if (!usingMemoryStream)
            {
                FileHelper.FlushFileCache(tmpFilename_baseline);
            }
            else
            {
                ms_baseline.SetLength(ms_baseline.Length);
                ms_baseline.Position = 0;
            }
            using (StreamChain sc = new StreamChain())
            {
                Stream s = sc.ComposeChain(
                    usingMemoryStream ? ms_baseline :
                    File.Open(tmpFilename_baseline, FileMode.Create, FileAccess.Write, FileShare.None), sw_cfg);
                Type t = typeof(T);
                foreach (T obj in objArr)
                    model.SerializeWithLengthPrefix(s, obj, t, PrefixStyle.Base128, 1);
            }
            if (usingMemoryStream)
            {
                ms_baseline.Flush();
                ms_baseline.Position = 0;
            }
        }

        public async Task<int> ReadAsync_Parallel(int totWorkerThreads, BufferedStreamReaderConfig config_r, bool usingMemoryStream)
        {
            if (!usingMemoryStream)
                FileHelper.FlushFileCache(tmpFilename_parallel);
            try
            {
                int res = 0;
                using (StreamChain sc1 = new StreamChain())
                {
                    Stream s = sc1.ComposeChain(
                        usingMemoryStream ? ms_parallel :
                        File.Open(tmpFilename_parallel, FileMode.Open, FileAccess.Read, FileShare.None), config_r);
                    using (var dmp = new CollectionDeserializerAsync<T>(new FIFOWorkerConfig(totWorkerThreads), model))
                        await foreach (var i in dmp.DeserializeAsync(s))
                            res ^= i.DoStuff();
                }
                return res;
            }
            finally
            {
                if (usingMemoryStream)
                {
                    ms_parallel.Flush();
                    ms_parallel.Position = 0;
                }
            }
        }

        public async Task WriteAsync_Parallel(int totWorkerThreads, BufferedStreamWriterConfig sw_cfg, bool usingMemoryStream)
        {
            if (!usingMemoryStream)
            {
                FileHelper.FlushFileCache(tmpFilename_baseline);
            }
            else
            {
                ms_parallel.SetLength(ms_parallel.Length);
                ms_parallel.Position = 0;
            }
            FileHelper.FlushFileCache(tmpFilename_parallel);
            using (StreamChain sc = new StreamChain())
            {
                Stream s = sc.ComposeChain(
                    usingMemoryStream ? ms_parallel :
                    File.Open(tmpFilename_parallel, FileMode.Create, FileAccess.Write, FileShare.None), sw_cfg);
                using (CollectionSerializerAsync<T> smp = new CollectionSerializerAsync<T>(s, new FIFOWorkerConfig(totWorkerThreads), model))
                    foreach (var obj in objArr)
                        await smp.SerializeAsync(obj);
            }
            if (usingMemoryStream)
            {
                ms_parallel.Flush();
                ms_parallel.Position = 0;
            }
        }

        #region helper methods
        private void DelTempFile()
        {
            if (Directory.Exists(tmpFilesRoot))
                foreach (var file in new DirectoryInfo(tmpFilesRoot).GetFiles())
                    file.Delete();
        }

        private T[] GetRandInstanceArr(int len1, int arrayLen)
        {
            RandHelper helper = new RandHelper();
            T[] res = new T[arrayLen];
            for (int f = 0; f < arrayLen; f++)
                res[f] = new T().GetRandInstance(helper, len1);
            return res;
        }

        #endregion
    }
}
