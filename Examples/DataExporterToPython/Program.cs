/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using slStreamUtils;
using System;
using System.IO;
using System.Threading.Tasks;
using slProto = slStreamUtilsProtobuf;
using slMsgPack = slStreamUtilsMessagePack;
using System.Linq;

namespace DataExporterToPython
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int totItems = 4;
            TestClass[] originalArray = new TestClass[totItems];
            for (int f = 1; f <= totItems; f++)
                originalArray[f - 1] = new TestClass() { b1 = f % 2 == 0, l1 = DateTime.Now.AddSeconds(f).Ticks, i1 = f, i2 = -f };
            TestClassMPContainer container = new TestClassMPContainer() { arr = originalArray.Select(f => (slMsgPack.Frame<TestClass>)f).ToArray() };

            string protostr = ProtoBuf.Serializer.GetProto<slProto.ParallelServices_ArrayWrapper<TestClass>>();
            string pythonProj = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName, @"Examples\PythonDeserializationTest");
            File.WriteAllText(Path.Combine(pythonProj, "TestClass.proto"), protostr);

            using (var fw = File.Create(Path.Combine(pythonProj, "TestClassCollection_proto.dat")))
            await using (var ser = new slProto.CollectionSerializerAsync<TestClass>(fw, new FIFOWorkerConfig(1)))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(item);

            using (var fw = File.Create(Path.Combine(pythonProj, "TestClassCollection_msgPack.dat")))
            await using (var ser = new slMsgPack.CollectionSerializerAsync<TestClass>(fw, new FIFOWorkerConfig(1)))
                foreach (var t in originalArray)
                    await ser.SerializeAsync(new slMsgPack.Frame<TestClass>(t));

            var opts = new slMsgPack.FrameParallelOptions(2, MessagePackSerializerOptions.Standard.WithResolver(slMsgPack.FrameResolverPlusStandarResolver.Instance));
            using (var fw = File.Create(Path.Combine(pythonProj, "TestClassContainer_msgPack.dat")))
                await MessagePackSerializer.SerializeAsync(fw, container, opts);

            Console.WriteLine("Saved data files. Now use the python project 'PythonDeserializationTest' to load the serialized data.");
            Console.ReadLine();
        }
    }
}
