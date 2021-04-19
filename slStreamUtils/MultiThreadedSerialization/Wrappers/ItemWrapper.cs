/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */

namespace slStreamUtils.MultiThreadedSerialization.Wrappers
{
    public abstract class ItemWrapper<T>
    {
        public ItemWrapper() : this(default, default)
        {
        }
        public ItemWrapper(T item, int totalBytes)
        {
            l = totalBytes;
            t = item;
        }
        public ItemWrapper(T item) : this(item, -1)
        {
        }
        public virtual int l { get; set; }
        public virtual T t { get; set; }
    }
}
