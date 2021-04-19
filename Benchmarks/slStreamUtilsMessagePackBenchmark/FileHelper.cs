/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace slStreamUtilsMessagePackBenchmark
{
    public static class FileHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hHandle);
        const int FILE_FLAG_NO_BUFFERING = unchecked(0x20000000);

        [DllImport("KERNEL32", SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        static extern IntPtr CreateFile(
              string fileName,
              int desiredAccess,
              FileShare shareMode,
              IntPtr securityAttrs,
              FileMode creationDisposition,
              int flagsAndAttributes,
              IntPtr templateFile);
        public static void FlushFileCache(string filename)
        {
            if (!File.Exists(filename))
                return;
            var handle = CreateFile(filename, (int)FileAccess.ReadWrite, FileShare.None, IntPtr.Zero, FileMode.OpenOrCreate, FILE_FLAG_NO_BUFFERING, IntPtr.Zero);
            CloseHandle(handle);
        }
    }
}
