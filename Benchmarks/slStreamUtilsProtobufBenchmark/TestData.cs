/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[module: CompatibilityLevel(CompatibilityLevel.Level300)]
namespace slStreamUtilsProtobufBenchmark
{
    public interface IDoStuff
    {
        int DoStuff();
    }
    public interface IMeasureSizeWithAllignmentPadding
    {
        int GetSize();
    }
    public interface IAmRandomInstantiable<T>
    {
        T GetRandInstance(RandHelper helper, params int[] lengths);
    }




    [ProtoContract]
    public struct TestStructSmall1 : IEquatable<TestStructSmall1>, IAmRandomInstantiable<TestStructSmall1>, IMeasureSizeWithAllignmentPadding
    {
        [ProtoMember(1)]
        public int i1;
        [ProtoMember(2)]
        public int i2;
        [ProtoMember(3)]
        public bool b1;
        [ProtoMember(4)]
        public long l1;

        public int GetSize()
        {
            return Marshal.SizeOf(GetType());
        }

        public TestStructSmall1 GetRandInstance(RandHelper helper, params int[] lengths)
        {
            TestStructSmall1 res = new TestStructSmall1();
            helper.GetRand(out res.i1);
            helper.GetRand(out res.i2);
            helper.GetRand(out res.b1);
            helper.GetRand(out res.l1);
            return res;
        }
        public bool Equals([DisallowNull] TestStructSmall1 other)
        {
            return
                other.i1 == i1 &&
                other.i2 == i2 &&
                other.b1 == b1 &&
                other.l1 == l1;
        }
    }

    [ProtoContract]
    public struct TestStructSmall2 : IEquatable<TestStructSmall2>, IAmRandomInstantiable<TestStructSmall2>, IMeasureSizeWithAllignmentPadding
    {
        [ProtoMember(1)]
        public long l1;
        [ProtoMember(2)]
        public bool b1;
        [ProtoMember(3)]
        public bool b2;
        [ProtoMember(4)]
        public long l2;
        [ProtoMember(5)]
        public DateTime dt;
        public int GetSize()
        {
            return Marshal.SizeOf(GetType());
        }
        public TestStructSmall2 GetRandInstance(RandHelper helper, params int[] lengths)
        {
            TestStructSmall2 res = new TestStructSmall2();
            helper.GetRand(out res.l1);
            helper.GetRand(out res.b1);
            helper.GetRand(out res.b2);
            helper.GetRand(out res.l2);
            helper.GetRand(out res.dt);
            return res;
        }
        public bool Equals([DisallowNull] TestStructSmall2 other)
        {
            return
                other.l1 == l1 &&
                other.b1 == b1 &&
                other.b2 == b2 &&
                other.l2 == l2 &&
                other.dt == dt;
        }
    }


    [ProtoContract]
    public struct TestStructLarge1 : IEquatable<TestStructLarge1>, IAmRandomInstantiable<TestStructLarge1>, IMeasureSizeWithAllignmentPadding
    {
        [ProtoMember(1)]
        public bool b1;
        [ProtoMember(2)]
        public TimeSpan t1;
        [ProtoMember(3)]
        public DateTime dt1;
        [ProtoMember(4)]
        public TestStructSmall1 ss1;
        [ProtoMember(5)]
        public TestStructSmall2 ss2;
        [ProtoMember(6)]
        public int i1;
        [ProtoMember(8)]
        public long l1;

        public int GetSize()
        {
            return Marshal.SizeOf(GetType());
        }

        public TestStructLarge1 GetRandInstance(RandHelper helper, params int[] lengths)
        {
            TestStructLarge1 res = new TestStructLarge1();
            helper.GetRand(out res.b1);
            helper.GetRand(out res.t1);
            helper.GetRand(out res.dt1);
            res.ss1 = new TestStructSmall1().GetRandInstance(helper);
            res.ss2 = new TestStructSmall2().GetRandInstance(helper);
            helper.GetRand(out res.i1);
            helper.GetRand(out res.l1);
            return res;
        }
        public bool Equals([DisallowNull] TestStructLarge1 other)
        {
            return
                other.b1 == b1 &&
                other.t1 == t1 &&
                other.dt1 == dt1 &&
                other.ss1.Equals(ss1) &&
                other.ss2.Equals(ss2) &&
                other.i1 == i1 &&
                other.l1 == l1;
        }
    }

