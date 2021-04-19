/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using NUnit.Framework;
using slStreamUtils;
using slStreamUtilsMessagePack;

namespace slStreamUtilsMessagePackTest
{

    [TestFixture]
    public class ParallelSerializationTest
    {
        MessagePackSerializerOptions mpOptionsSerial;


        [SetUp]
        public void Setup()
        {
            mpOptionsSerial = MessagePackSerializerOptions.Standard;
        }

        private FrameParallelOptions GetOptionsParallel(int totThreads, bool throwOnUnnasignedFrameDeserialization , int? hintAvgFrameSize )
        {
            return new FrameParallelOptions(new FrameFormatterSerializationOptions(
                new BatchSizeEstimatorConfig(hintAvgFrameSize_bytes: hintAvgFrameSize),
                new FIFOWorkerConfig(maxConcurrentTasks: totThreads),
                new MultiThreadedWorkerConfig(totThreads: totThreads),
                throwOnUnnasignedFrameDeserialization: throwOnUnnasignedFrameDeserialization),
                mpOptionsSerial.WithResolver(FrameResolverPlusStandarResolver.Instance));
        }

        [Test]
        public void Serialize_Parallel1Threads_MatchesParallelManyThreads(
            [Values(2, 10, 20)] int totThreads, [Values(true, false)] bool throwOnUnnasignedFrameDeserialization , [Values(null, 10)] int? hintAvgFrameSize)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(OuterContainerSampleType.LargeLst1SometimesNull);
            byte[] msgpackBytes_Thr0 = MessagePackSerializer.Serialize(ocOriginal, GetOptionsParallel(1, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize));

            byte[] msgpackBytes_ThrX = MessagePackSerializer.Serialize(ocOriginal, GetOptionsParallel(totThreads, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize));

            Assert.AreEqual(msgpackBytes_Thr0, msgpackBytes_ThrX);
        }


        [Test]
        public void Deserialize_Parallel1Threads_MatchesParallelManyThreads(
            [Values(1, 2, 5, 10, 20)] int totThreads, [Values(true, false)] bool throwOnUnnasignedFrameDeserialization, [Values(null, 10)] int? hintAvgFrameSize)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(OuterContainerSampleType.LargeLst1SometimesNull);
            byte[] msgpackBytes_Thr0 = MessagePackSerializer.Serialize(ocOriginal, GetOptionsParallel(1, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize));

            var ocThrX = MessagePackSerializer.Deserialize<TestOuterContainer>(msgpackBytes_Thr0, GetOptionsParallel(totThreads, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize));

