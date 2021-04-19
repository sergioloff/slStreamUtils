/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using slStreamUtils.FIFOWorker;
using slStreamUtils.MultiThreadedSerialization.Wrappers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.MultiThreadedSerialization.MessagePack
{
    public class CollectionDeserializer<T> : MultiThreadedSerialization.CollectionDeserializer<T>
    {
        private readonly MessagePackSerializerOptions r_opts;
        private readonly ArrayPool<byte> ap;
        public CollectionDeserializer(FIFOWorkerConfig config, MessagePackSerializerOptions r_opts)
            : base(config)
        {
            ap = ArrayPool<byte>.Shared;
            this.r_opts = r_opts;
        }

        protected override async Task<T> ReadBodyAsync(Stream s, CancellationToken token)
        {
            T t = await MessagePackSerializer.DeserializeAsync<T>(s, r_opts, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return t;
        }


        protected override async Task<Tuple<bool, int>> ReadHeaderAsync(Stream s, CancellationToken token)
        {
            byte[] buf = ap.Rent(MessagePackConsts.maxHeaderLength);
            try
            {
                int outLen = await s.ReadAsync(buf, 0, MessagePackConsts.header1Length, token).ConfigureAwait(false);
                if (outLen == 0)
                    return new Tuple<bool, int>(false, default);
                void CheckByte(byte b, byte expected)
                {
                    if (b != expected) throw new Exception($"Invalid byte {b}, expected {expected}");
                }
                int f = 0;
                CheckByte(buf[f++], MessagePackConsts.tag_fixmap_2);
                CheckByte(buf[f++], MessagePackConsts.tag_fixstr_1);
                CheckByte(buf[f++], MessagePackConsts.field1_Name);
                int len = await ReadInt32Async(s, buf, token).ConfigureAwait(false);
                f = 0;
                await s.ReadAsync(buf, 0, MessagePackConsts.header2Length, token).ConfigureAwait(false);
                CheckByte(buf[f++], MessagePackConsts.tag_fixstr_1);
                CheckByte(buf[f++], MessagePackConsts.field2_Name);
                return new Tuple<bool, int>(true, len);
            }
            finally
            {
                ap.Return(buf);
            }
        }

        private async Task<int> ReadInt32Async(Stream s, byte[] buf, CancellationToken token)
        {
            await s.ReadAsync(buf, 0, 1, token).ConfigureAwait(false);
            byte b = buf[0];
            int i;
            switch (b)
            {
                case MessagePackCode.Int64:
                case MessagePackCode.UInt64:
                    throw new NotSupportedException($"Int64 not supported");
                case MessagePackCode.Int32:
                case MessagePackCode.UInt32:
                    await s.ReadAsync(buf, 0, sizeof(int), token).ConfigureAwait(false);
                    i = buf[0] << 0 | buf[1] << 8 | buf[2] << 16 | buf[3] << 24;
                    if (BitConverter.IsLittleEndian)
                        i = BinaryPrimitives.ReverseEndianness(i);
                    break;
                case MessagePackCode.Int16:
                case MessagePackCode.UInt16:
                    throw new NotSupportedException($"Int16 not supported");
                default:
                    if ((b & 0x80) == 0)
                        i = b & ~0x80;
                    else
                        throw new Exception($"unknown int packing code: {b}");
                    break;
            }
            return i;
        }
        protected override ItemWrapper<T> GetWrapper(T t, int l)
        {
            return new MessagePackItemWrapper<T>(t, l);
        }
    }
}