    struct Aux_TestClassSmall1
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0169
#pragma warning disable CS0649
        bool b1;
        TestStructSmall1 ss1;
        public IntPtr ia1;
        long l1;
#pragma warning restore CS0649
#pragma warning restore CS0169
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
    }

    [ProtoContract]
    public class TestClassSmall : IEquatable<TestClassSmall>, IDoStuff, IAmRandomInstantiable<TestClassSmall>, IMeasureSizeWithAllignmentPadding
    {
        [ProtoMember(1)]
        public bool b1;
        [ProtoMember(2)]
        public TestStructSmall1 ss1;
        [ProtoMember(3)]
        public int[] ia1;
        [ProtoMember(4)]
        public long l1;

        public int GetSize()
        {
            return Marshal.SizeOf<Aux_TestClassSmall1>() + ia1.Length * sizeof(int);
        }

        public TestClassSmall GetRandInstance(RandHelper helper, params int[] lengths)
        {
            TestClassSmall res = new TestClassSmall();
            helper.GetRand(out res.b1);
            res.ss1 = new TestStructSmall1().GetRandInstance(helper);
            res.ia1 = new int[lengths[0]];
            for (int i = 0; i < res.ia1.Length; i++)
                helper.GetRand(out res.ia1[i]);
            helper.GetRand(out res.l1);
            return res;
        }
        public bool Equals([DisallowNull] TestClassSmall other)
        {
            return
                other.b1 == b1 &&
                other.ss1.Equals(ss1) &&
                other.ia1.SequenceEqual(ia1) &&
                other.l1 == l1;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public int DoStuff()
        {
            return l1.GetHashCode() ^ ia1.Length ^ ss1.GetHashCode() ^ b1.GetHashCode();
        }
    }

    struct Aux_TestClassLarge1
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0169
#pragma warning disable CS0649
        public bool b1;
        public IntPtr ssa1;
        public int i1;
        public IntPtr sla1;
        public long l1;
        public IntPtr ia1;
#pragma warning restore CS0649
#pragma warning restore CS0169
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 0)]
    [ProtoContract]
    public class TestClassLarge : IEquatable<TestClassLarge>, IDoStuff, IAmRandomInstantiable<TestClassLarge>, IMeasureSizeWithAllignmentPadding
    {
        [ProtoMember(3)]
        public bool b1;
        [ProtoMember(4)]
        public TestStructSmall2[] ssa1;
        [ProtoMember(5)]
        public int i1;
        [ProtoMember(6)]
        public TestStructLarge1[] sla1;
        [ProtoMember(7)]
        public long l1;
        [ProtoMember(8)]
        public int[] ia1;

        public int GetSize()
        {
            return Marshal.SizeOf<Aux_TestClassLarge1>() +
                new TestStructSmall2().GetSize() * ssa1.Length +
                new TestStructLarge1().GetSize() * sla1.Length +
                sizeof(int) * ia1.Length;
        }

        public TestClassLarge GetRandInstance(RandHelper helper, params int[] lengths)
        {
            TestClassLarge res = new TestClassLarge();
            helper.GetRand(out res.b1);
            res.ssa1 = new TestStructSmall2[lengths[0]];
            for (int i = 0; i < res.ssa1.Length; i++)
                res.ssa1[i] = new TestStructSmall2().GetRandInstance(helper);
            helper.GetRand(out res.i1);
            res.sla1 = new TestStructLarge1[lengths[0]];
            for (int i = 0; i < res.sla1.Length; i++)
                res.sla1[i] = new TestStructLarge1().GetRandInstance(helper);
            helper.GetRand(out res.l1);
            res.ia1 = new int[lengths[0]];
            for (int i = 0; i < res.ia1.Length; i++)
                helper.GetRand(out res.ia1[i]);
            return res;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public int DoStuff()
        {
            return l1.GetHashCode() ^ ia1.Length ^ sla1.GetHashCode() ^ b1.GetHashCode();
        }

        public bool Equals([DisallowNull] TestClassLarge other)
        {
            return
                other.b1 == b1 &&
                other.ssa1.SequenceEqual(ssa1) &&
                other.i1 == i1 &&
                other.sla1.SequenceEqual(sla1) &&
                other.l1 == l1 &&
                other.ia1.SequenceEqual(ia1);
        }

    }

    public class RandHelper
    {
        public Random r;

        public RandHelper()
        {
            r = new Random(0);
        }
        public RandHelper(int seed)
        {
            r = new Random(seed);
        }

        public void GetRand(out int v)
        {
            v = r.Next(0, 4);
        }
        public void GetRand(out bool v)
        {
            v = r.Next(2) == 0;
        }
        public void GetRand(out long v)
        {
            v = (long)r.Next(4, 8) << 32 | (long)r.Next(8, 12);
        }
        public void GetRand(out DateTime v)
        {
            v = new DateTime(1900, 1, 1).AddYears(r.Next(12, 16)).AddDays(r.Next(16, 20)).AddSeconds(r.Next(20, 24));
        }
        public void GetRand(out TimeSpan v)
        {
            v = TimeSpan.FromSeconds(r.Next(20));
        }
        public void GetRand(out string v)
        {
            int len = r.Next(1, 200);
            v = Encoding.ASCII.GetString(Enumerable.Range(0, len).Select(f => (byte)('a' + r.Next(0, 4))).ToArray());
        }
        public void GetRand(out Guid v)
        {
            byte[] b = new byte[16];
            r.NextBytes(b);
            v = new Guid(b);
        }

    }
}