            Assert.AreEqual(ocOriginal, ocThrX);
        }

        [Test]
        public void Serialize_Parallel_SerialDeserializationsMatch([Values] OuterContainerSampleType sampleType, 
            [Values(1, 2, 3)] int totThreads, [Values(true, false)] bool throwOnUnnasignedFrameDeserialization, [Values(null, 10)] int? hintAvgFrameSize)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(sampleType);
            
            byte[] msgpackBytes = MessagePackSerializer.Serialize(ocOriginal, GetOptionsParallel(totThreads, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize));

            TestOuterContainer ocFinal = MessagePackSerializer.Deserialize<TestOuterContainer>(msgpackBytes, mpOptionsSerial);
            Assert.AreEqual(ocOriginal, ocFinal);
        }


        [Test]
        public void Serialize_Serial_SerialDeserializationsMatch([Values] OuterContainerSampleType sampleType,
            [Values(true, false)] bool throwOnUnnasignedFrameDeserialization, [Values(null, 10)] int? hintAvgFrameSize)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(sampleType);

            byte[] msgpackBytes = MessagePackSerializer.Serialize(ocOriginal, mpOptionsSerial);

            TestOuterContainer ocFinal = MessagePackSerializer.Deserialize<TestOuterContainer>(msgpackBytes, mpOptionsSerial);
            Assert.AreEqual(ocOriginal, ocFinal);
        }

        [Test]
        public void Deserialize_Parallel_FromSerialMatchesOriginal([Values] OuterContainerSampleType sampleType, 
            [Values(1, 2, 3)] int totThreads, [Values(true, false)] bool throwOnUnnasignedFrameDeserialization, [Values(null, 10)] int? hintAvgFrameSize)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(sampleType);
            var mpOptionsParallel = GetOptionsParallel(totThreads, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize);
            byte[] msgpackBytes = MessagePackSerializer.Serialize(ocOriginal, mpOptionsParallel);

            TestOuterContainer ocFinal = MessagePackSerializer.Deserialize<TestOuterContainer>(msgpackBytes, mpOptionsParallel);

            Assert.AreEqual(ocOriginal, ocFinal);
        }


        [Test]
        public void Deserialize_Parallel_FromParallelMatchesOriginal([Values] OuterContainerSampleType sampleType, 
            [Values(1, 2, 3)] int totThreads, [Values(true, false)] bool throwOnUnnasignedFrameDeserialization, [Values(null, 10)] int? hintAvgFrameSize)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(sampleType);
            FrameParallelOptions mpOptionsParallel = GetOptionsParallel(totThreads, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize);
            byte[] msgpackBytes = MessagePackSerializer.Serialize(ocOriginal, mpOptionsParallel);

            TestOuterContainer ocFinal = MessagePackSerializer.Deserialize<TestOuterContainer>(msgpackBytes, mpOptionsParallel);

            Assert.AreEqual(ocOriginal, ocFinal);
        }



        [Test]
        public void Deserialize_Serial_FromSerialMatchesOriginal([Values] OuterContainerSampleType sampleType)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(sampleType);
            byte[] msgpackBytes = MessagePackSerializer.Serialize(ocOriginal, mpOptionsSerial);

            TestOuterContainer ocFinal = MessagePackSerializer.Deserialize<TestOuterContainer>(msgpackBytes, mpOptionsSerial);

            Assert.AreEqual(ocOriginal, ocFinal);
        }



        [Test]
        public void Deserialize_Serial_FromParallelMatchesOriginal([Values] OuterContainerSampleType sampleType, 
            [Values(1, 2, 3)] int totThreads, [Values(true, false)] bool throwOnUnnasignedFrameDeserialization, [Values(null, 10)] int? hintAvgFrameSize)
        {
            TestOuterContainer ocOriginal = SampleCreator.GetOuterContainerSample(sampleType);
                        byte[] msgpackBytes = MessagePackSerializer.Serialize(ocOriginal, GetOptionsParallel(totThreads, throwOnUnnasignedFrameDeserialization, hintAvgFrameSize));

            TestOuterContainer ocFinal = MessagePackSerializer.Deserialize<TestOuterContainer>(msgpackBytes, mpOptionsSerial);

            Assert.AreEqual(ocOriginal, ocFinal);
        }


    }

    [TestFixture]
    public class FrameTest
    {

        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void Equals_Obj_Matches()
        {
            Obj obj1 = new Obj() { i = 1 };
            Obj obj2 = new Obj() { i = 1 };
            Frame<Obj> pi1 = obj1;
            Frame<Obj> pi2 = obj2;

            Assert.AreEqual(pi1, pi2);
        }

        [Test]
        public void Equals_ObjEq_Matches()
        {
            ObjEq obj1 = new ObjEq() { i = 1 };
            ObjEq obj2 = new ObjEq() { i = 1 };
            Frame<ObjEq> pi1 = obj1;
            Frame<ObjEq> pi2 = obj2;

            Assert.AreEqual(pi1, pi2);
        }

        [Test]
        public void Equals_ObjObjEq_Matches()
        {
            ObjEq obj1 = new ObjEq() { i = 1 };
            ObjEq obj2 = new ObjEq() { i = 1 };
            object pi1 = new Frame<ObjEq>(obj1);
            object pi2 = new Frame<ObjEq>(obj2);
            Assert.IsTrue(pi1.Equals(pi2));
        }

        [Test]
        public void Equals_ObjObjEq_Differs()
        {
            ObjEq obj1 = new ObjEq() { i = 1 };
            ObjEq obj2 = new ObjEq() { i = 2 };
            object pi1 = new Frame<ObjEq>(obj1);
            object pi2 = new Frame<ObjEq>(obj2);
            Assert.IsFalse(pi1.Equals(pi2));
        }
        [Test]
        public void Equals_ObjObj_Differs()
        {
            Obj obj1 = new Obj() { i = 1 };
            ObjEq obj2 = new ObjEq() { i = 2 };
            object pi1 = new Frame<Obj>(obj1);
            object pi2 = new Frame<ObjEq>(obj2);
            Assert.IsFalse(pi1.Equals(pi2));
        }
        [Test]
        public void ToString_DoesntThrow()
        {
            Obj obj1 = new Obj() { i = 1 };
            object pi1 = new Frame<Obj>(obj1);

            Assert.DoesNotThrow(() => pi1.ToString());
        }
        [Test]
        public void ToString_NullDoesntThrow()
        {
            Obj obj1 = null;
            object pi1 = new Frame<Obj>(obj1);

            Assert.DoesNotThrow(() => pi1.ToString());
        }

        [Test]
        public void Assign_MatchesInner()
        {
            ObjEq obj1 = new ObjEq() { i = 1 };
            Frame<ObjEq> pi1 = obj1;

            ObjEq obj2 = pi1;

            Assert.AreEqual(obj2, pi1.Item);
        }

        [Test]
        public void GetHashCode_MatchesInner()
        {
            int i = 1;
            Frame<ObjEq> pi1 = new ObjEq() { i = i };

            int hc = pi1.GetHashCode();

            Assert.AreEqual(i.GetHashCode(), hc);
        }
        [Test]
        public void GetHashCode_MatchesInnerNull()
        {
            Frame<ObjEq> pi1 = null;

            int hc = pi1.GetHashCode();

            Assert.AreEqual(-1.GetHashCode(), hc);
        }

    }
}
