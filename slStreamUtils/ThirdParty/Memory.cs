﻿// copied from https://github.com/Corvalius/ravendb/tree/master/Raven.Sparrow/Sparrow. Please follow the link for licencing info
#if !DNXCORE50
using System.Runtime.CompilerServices;

namespace slStreamUtils.ThirdParty.Sparrow
{
    public unsafe static class Memory
    {

        /// <summary>
        /// Bulk copy is optimized to handle copy operations where n is statistically big. While it will use a faster copy operation for 
        /// small amounts of memory, when you have smaller than 2048 bytes calls (depending on the target CPU) it will always be
        /// faster to call .Copy() directly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void BulkCopy(byte* dest, byte* src, int n)
        {
#if WINDOWS_NT
            UnmanagedMemory.Copy(dest, src, n);
#else
            Buffer.MemoryCopy(src, dest, n, n);
#endif
        }

        public unsafe static void Copy(byte* dest, byte* src, int n)
        {
            CopyInline(dest, src, n);
        }

        public unsafe static void Copy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            fixed (byte* srcPtr = src)
            fixed (byte* dstPtr = dst)
            {
                Copy(dstPtr + dstOffset, srcPtr + srcOffset, count);
            }
        }


        /// <summary>
        /// Copy is optimized to handle copy operations where n is statistically small. 
        /// This method is optimized at the IL level to be extremely efficient for copies smaller than
        /// 4096 bytes or heterogeneous workloads with occasional big copies.         
        /// </summary>
        /// <remarks>This is a forced inline version, use with care.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void CopyInline(byte* dest, byte* src, int n)
        {
            SMALLTABLE:
            switch (n)
            {
                case 16:
                    *(long*)dest = *(long*)src;
                    *(long*)(dest + 8) = *(long*)(src + 8);
                    return;
                case 15:
                    *(short*)(dest + 12) = *(short*)(src + 12);
                    *(dest + 14) = *(src + 14);
                    goto case 12;
                case 14:
                    *(short*)(dest + 12) = *(short*)(src + 12);
                    goto case 12;
                case 13:
                    *(dest + 12) = *(src + 12);
                    goto case 12;
                case 12:
                    *(long*)dest = *(long*)src;
                    *(int*)(dest + 8) = *(int*)(src + 8);
                    return;
                case 11:
                    *(short*)(dest + 8) = *(short*)(src + 8);
                    *(dest + 10) = *(src + 10);
                    goto case 8;
                case 10:
                    *(short*)(dest + 8) = *(short*)(src + 8);
                    goto case 8;
                case 9:
                    *(dest + 8) = *(src + 8);
                    goto case 8;
                case 8:
                    *(long*)dest = *(long*)src;
                    return;
                case 7:
                    *(short*)(dest + 4) = *(short*)(src + 4);
                    *(dest + 6) = *(src + 6);
                    goto case 4;
                case 6:
                    *(short*)(dest + 4) = *(short*)(src + 4);
                    goto case 4;
                case 5:
                    *(dest + 4) = *(src + 4);
                    goto case 4;
                case 4:
                    *(int*)dest = *(int*)src;
                    return;
                case 3:
                    *(dest + 2) = *(src + 2);
                    goto case 2;
                case 2:
                    *(short*)dest = *(short*)src;
                    return;
                case 1:
                    *dest = *src;
                    return;
                case 0:
                    return;
                default:
                    break;
            }

            if (n <= 512)
            {
                int count = n / 32;
                n -= (n / 32) * 32;

                while (count > 0)
                {
                    ((long*)dest)[0] = ((long*)src)[0];
                    ((long*)dest)[1] = ((long*)src)[1];
                    ((long*)dest)[2] = ((long*)src)[2];
                    ((long*)dest)[3] = ((long*)src)[3];

                    dest += 32;
                    src += 32;
                    count--;
                }

                if (n > 16)
                {
                    ((long*)dest)[0] = ((long*)src)[0];
                    ((long*)dest)[1] = ((long*)src)[1];

                    src += 16;
                    dest += 16;
                    n -= 16;
                }

                goto SMALLTABLE;
            }

            BulkCopy(dest, src, n);
        }
    }
}
#endif
