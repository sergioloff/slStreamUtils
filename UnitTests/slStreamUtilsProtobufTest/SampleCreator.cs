/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using slStreamUtilsProtobuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace slStreamUtilsProtobufTest
{
    [ProtoContract]
    public class TestItemProto
    {
        [ProtoMember(1)]
        public byte f;
    }

    [ProtoContract]
    public class TestItemPB
    {
        [ProtoMember(1)]
        public byte f;
    }

    [ProtoContract]
    public class TestItemPBLarge
    {
        [ProtoMember(1)]
        public int f;
        [ProtoMember(2)]
        public string st;
    }

    public enum OuterContainerSampleType
    {
        Lst1SometimesNull,
        Lst1Null,
        Arr1Null,
        Null,
        Arr1Empty_Lst1Null,
        Arr1Null_Lst1Empty,
        LargeLst1SometimesNull
    }

    [ProtoContract]
    public class TestOuterContainer : IEquatable<TestOuterContainer>
    {
        [ProtoMember(1)]
        public int i1;
        [ProtoMember(2)]
        public int[] arr1;
        [ProtoMember(3)]
        public int i2;
        [ProtoMember(4)]
        public List<TestInnerContainer> lst1;
        [ProtoMember(5)]
        public int i3;
        public override string ToString()
        {
            return
                $"{nameof(i1)}={i1} {nameof(i2)}={i2} {nameof(i3)}={i3} " +
                $"{nameof(arr1)}=[{(arr1 == null ? "N/A" : string.Join(",", arr1.Select(f => f.ToString())))}] " +
                $"{nameof(lst1)}=[{(lst1 == null ? "N/A" : string.Join(",", lst1.Select(f => f.ToString())))}] ";
        }
        public bool Equals(TestOuterContainer other)
        {
            if (i1 != other.i1 || i2 != other.i2 || i3 != other.i3)
                return false;
            if ((arr1 == null) != (other.arr1 == null))
                return false;
            if (arr1 != null)
            {
                if (arr1.Length != other.arr1.Length)
                    return false;
                for (int i = 0; i < arr1.Length; i++)
                    if (arr1[i] != other.arr1[i])
                        return false;
            }
            if ((lst1 == null) != (other.lst1 == null))
                return false;
            if (lst1 != null)
            {
                if (lst1.Count != other.lst1.Count)
                    return false;
                for (int i = 0; i < lst1.Count; i++)
                {
                    if ((lst1[i] == null) != (other.lst1[i] == null))
                        return false;
                    if (lst1[i] != null)
                        if (!lst1[i].Equals(other.lst1[i]))
                            return false;
                }
            }

            return true;
        }
    }

    [ProtoContract]
    public class TestInnerContainer : IEquatable<TestInnerContainer>
    {
        [ProtoMember(1)]
        public int i1;
        [ProtoMember(2)]
        public int[] arr1;
        [ProtoMember(3)]
        public int i2;

        public override string ToString()
        {
            return
                $"{nameof(i1)}={i1} {nameof(i2)}={i2} " +
                $"{nameof(arr1)}=[{(arr1 == null ? "N/A" : string.Join(",", arr1.Select(f => f.ToString())))}] ";
        }

        public override bool Equals(object obj)
        {
            if (obj is TestInnerContainer pObj)
                return Equals(pObj);
            return false;
        }

        public bool Equals(TestInnerContainer other)
        {
            if (i1 != other.i1 || i2 != other.i2)
                return false;
            if ((arr1 == null) != (other.arr1 == null))
                return false;
            if (arr1 != null)
            {
                if (arr1.Length != other.arr1.Length)
                    return false;
                for (int i = 0; i < arr1.Length; i++)
                    if (arr1[i] != other.arr1[i])
                        return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return i1.GetHashCode() ^ i2.GetHashCode() ^ 
                (arr1 == null ? 0 : arr1.Select(f => f.GetHashCode()).Aggregate((f, g) => f ^ g));
        }
    }


    public class ObjEq : IEquatable<ObjEq>
    {
        public int i;

        public bool Equals(ObjEq other)
        {
            if (other == null)
                return false;
            return other.i == i;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is ObjEq))
                return false;
            return Equals((ObjEq)obj);
        }

        public override int GetHashCode()
        {
            return i.GetHashCode();
        }
    }
    public class Obj
    {
        public int i;
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Obj))
                return false;
            return ((Obj)obj).i == i;
        }
        public override int GetHashCode()
        {
            return i.GetHashCode();
        }
    }
}
