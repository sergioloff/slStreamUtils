/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System.Collections.Generic;
using System.Linq;

namespace slStreamUtilsMessagePack.Formatters
{

    internal class FrameListFormatter<TFrameList, T> : FrameCollectionFormatter<TFrameList, T> where TFrameList : IList<Frame<T>>
    {

        public class ListFrameWrapper_InnerList : ListFrameWrapper
        {
            private readonly Frame<T>[] array;

            public ListFrameWrapper_InnerList(Frame<T>[] array)
            {
                this.array = array;
            }

            public override Frame<T>[] AsFrameArray()
            {
                return array;
            }
            public override TFrameList AsFrameList()
            {
                return (TFrameList)(IList<Frame<T>>)array?.ToList() ?? default;
            }
        }

        public override ListFrameWrapper GetTFrameListWrapper(TFrameList source)
        {
            return new ListFrameWrapper_InnerList(((IList<Frame<T>>)source).ToArray());
        }

        public override ListFrameWrapper GetTFrameListWrapper(int count)
        {
            return new ListFrameWrapper_InnerList(new Frame<T>[count]);
        }
    }
}
