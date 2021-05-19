/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using ProtoBuf;

namespace slStreamUtilsProtobuf
{
    internal static class ProtobufConsts
    {
        internal const int protoRepeatedTag1 = ((int)WireType.String) + (1 << 3); // byte indicating a repeated element (string) with field number = 1
    }
}
