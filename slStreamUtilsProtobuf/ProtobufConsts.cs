/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
namespace slStreamUtils.MultiThreadedSerialization.Protobuf
{
    internal static class ProtobufConsts
    {
        internal const int protoRepeatedTag1 = 2 + (1 << 3); // byte indicating a repeated element (type id=2) with tag=1
    }
}
