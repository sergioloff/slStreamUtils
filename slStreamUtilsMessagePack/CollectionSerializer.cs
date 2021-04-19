/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using slStreamUtils.FIFOWorker;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.MultiThreadedSerialization.MessagePack
{
    public class CollectionSerializer<T> : MultiThreadedSerialization.CollectionSerializer<T>
    {
        private readonly MessagePackSerializerOptions w_opts;
        private readonly ArrayPool<byte> ap;
        public CollectionSerializer(Stream stream, FIFOWorkerConfig config, MessagePackSerializerOptions w_opts)
            : base(stream, config)
        {
            ap = ArrayPool<byte>.Shared;
            this.w_opts = w_opts;
        }

        protected override async Task WriteBodyAsync(Stream stream, T t, CancellationToken token)
        {
            await MessagePackSerializer.SerializeAsync(stream, t, w_opts, token).ConfigureAwait(false);
        }

        protected override async Task WriteHeaderAsync(Stream stream, int i, CancellationToken token)
        {
            if (BitConverter.IsLittleEndian)
                i = BinaryPrimitives.ReverseEndianness(i);

            byte[] buf = ap.Rent(MessagePackConsts.maxHeaderLength);
            try
            {
                int f = 0;
                buf[f++] = MessagePackConsts.tag_fixmap_2;
                buf[f++] = MessagePackConsts.tag_fixstr_1;
                buf[f++] = MessagePackConsts.field1_Name;
                WriteInt32(buf, ref f, i);
                buf[f++] = MessagePackConsts.tag_fixstr_1;
                buf[f++] = MessagePackConsts.field2_Name;
                if (f != MessagePackConsts.maxHeaderLength)
                    throw new Exception($"invalid total bytes written ({f}), expected {MessagePackConsts.maxHeaderLength}");
                await stream.WriteAsync(buf, 0, MessagePackConsts.maxHeaderLength, token).ConfigureAwait(false);
            }
            finally
            {
                ap.Return(buf);
            }
        }
        private void WriteInt32(byte[] buf, ref int f, int i)
        {
            if (i <= MessagePackCode.MaxFixInt)
                buf[f++] = (byte)i;
            else
            {
                buf[f++] = MessagePackConsts.tag_int32;
                buf[f++] = (byte)(i >> 0);
                buf[f++] = (byte)(i >> 8);
                buf[f++] = (byte)(i >> 16);
                buf[f++] = (byte)(i >> 24);
            }
        }
    }
}
