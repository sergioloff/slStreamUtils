using System;
using System.IO;
using slStreamUtilsMessagePackBenchmark;
using MessagePack;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using slStreamUtilsMessagePack;
using System.Threading.Tasks;
using slStreamUtils;
using slStreamUtils.Streams;

namespace ScratchPad
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int totThreads = 5;
            MemoryStream ms = new MemoryStream();
            //TestClassLarge[] objs = slStreamUtilsMessagePackBenchmark.CollectionSerialization.BenchmarkLogic<TestClassLarge>.GetRandInstanceArr(128, 2 * 2048 * 16 * 4);
            TestClassLargeParallel obj = slStreamUtilsMessagePackBenchmark.ParallelSerialization.BenchmarkLogicHelper.GetRandInstance<TestClassLargeParallel>(128, 2 * 2048 * 16 * 4);
            var aai = GC.GetGCMemoryInfo();
            Console.WriteLine("Concurrent=" + aai.Concurrent);
            Console.WriteLine("HeapSizeBytes=" + aai.HeapSizeBytes);
            Console.WriteLine("PauseTimePercentage=" + aai.PauseTimePercentage);
            Console.WriteLine("PinnedObjectsCount=" + aai.PinnedObjectsCount);
            Console.WriteLine("TotalCommittedBytes=" + aai.TotalCommittedBytes);

            string fn = @"c:\lixo\pokaralho.dat";

            FileHelper.FlushFileCache(fn);
            var optsParallel = new FrameParallelOptions(totThreads, MessagePackSerializerOptions.Standard.WithResolver(FrameResolverPlusStandarResolver.Instance));
            Stopwatch sw = Stopwatch.StartNew();
            using (var s = File.Create(fn))
            //using (var bs = new BufferedStreamWriter(s, new BufferedStreamWriterConfig(8)))
            //{
            //    await using (var ser = new CollectionSerializerAsync<TestClassLarge>(bs, new FIFOWorkerConfig(maxConcurrentTasks: totThreads)))
            //        foreach (var item in objs)
            //            await ser.SerializeAsync(item);
            //}
            using (var bs = new BufferedStreamWriter(s, new BufferedStreamWriterConfig(8)))
                await MessagePackSerializer.SerializeAsync(bs, obj, optsParallel);

            Console.WriteLine("write: " + sw.Elapsed);
            FileHelper.FlushFileCache(fn);
            sw.Restart();
            int i = 0;
            using (var s = File.OpenRead(fn))
            using (var bs = new BufferedStreamReader(s, new BufferedStreamReaderConfig(8)))
                //{
                //    using (var ser = new CollectionDeserializerAsync<TestClassLarge>(totThreads))
                //        await foreach (var item in ser.DeserializeAsync(bs))
                //        {
                //            i ^= item.Item.i1;
                //        }
                //}
                i = (await MessagePackSerializer.DeserializeAsync<TestClassLargeParallel>(bs, optsParallel)).arr[sw.ElapsedTicks%2].BufferLength;

            Console.WriteLine("read: " + sw.Elapsed); sw.Restart();
            Debug.WriteLine(i);



            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}
