/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace slStreamUtilsMessagePack
{
    internal static class UIntWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int WriteUInt(Span<byte> span, uint value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                return WriteUint8Small(span, value);
            }
            else if (value <= byte.MaxValue)
            {
                return WriteUint8Large(span, value);
            }
            else if (value <= UInt16.MaxValue)
            {
                return WriteUInt16(span, (UInt16)value);
            }
            else
            {
                return WriteUInt32(span, (UInt32)value);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUint8Large(Span<byte> span, uint value)
        {
            span[0] = MessagePackCode.UInt8;
            span[1] = unchecked((byte)value);
            return 2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUint8Small(Span<byte> span, uint value)
        {
            span[0] = unchecked((byte)value);
            return 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUInt32(Span<byte> span, UInt32 value)
        {
            span[0] = MessagePackCode.UInt32;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), value);
            return 5;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUInt16(Span<byte> span, UInt16 value)
        {
            span[0] = MessagePackCode.UInt16;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), value);
            return 3;
        }
    }
}
