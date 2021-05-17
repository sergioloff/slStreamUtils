/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;
using System;

namespace slStreamUtilsProtobuf
{
    [ProtoContract]
    public struct Frame<T> : IEquatable<Frame<T>>
    {
        [ProtoIgnore]
        public int BufferLength { get; internal set; }

        [ProtoMember(1)]
        public T Item { get; set; }

        public const int unassigned = 0;
        public Frame(T item)
        {
            Item = item;
            BufferLength = unassigned;
        }

        public Frame(int length, T item)
        {
            BufferLength = length;
            Item = item;
        }

        [ProtoIgnore]
        public bool BufferLengthIsAssigned => BufferLength > unassigned;

        public static implicit operator Frame<T>(T item)
        {
            return new Frame<T>(item);
        }

        public static implicit operator T(Frame<T> value)
        {
            return value.Item;
        }
        public static bool operator ==(Frame<T> obj1, Frame<T> obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Frame<T> obj1, Frame<T> obj2)
        {
            return !obj1.Equals(obj2);
        }

        public bool Equals(Frame<T> obj)
        {
            T other = obj.Item;
            if (Item == null != (other == null))
                return false;
            if (Item != null)
            {
                if (Item is IEquatable<T> eqItem && other is IEquatable<T> eqOther)
                    return eqItem.Equals(eqOther);
                return Item.Equals(other);
            }
            else return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is Frame<T> pObj)
                return Equals(pObj);
            return false;
        }

        public override int GetHashCode()
        {
            if (Item == null)
                return -1;
            return Item.GetHashCode();
        }

        public override string ToString()
        {
            return Item?.ToString() ?? "null";
        }
    }
}
