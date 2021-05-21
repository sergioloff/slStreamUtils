using System;
using System.IO;
using MessagePack;
using ProtoBuf;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using slStreamUtils;
using slStreamUtils.Streams;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Collections.Generic;
using static ProtoBuf.Meta.RuntimeTypeModel;
using slStreamUtilsProtobuf;

namespace ScratchPad
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ////ola();
            //Y y = new Y() { xlist = new List<X> { new X() { i1 = 2 }, new X() { i1 = 3 } }, frame1 = new slStreamUtilsProtobuf.Frame<int>(223, 222) };
            //MemoryStream ms = new MemoryStream();
            ////RuntimeTypeModel.Default.Serialize(ms, y);
            ////RuntimeTypeModel.Default.SerializeWithLengthPrefix(


            //RuntimeTypeModel myModel = RuntimeTypeModel.Create();
            //myModel.Add<X>();
            //myModel.Add<Y>();
            //myModel.Add<slStreamUtilsProtobuf.Frame<int>>();
            //myModel.RegisterParallel<X>();
            //TypeModel compiledModel = myModel.Compile();



            //compiledModel.Serialize(ms, y);

            //Console.WriteLine(string.Join(",", ms.ToArray()));

            var xar = new XArr<X>() { arr = new X[] { new X() { i1 = 111 }, new X() { i1 = 113 } } };
            RuntimeTypeModel myModel = RuntimeTypeModel.Create();
            myModel.Add<X>();
            myModel.Add<XArr<X>>();
            myModel.Add<Frame<X>>();
            Console.WriteLine(myModel.IsDefined(typeof(XArr<X>)));
            Console.WriteLine(myModel.IsDefined(typeof(Frame<X>)));
            TypeModel compiledModel = myModel.Compile();
            Console.WriteLine(compiledModel.IsDefined(typeof(XArr<X>)));
            Console.WriteLine(compiledModel.IsDefined(typeof(Frame<X>)));


            Console.ReadLine();
        }
        //static void ola()
        //{
        //    //RuntimeTypeModel rtm = RuntimeTypeModel.Create();
        //    RuntimeTypeModel rtm = RuntimeTypeModel.Default;
        //    MetaType ti = rtm.Add(typeof(List<X>), true);
        //    ti.SerializerType = typeof(XListSerializer);
        //    //RuntimeTypeModel rtm = RuntimeTypeModel.Default;
        //    //MetaType a = rtm.Add(typeof(X), true);
        //    //a.SerializerType = typeof(XSerializer);
        //}
    }
    public static class RuntimeTypeModelParallelServicesExtension
    {
        public static void RegisterParallel<T>(this RuntimeTypeModel rtm, bool applyDefaultBehaviour = true, CompatibilityLevel compatibilityLevel = CompatibilityLevel.NotSpecified)
        {
            if (!rtm.IsDefined(typeof(ParallelServices_ListWrapper<T>)))
                rtm.Add(typeof(ParallelServices_ListWrapper<T>), applyDefaultBehaviour, compatibilityLevel);
            if (!rtm.IsDefined(typeof(ParallelServices_ArrayWrapper<T>)))
                rtm.Add(typeof(ParallelServices_ArrayWrapper<T>), applyDefaultBehaviour, compatibilityLevel);
        }
    }

    public class XSerializer : ISerializer<X>
    {
        public SerializerFeatures Features => throw new NotImplementedException();

        public X Read(ref ProtoReader.State state, X value)
        {
            throw new NotImplementedException();
        }

        public void Write(ref ProtoWriter.State state, X value)
        {
            throw new NotImplementedException();
        }
    }

    public class XArrSerializer : ISerializer<X[]>
    {
        public SerializerFeatures Features => throw new NotImplementedException();

        public X[] Read(ref ProtoReader.State state, X[] value)
        {
            throw new NotImplementedException();
        }

        public void Write(ref ProtoWriter.State state, X[] value)
        {
            throw new NotImplementedException();

        }
    }
    public class XListSerializer : ISerializer<List<X>>
    {
        public SerializerFeatures Features => throw new NotImplementedException();

        public List<X> Read(ref ProtoReader.State state, List<X> value)
        {
            throw new NotImplementedException();
        }

        public void Write(ref ProtoWriter.State state, List<X> value)
        {
            throw new NotImplementedException();

        }
    }

    [ProtoContract]
    public class X
    {
        [ProtoMember(1)]
        public int i1 { get; set; }
    }

    [ProtoContract]
    public class XArr<X>
    {
        [ProtoMember(1)]
        public X[] arr { get; set; }
    }
    [ProtoContract]
    public class XList<X>
    {
        [ProtoMember(1)]
        public List<X> list { get; set; }
    }

}
