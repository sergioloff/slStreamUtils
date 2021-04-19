/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using MessagePack.Formatters;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.HighPerformance.Buffers;
using slStreamUtils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace slStreamUtilsMessagePack.Formatters
{
    internal class FrameItemFormatter<T> : IMessagePackFormatter<Frame<T>>
    {
        private DefaultObjectPool<ArrayPoolBufferWriter<byte>> objPoolBufferWriterBodies;

        public FrameItemFormatter()
        {
            int defaultMaxQueuedItems = new FIFOWorkerConfig(Environment.ProcessorCount * 2).MaxQueuedItems;
            objPoolBufferWriterBodies = new DefaultObjectPool<ArrayPoolBufferWriter<byte>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<byte>(1024 * 64), defaultMaxQueuedItems);
        }

        public Frame<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            FrameFormatterSerializationOptions frameOptions = options.GetOptionParams();
            var formatter = options.Resolver.GetFormatterWithVerify<T>();
            return Deserialize(ref reader, options, frameOptions, formatter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Frame<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options, FrameFormatterSerializationOptions frameOptions, IMessagePackFormatter<T> formatter)
        {
            int frameLength = ReadElementHeader(ref reader);
            if (frameLength == Frame<T>.unassigned && frameOptions.ThrowOnUnnasignedFrameDeserialization)
                throw new StreamSerializationException($"Unassigned buffer length found during parallel deserialize for {nameof(Frame<T>)}");
            return new Frame<T>(frameLength, formatter.Deserialize(ref reader, options));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ReadElementHeader(ref MessagePackReader reader)
        {
            int arrTotElems = reader.ReadArrayHeader();
            if (arrTotElems != Frame<T>.parallelItemTotElems)
                throw new StreamSerializationException($"invalid array header returned ({arrTotElems}). Expected ({Frame<T>.parallelItemTotElems})");
            uint length = reader.ReadUInt32();
            if (length > int.MaxValue)
                throw new StreamSerializationException($"invalid body length ({length})");
            return (int)length;
        }

        public void Serialize(ref MessagePackWriter writer, Frame<T> value, MessagePackSerializerOptions options)
        {
            ArrayPoolBufferWriter<byte> bodyWriter = objPoolBufferWriterBodies.Get();
            try
            {
                var formatterT = options.Resolver.GetFormatterWithVerify<T>();
                Serialize(ref writer, value.Item, options, bodyWriter, formatterT);
            }
            finally
            {
                objPoolBufferWriterBodies.Return(bodyWriter);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options, ArrayPoolBufferWriter<byte> bodyWriter, IMessagePackFormatter<T> formatterT)
        {
            MessagePackWriter cloneWriter = writer.Clone(bodyWriter);
            formatterT.Serialize(ref cloneWriter, value, options);
            cloneWriter.Flush();
            writer.WriteArrayHeader(Frame<T>.parallelItemTotElems);
            writer.Write((uint)bodyWriter.WrittenCount);
            writer.WriteRaw(bodyWriter.WrittenSpan);
            bodyWriter.Clear();
        }
    }
}
