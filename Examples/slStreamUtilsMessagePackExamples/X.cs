/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using slStreamUtilsMessagePack;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace slStreamUtilsMessagePackExamples
{
    [MessagePackObject]
    public class X : IEquatable<X>
    {
        [Key(0)]
        public int i1;
        [Key(1)]
        public bool b1;
        [Key(2)]
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

    [MessagePackObject]
    public class ArrayX : IEquatable<ArrayX>
    {
        [Key(0)]
        public Frame<X>[] arr;

        public bool Equals(ArrayX other)
        {
            if (other == null)
                return false;
            return arr.SequenceEqual(other.arr);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is ArrayX objArrayX)
                return Equals(objArrayX);
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
