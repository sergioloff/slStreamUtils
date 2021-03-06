// copied from https://github.com/Corvalius/ravendb/tree/master/Raven.Sparrow/Sparrow. Please follow the link for licencing info
#if !DNXCORE50
#if WINDOWS_NT
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace slStreamUtils.ThirdParty.Sparrow
{
    public static unsafe partial class UnmanagedMemory
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = false)]
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        public static extern IntPtr Copy(byte* dest, byte* src, int count);

        [DllImport("msvcrt.dll", EntryPoint = "memcmp", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        public static extern int Compare(byte* b1, byte* b2, int count);

        [DllImport("msvcrt.dll", EntryPoint = "memmove", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        public static extern int Move(byte* b1, byte* b2, int count);

        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        public static extern IntPtr Set(byte* dest, int c, int count);
    }
}
#endif
#endif
