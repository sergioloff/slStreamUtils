/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */

namespace slStreamUtils.MultiThreadedSerialization.Wrappers
{
    public abstract class ColectionWrapper<T>
    {
        public ColectionWrapper()
        {
            a = default;
        }
        public ColectionWrapper(T[] items)
        {
            a = items;
        }

        public virtual T[] a { get; set; }
    }
}
