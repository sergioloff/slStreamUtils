/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;

namespace slStreamUtilsMessagePackBenchmark
{
    public class Rand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SerializableTypes.Rand"/> class, using the specified seed value.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence. If a negative number is specified, the absolute value of the number is used. </param>
        public Rand(int seed)
        {
            _seed = seed > 0 ? seed : ((uint)seed).GetHashCode();
        }
        public Rand() : this(new Rand().Next()) { }


        private int _seed;

        public int CurrentSeed
        {
            get { return _seed; }
            set { _seed = value; }
        }

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// 
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero and less than <see cref="F:System.Int32.MaxValue"/>.
        /// </returns>
        public int Next()
        {
            const double a = 16807;      //ie 7**5
            const double m = 2147483647; //ie 2**31-1
            double temp = (double)_seed * a;
            _seed = (int)(temp - m * Math.Floor(temp / m));
            return _seed;
        }
        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// 
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily includes zero but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals zero, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to zero. </param>
        public int Next(int maxValue)
        {
            Next();
            return _seed % maxValue;
        }
        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// 
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/> but not <paramref name="maxValue"/>. If <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <param name="minValue">The inclusive lower bound of the random number returned. </param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>. </param>
        public int Next(int minValue, int maxValue)
        {
            Next();
            return minValue + (_seed % (maxValue - minValue));
        }
        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// 
        /// <returns>
        /// A double-precision floating point number greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public double NextDouble()
        {
            Next();
            double d = (double)_seed / (double)int.MaxValue;
            return d;
        }
    }
}
