/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using slStreamUtils.FIFOWorker;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using slProto = slStreamUtils.MultiThreadedSerialization.Protobuf;
using slMsgPack = slStreamUtils.MultiThreadedSerialization.MessagePack;

namespace DataExporterToPython
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int totItems = 4;
            TestStructSmall1[] originalArray = new TestStructSmall1[totItems];
            for (int f = 1; f <= totItems; f++)
                originalArray[f - 1] = new TestStructSmall1() { b1 = f % 2 == 0, l1 = DateTime.Now.AddSeconds(f).Ticks, i1 = f, i2 = -f };

            string protostr = ProtoBuf.Serializer.GetProto<slProto.ProtobufColectionWrapper<TestStructSmall1>>();
            string pythonProj = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName, @"Examples\PythonDeserializationTest");
            File.WriteAllText(Path.Combine(pythonProj, "TestStructSmall1.proto"), protostr);

            using (var fw = File.Create(Path.Combine(pythonProj, "TestStructSmall1_proto.dat")))
            await using (var ser = new slProto.CollectionSerializer<TestStructSmall1>(fw, new FIFOWorkerConfig(0)))
                foreach (var item in originalArray)
                    await ser.SerializeAsync(new slProto.ProtobufItemWrapper<TestStructSmall1>(item));

            using (var fw = File.Create(Path.Combine(pythonProj, "TestStructSmall1_msgPack.dat")))
            await using (var ser = new slMsgPack.CollectionSerializer<TestStructSmall1>(fw, new FIFOWorkerConfig(0), MessagePackSerializerOptions.Standard))
                foreach (var t in originalArray)
                    await ser.SerializeAsync(new slMsgPack.MessagePackItemWrapper<TestStructSmall1>(t));

            Console.WriteLine("Saved data files. Now use the python project 'PythonDeserializationTest' to load the serialized data.");
            Console.ReadLine();
        }
    }
}
