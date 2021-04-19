/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using ProtoBuf;
using System;
using System.Diagnostics.CodeAnalysis;

[module: CompatibilityLevel(CompatibilityLevel.Level300)]
namespace BlogExamples
{
    [MessagePackObject, ProtoContract]
    public class X : IEquatable<X>
    {
        [ProtoMember(1), Key(0)]
        public int i1;
        [ProtoMember(2), Key(1)]
        public bool b1;
        [ProtoMember(3), Key(2)]
        public long l1;

        public bool Equals([AllowNull] X other)
        {
            if (other == null)
                return false;
            if (other.i1 != i1)
                return false;
            if (other.b1 != b1)
                return false;
            if (other.l1 != l1)
                return false;
            return true;
        }
    }
}
