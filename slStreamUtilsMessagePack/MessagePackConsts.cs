/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using System;

namespace slStreamUtils.MultiThreadedSerialization.MessagePack
{
    internal static class MessagePackConsts
    {
        internal const byte tag_fixmap_2 = MessagePackCode.MinFixMap | 0x02;
        internal const byte tag_fixstr_1 = MessagePackCode.MinFixStr | 0x01;
        internal static readonly byte field1_Name = (byte)(nameof(MessagePackItemWrapper<object>.l)[0]);
        internal const byte tag_int32 = MessagePackCode.Int32;
        internal static readonly byte field2_Name = (byte)(nameof(MessagePackItemWrapper<object>.t)[0]);
        internal const int maxHeaderLength =
            header1Length +
            1 + // tag int32
            sizeof(Int32) +
            header2Length;
        internal const int header1Length =
            1 + // tag fixmap 2
            1 + // tag fixstr 1
            1; // sizeof(char)
        internal const int header2Length =
            1 + // tag fixstr 1
            1;  // sizeof(char)
    }
}
