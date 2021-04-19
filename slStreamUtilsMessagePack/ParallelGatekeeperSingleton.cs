/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
namespace slStreamUtilsMessagePack
{
    internal static class ParallelGatekeeperSingleton
    {
        internal volatile static int wrapperDepth;
    }
}
