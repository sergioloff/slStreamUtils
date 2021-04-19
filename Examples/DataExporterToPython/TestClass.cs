/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using ProtoBuf;
using slStreamUtilsMessagePack;

[module: CompatibilityLevel(CompatibilityLevel.Level300)]
namespace DataExporterToPython
{
    [MessagePackObject, ProtoContract]
    public class TestClass
    {
        [ProtoMember(1), Key(0)]
        public int i1;
        [ProtoMember(2), Key(1)]
        public int i2;
        [ProtoMember(3), Key(2)]
        public bool b1;
        [ProtoMember(4), Key(3)]
        public long l1;
    }

    [MessagePackObject]
    public class TestClassMPContainer
    {
        [Key(0)]
        public Frame<TestClass>[] arr;
    }
}
