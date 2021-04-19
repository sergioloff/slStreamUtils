/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;

namespace slStreamUtils
{
    public class FIFOWorkerException : Exception
    {
        public FIFOWorkerException(string msg) : base(msg) { }
    }
}
