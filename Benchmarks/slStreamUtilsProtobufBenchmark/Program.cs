/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtilsProtobufBenchmark.CollectionSerialization;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace slStreamUtilsProtobufBenchmark
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            await TestCollectionBenchmark();
            Console.WriteLine("done");
            Console.ReadLine();
        }

        private static async Task TestCollectionBenchmark()
        {
            var br = new ProtobufCollectionBenchmark();
            await br.GlobalSetup();
            bool usingMemoryStream = true;
            Stopwatch sw = Stopwatch.StartNew();
            int i = 0;
            sw.Restart();
            i ^= br.Read_Baseline(isSmall: true, totalPreFetchBlocks: 4, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.Read_Baseline)} baseline={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            br.Write_Baseline(isSmall: true, totalDelayedWriterBlocks: 4, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.Write_Baseline)} baseline={sw.ElapsedMilliseconds} ms");

            Console.WriteLine();

            sw.Restart();
            i ^= await br.ReadAsync_Parallel(isSmall: true, totalPreFetchBlocks: 4, totWorkerThreads: 4, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.ReadAsync_Parallel)} parallel={sw.ElapsedMilliseconds} ms");

            sw.Restart();
            await br.WriteAsync_Parallel(isSmall: true, totalDelayedWriterBlocks: 4, totWorkerThreads: 4, usingMemoryStream: usingMemoryStream);
            sw.Stop();
            Console.WriteLine($"{nameof(br.WriteAsync_Parallel)} parallel={sw.ElapsedMilliseconds} ms");

            br.GlobalCleanup();
        }
    }
    }
