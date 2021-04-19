/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;
using slStreamUtils;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace slStreamUtilsMessagePack
{
    public partial class CollectionDeserializerAsync<T> : IDisposable
    {
        private FIFOWorker<BatchWithBufferWriters, BatchWithFramesArray> fifow;
        private MessagePackSerializerOptions r_opts;
        private ObjectPool<ArrayPoolBufferWriter<byte>> objPoolBufferWriterBodies;
        private ObjectPool<ArrayPoolBufferWriter<int>> objPoolBufferWriterBodyLengths;
        private ArrayPool<Frame<T>> arrPoolOutputBatch;
        private int desiredBatchSize_bytes;
        private const byte fmtCode_fixArraySize2 = MessagePackCode.MinFixArray | 0x02;
        private const int maxHeaderLength = sizeof(uint) + 2;

        private struct BatchWithBufferWriters
        {
            public ArrayPoolBufferWriter<int> lengths;
            public ArrayPoolBufferWriter<byte> concatenatedBodies;
        }
        private struct BatchWithFramesArray
        {
            public Frame<T>[] batch;
            public int batchSize;
        }

        public CollectionDeserializerAsync(FIFOWorkerConfig fifowConfig) :
            this(fifowConfig, MessagePackSerializerOptions.Standard)
        {
        }

        public CollectionDeserializerAsync(int maxConcurrentTasks) :
            this(new FIFOWorkerConfig(maxConcurrentTasks), MessagePackSerializerOptions.Standard)
        {
        }

        public CollectionDeserializerAsync(FIFOWorkerConfig fifowConfig, MessagePackSerializerOptions r_opts, int desiredBatchSize_bytes = 1024 * 64)
        {
            fifow = new FIFOWorker<BatchWithBufferWriters, BatchWithFramesArray>(fifowConfig, HandleWorkerOutput);
            this.desiredBatchSize_bytes = desiredBatchSize_bytes;
            objPoolBufferWriterBodies = new DefaultObjectPool<ArrayPoolBufferWriter<byte>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<byte>(Math.Max(1024 * 64, desiredBatchSize_bytes)),
                fifowConfig.MaxQueuedItems);
            objPoolBufferWriterBodyLengths = new DefaultObjectPool<ArrayPoolBufferWriter<int>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<int>(1024),
                fifowConfig.MaxQueuedItems);
            arrPoolOutputBatch = ArrayPool<Frame<T>>.Shared;
            this.r_opts = r_opts;
        }

        public async IAsyncEnumerable<Frame<T>> DeserializeAsync(Stream stream, [EnumeratorCancellation] CancellationToken token = default)
        {
            Interlocked.Increment(ref ParallelGatekeeperSingleton.wrapperDepth);
            try
            {
                BatchWithBufferWriters currentBatch = new BatchWithBufferWriters();
                int currentBatchTotalSize = 0;
                while (true)
                {
                    if (!ReadHeader(stream, out int itemLength))
                        break;
                    if (currentBatchTotalSize + itemLength > desiredBatchSize_bytes && currentBatchTotalSize > 0)
                    {
                        // send prev batch
                        foreach (Frame<T> t in IterateOutputBatch(fifow.AddWorkItem(currentBatch, token)))
                            yield return t;
                        currentBatchTotalSize = 0;
                    }
                    if (currentBatchTotalSize == 0)
                    {
                        currentBatch.concatenatedBodies = objPoolBufferWriterBodies.Get();
                        currentBatch.lengths = objPoolBufferWriterBodyLengths.Get();
                    }
                    // read element from stream and add to batch
                    currentBatch.lengths.GetSpan(1)[0] = itemLength;
                    currentBatch.lengths.Advance(1);
                    int totRead = await stream.ReadAsync(currentBatch.concatenatedBodies.GetMemory(itemLength).Slice(0, itemLength), token).ConfigureAwait(false);
                    if (totRead != itemLength)
                        throw new StreamSerializationException($"Unexpected number of bytes read from stream ({totRead}). Expected {itemLength}");
                    currentBatch.concatenatedBodies.Advance(itemLength);
                    currentBatchTotalSize += itemLength;
                }
                if (currentBatchTotalSize > 0) // send unfinished batch
                    foreach (Frame<T> t in IterateOutputBatch(fifow.AddWorkItem(currentBatch, token)))
                        yield return t;
                foreach (Frame<T> t in IterateOutputBatch(fifow.Flush(token)))
                    yield return t;
            }
            finally
            {
                Interlocked.Decrement(ref ParallelGatekeeperSingleton.wrapperDepth);
            }
        }

        private IEnumerable<Frame<T>> IterateOutputBatch(IEnumerable<BatchWithFramesArray> outBatches)
        {
            foreach (BatchWithFramesArray outBatch in outBatches)
            {
                try
                {
                    for (int itemIx = 0; itemIx < outBatch.batchSize; itemIx++)
                        yield return outBatch.batch[itemIx];
                }
                finally
                {
                    arrPoolOutputBatch.Return(outBatch.batch);
                }
            }
        }

        private T DeserializeBody(ReadOnlyMemory<byte> mem, CancellationToken token)
        {
            T t = MessagePackSerializer.Deserialize<T>(mem, r_opts, token);
            token.ThrowIfCancellationRequested();
            return t;
        }

        private bool ReadHeader(Stream s, out int length)
        {
            int i = s.ReadByte();
            if (i == -1)
            {
                length = default;
                return false;
            }
            byte b = (byte)i;
            if (b != fmtCode_fixArraySize2)
                throw new StreamSerializationException($"Unexpected msgpack code {b} ({MessagePackCode.ToFormatName(b)}) encountered. Expected {fmtCode_fixArraySize2}");
            var ulength = UnpackUIntX(s);
            if (ulength > int.MaxValue)
                throw new StreamSerializationException($"Frame length too large: {ulength}");
            length = (int)ulength;
            return true;
        }

        private BatchWithFramesArray HandleWorkerOutput(BatchWithBufferWriters batchIn, CancellationToken token)
        {
            try
            {
                ReadOnlySpan<int> lengths = batchIn.lengths.WrittenSpan;
                ReadOnlyMemory<byte> bodies = batchIn.concatenatedBodies.WrittenMemory;
                int batchSize = batchIn.lengths.WrittenCount;
                Frame<T>[] resFrames = arrPoolOutputBatch.Rent(batchSize);
                for (int ix = 0, bodyStartIx = 0; ix < batchSize; ix++)
                {
                    int itemLen = lengths[ix];
                    ReadOnlyMemory<byte> body = bodies.Slice(bodyStartIx, itemLen);
                    resFrames[ix] = DeserializeBody(body, token);
                    bodyStartIx += itemLen;
                }
                return new BatchWithFramesArray()
                {
                    batchSize = batchSize,
                    batch = resFrames
                };
            }
            finally
            {
                objPoolBufferWriterBodies.Return(batchIn.concatenatedBodies);
                objPoolBufferWriterBodyLengths.Return(batchIn.lengths);
            }
        }

        private static uint UnpackUIntX(Stream s)
        {
            int i = s.ReadByte();
            if (i == -1)
                throw new StreamSerializationException("Unexpected end of stream while unpacking msgpack code");
            var b = (byte)i;
            switch (b)
            {
                case MessagePackCode.UInt32:
                    return UnpackUInt32(s);
                case MessagePackCode.UInt16:
                    return UnpackUInt16(s);
                case MessagePackCode.UInt8:
                    return UnpackUInt8(s);
                default:
                    if ((b & 0x80) == 0)
                        return UnpackFixedUInt(b);
                    else
                        throw new StreamSerializationException($"Unexpected msgpack code {b} ({MessagePackCode.ToFormatName(b)}) encountered.");
            }
        }
        private static uint UnpackUInt32(Stream s)
        {
            Span<byte> buf = stackalloc byte[sizeof(UInt32)];
            if (s.Read(buf) != sizeof(UInt32))
                throw new StreamSerializationException($"Unexpected end of stream while reading {sizeof(UInt32)} bytes from stream");
            return BinaryPrimitives.ReadUInt32BigEndian(buf);
        }
        private static uint UnpackUInt16(Stream s)
        {
            Span<byte> buf = stackalloc byte[sizeof(UInt16)];
            if (s.Read(buf) != sizeof(UInt16))
                throw new StreamSerializationException($"Unexpected end of stream while reading {sizeof(UInt16)} bytes from stream");
            return BinaryPrimitives.ReadUInt16BigEndian(buf);
        }
        private static uint UnpackUInt8(Stream s)
        {
            int i = s.ReadByte();
            if (i == -1)
                throw new StreamSerializationException($"Unexpected end of stream while reading {sizeof(byte)} bytes from stream");
            return (byte)i;
        }
        private static uint UnpackFixedUInt(byte b)
        {
            return b & ~0x80U;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                fifow?.Dispose();
            fifow = null;
            r_opts = null;
            objPoolBufferWriterBodies = null;
            objPoolBufferWriterBodyLengths = null;
            arrPoolOutputBatch = null;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
