/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using System;
using System.Diagnostics.CodeAnalysis;

[module: CompatibilityLevel(CompatibilityLevel.Level300)]
namespace slStreamUtilsProtobufExamples
{
    [ProtoContract]
    public class X : IEquatable<X>
    {
        [ProtoMember(1)]
        public int i1;
        [ProtoMember(2)]
        public bool b1;
        [ProtoMember(3)]
        public long l1;
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is X objX)
                return Equals(objX);
            return false;
        }
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

        public override int GetHashCode()
        {
            return i1.GetHashCode() ^
                b1.GetHashCode() ^
                l1.GetHashCode();
        }
    }

}
