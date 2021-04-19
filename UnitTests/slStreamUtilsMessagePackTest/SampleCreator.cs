/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using slStreamUtilsMessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace slStreamUtilsMessagePackTest
{
    [MessagePackObject]
    public class TestItemMP
    {
        [Key(0)]
        public byte f;
    }

    [MessagePackObject]
    public class TestItemMPLarge
    {
        [Key(0)]
        public int f;
        [Key(1)]
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

    [MessagePackObject]
    public class TestOuterContainer : IEquatable<TestOuterContainer>
    {
        [Key(0)]
        public int i1;
        [Key(1)]
        public Frame<int>[] arr1;
        [Key(2)]
        public int i2;
        [Key(3)]
        public List<Frame<TestInnerContainer>> lst1;
        [Key(4)]
        public int i3;
        [Key(5)]
        public Frame<TestInnerContainer> singleFrame1;
        public override string ToString()
        {
            return
                $"{nameof(i1)}={i1} {nameof(i2)}={i2} {nameof(i3)}={i3} " +
                $"{nameof(singleFrame1)}={singleFrame1} " +
                $"{nameof(arr1)}=[{(arr1 == null ? "N/A" : string.Join(",", arr1.Select(f => f.ToString())))}] " +
                $"{nameof(lst1)}=[{(lst1 == null ? "N/A" : string.Join(",", lst1.Select(f => f.ToString())))}] ";
        }
        public bool Equals(TestOuterContainer other)
        {
            if (i1 != other.i1 || i2 != other.i2 || i3 != other.i3 || singleFrame1 != other.singleFrame1)
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

    [MessagePackObject]
    public class TestInnerContainer : IEquatable<TestInnerContainer>
    {
        [Key(0)]
        public int i1;
        [Key(1)]
        public Frame<int>[] arr1;
        [Key(2)]
        public int i2;
        [Key(3)]
        public Frame<int> singleFrame2;

        public override string ToString()
        {
            return
                $"{nameof(i1)}={i1} {nameof(i2)}={i2} " +
                $"{nameof(singleFrame2)}={singleFrame2} " +
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
            if (i1 != other.i1 || i2 != other.i2 || singleFrame2 != other.singleFrame2)
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
            return singleFrame2.GetHashCode() ^ i1.GetHashCode() ^ i2.GetHashCode() ^ (arr1 == null ? 0 : HashCode.Combine(arr1.Select(f => f.Item.GetHashCode())));
        }
    }


    public static class SampleCreator
    {
        public static TestOuterContainer GetOuterContainerSample(OuterContainerSampleType type)
        {
            TestOuterContainer oc = null;
            switch (type)
            {
                case OuterContainerSampleType.Lst1SometimesNull: oc = GetOuterContainer_Lst1SometimesNull(); break;
                case OuterContainerSampleType.LargeLst1SometimesNull: oc = GetOuterContainer_LargeLst1SometimesNull(); break;
                case OuterContainerSampleType.Lst1Null: oc = GetOuterContainer_Lst1Null(); break;
                case OuterContainerSampleType.Arr1Null: oc = GetOuterContainer_Arr1Null(); break;
                case OuterContainerSampleType.Null: oc = GetOuterContainer_Null(); break;
                case OuterContainerSampleType.Arr1Null_Lst1Empty: oc = GetOuterContainer_Arr1Null_Lst1Empty(); break;
                case OuterContainerSampleType.Arr1Empty_Lst1Null: oc = GetOuterContainer_Arr1Empty_Lst1Null(); break;
            }
            return oc;
        }

        public static TestOuterContainer GetOuterContainer_Arr1Empty_Lst1Null()
        {
            return new TestOuterContainer()
            {
                arr1 = new Frame<int>[] { },
                i1 = 1123,
                i2 = 1345,
                i3 = 156,
                lst1 = null
            };
        }
        public static TestOuterContainer GetOuterContainer_Arr1Null_Lst1Empty()
        {
            var oc = new TestOuterContainer()
            {
                arr1 = null,
                i1 = 1123,
                i2 = 1345,
                i3 = 156,
                lst1 = new List<Frame<TestInnerContainer>>()
            };
            return oc;
        }

        public static TestOuterContainer GetOuterContainer_Null()
        {
            return null;
        }

        public static TestOuterContainer GetOuterContainer_Arr1Null()
        {

            return new TestOuterContainer()
            {
                arr1 = null,
                i1 = 1123,
                i2 = 1345,
                i3 = 156,
                lst1 = Enumerable.Range(0, 4).Select(ix => new Frame<TestInnerContainer>(GetInnerContainer())).ToList()
            };
        }

        public static TestOuterContainer GetOuterContainer_Lst1Null()
        {
            return new TestOuterContainer()
            {
                arr1 = GetFrameArray(),
                i1 = 1123,
                i2 = 1345,
                i3 = 156,
                lst1 = null
            };
        }
        public static TestOuterContainer GetOuterContainer_Lst1SometimesNull()
        {
            return new TestOuterContainer()
            {
                arr1 = GetFrameArray(),
                i1 = 1123,
                i2 = 1345,
                i3 = 156,
                lst1 = Enumerable.Range(0, 100).Select(ix => ix % 2 == 0 ? null : new Frame<TestInnerContainer>(GetInnerContainer())).ToList()
            };
        }

        public static TestOuterContainer GetOuterContainer_LargeLst1SometimesNull()
        {
            return new TestOuterContainer()
            {
                arr1 = GetFrameArray(1000),
                i1 = 1123,
                i2 = 1345,
                i3 = 156,
                lst1 = Enumerable.Range(0, 100).Select(ix => ix % 2 == 0 ? null : new Frame<TestInnerContainer>(GetInnerContainer(10))).ToList()
            };
        }


        public static Frame<int>[] GetFrameArray(int totItems = 3)
        {
            Frame<int>[] res = new Frame<int>[totItems];
            for (int f = 0; f < totItems; f++)
                res[f] = f + 1;
            return res;
        }
        public static TestInnerContainer GetInnerContainer(int totItems = 3)
        {
            return new TestInnerContainer() { arr1 = GetFrameArray(totItems), i1 = 4, i2 = 5 };
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
