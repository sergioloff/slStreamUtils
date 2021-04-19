/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using BenchmarkDotNet.Running;
using slStreamUtilsMessagePackBenchmark.CollectionSerialization;
using slStreamUtilsMessagePackBenchmark.ParallelSerialization;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace slStreamUtilsMessagePackBenchmark
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            //BenchmarkRunner.Run(typeof(Program).Assembly); 
            //return;
            await TestParallelBenchmark();
            await TestCollectionBenchmark();
            Console.WriteLine("done");
            Console.ReadLine();
        }

        private static async Task TestCollectionBenchmark()
        {
            var br = new CollectionBenchmark();
            await br.GlobalSetup();
            bool usingMemoryStream = true;
            Stopwatch sw = Stopwatch.StartNew();
            int i = 0;

            sw.Restart();
            await br.WriteAsync_Baseline(isSmall: true, totalDelayedWriterBlocks: -1, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.WriteAsync_Baseline)} baseline={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            i ^= await br.ReadAsync_Baseline(isSmall: true, totalPreFetchBlocks: -1, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.ReadAsync_Baseline)} baseline={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            await br.WriteAsync_Parallel(isSmall: true, totalDelayedWriterBlocks: -1, totWorkerThreads: 6, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.WriteAsync_Parallel)} parallel={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            i ^= await br.ReadAsync_Parallel(isSmall: true, totalPreFetchBlocks: -1, totWorkerThreads: 6, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.ReadAsync_Parallel)} parallel={sw.ElapsedMilliseconds} ms");

            br.GlobalCleanup();
        }

        private static async Task TestParallelBenchmark()
        {
            var br = new ParallelBenchmark();
            await br.GlobalSetup();
            bool usingMemoryStream = true;
            Stopwatch sw = Stopwatch.StartNew();
            int i = 0;

            sw.Restart();
            await br.WriteAsync_Baseline(isSmall: true, totalDelayedWriterBlocks: -1, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.WriteAsync_Baseline)} baseline={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            i ^= await br.ReadAsync_Baseline(isSmall: true, totalPreFetchBlocks: -1, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.ReadAsync_Baseline)} baseline={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            await br.WriteAsync_Parallel(isSmall: true, totalDelayedWriterBlocks: -1, totWorkerThreads: 6, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.WriteAsync_Parallel)} parallel={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            i ^= await br.ReadAsync_Parallel(isSmall: true, totalPreFetchBlocks: -1, totWorkerThreads: 6, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.ReadAsync_Parallel)} parallel={sw.ElapsedMilliseconds} ms");

            br.GlobalCleanup();
        }
    }
}
