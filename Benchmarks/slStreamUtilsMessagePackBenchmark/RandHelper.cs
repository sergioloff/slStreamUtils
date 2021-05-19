/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.Linq;
using System.Text;

namespace slStreamUtilsMessagePackBenchmark
{
    public class RandHelper
    {
        public Rand r;

        public RandHelper(int seed = 0)
        {
            r = new Rand(seed);
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

    }
}
