using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;

namespace slStreamUtilsBenchmarkLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var cfg = DefaultConfig.Instance.AddJob(Job.Default
                    .WithRuntime(CoreRuntime.Core50)
                    .WithPlatform(Platform.X64)
                    .WithJit(Jit.RyuJit)
                    .WithGcServer(true));

            BenchmarkRunner.Run(new[]{
                BenchmarkConverter.TypeToBenchmarks( typeof(slStreamUtilsMessagePackBenchmark.ParallelSerialization.MessagePackParallelBenchmark), cfg),
                BenchmarkConverter.TypeToBenchmarks( typeof(slStreamUtilsMessagePackBenchmark.CollectionSerialization.MessagePackCollectionBenchmark), cfg),
                BenchmarkConverter.TypeToBenchmarks( typeof(slStreamUtilsProtobufBenchmark.CollectionSerialization.ProtobufCollectionBenchmark), cfg),
            });

        }
    }
}
