/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using MessagePack.Formatters;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;
using slStreamUtils;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsMessagePack
{
    public partial class CollectionSerializerAsync<T> : IDisposable, IAsyncDisposable
    {
        private readonly Stream stream;
        private MessagePackSerializerOptions w_opts;
        private ObjectPool<ArrayPoolBufferWriter<byte>> objPoolBufferWriterBodies;
        private ObjectPool<ArrayPoolBufferWriter<int>> objPoolBufferWriterBodyLengths;
        private ObjectPool<ArrayPoolBufferWriter<T>> objPoolOutputBatch;
        private FIFOWorker<ArrayPoolBufferWriter<T>, BatchWithBufferWriters> fifow;
        private BatchSizeEstimator batchEstimator;
        private ArrayPoolBufferWriter<T> currentBatch;
        private IMessagePackFormatter<T> formatterT;
        private const byte fmtCode_fixArraySize2 = MessagePackCode.MinFixArray | 0x02;
        private const int headerMaxLength =
            1 + // fmtCode_fixArraySize2 
            1 + // MessagePackCode.UInt32
            4; // sizeof(uint32)

        private struct BatchWithBufferWriters
        {
            public ArrayPoolBufferWriter<int> lengths;
            public ArrayPoolBufferWriter<byte> concatenatedBodies;
        }

        public CollectionSerializerAsync(Stream stream, FIFOWorkerConfig fifowConfig) :
            this(stream, fifowConfig, new BatchSizeEstimatorConfig(), MessagePackSerializerOptions.Standard)
        {
        }

        public CollectionSerializerAsync(Stream stream, FIFOWorkerConfig fifowConfig, BatchSizeEstimatorConfig estimatorConfig) :
            this(stream, fifowConfig, estimatorConfig, MessagePackSerializerOptions.Standard)
        {
        }

        public CollectionSerializerAsync(Stream stream, int maxConcurrentTasks) :
            this(stream, new FIFOWorkerConfig(maxConcurrentTasks), new BatchSizeEstimatorConfig(), MessagePackSerializerOptions.Standard)
        {
        }

        public CollectionSerializerAsync(Stream stream, FIFOWorkerConfig fifowConfig, BatchSizeEstimatorConfig estimatorConfig, MessagePackSerializerOptions w_opts)
        {
            this.stream = stream;
            fifow = new FIFOWorker<ArrayPoolBufferWriter<T>, BatchWithBufferWriters>(fifowConfig, HandleWorkerOutput);
            batchEstimator = new BatchSizeEstimator(estimatorConfig);
            this.w_opts = w_opts;
            objPoolBufferWriterBodies = new DefaultObjectPool<ArrayPoolBufferWriter<byte>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<byte>(Math.Max(1024 * 64, estimatorConfig.DesiredBatchSize_bytes)),
                fifowConfig.MaxQueuedItems);
            objPoolBufferWriterBodyLengths = new DefaultObjectPool<ArrayPoolBufferWriter<int>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<int>(1024),
                fifowConfig.MaxQueuedItems);
            objPoolOutputBatch = new DefaultObjectPool<ArrayPoolBufferWriter<T>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<T>(1024),
                fifowConfig.MaxQueuedItems);
            currentBatch = objPoolOutputBatch.Get();
            formatterT = w_opts.Resolver.GetFormatterWithVerify<T>();
        }

        public Task SerializeAsync(Frame<T> obj, CancellationToken token = default)
        {
            return SerializeAsync(obj.Item, token);
        }

        public async Task SerializeAsync(T t, CancellationToken token = default)
        {
            Interlocked.Increment(ref ParallelGatekeeperSingleton.wrapperDepth);
            try
            {
                currentBatch.GetSpan(1)[0] = t;
                currentBatch.Advance(1);
                await CompleteBatch(false, token).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref ParallelGatekeeperSingleton.wrapperDepth);
            }
        }

        public async Task FlushAsync(CancellationToken token = default)
        {
            Interlocked.Increment(ref ParallelGatekeeperSingleton.wrapperDepth);
            try
            {
                await CompleteBatch(true, token).ConfigureAwait(false);
                foreach (var bw in fifow.Flush(token))
                    await BatchToStreamAsync(bw, token).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref ParallelGatekeeperSingleton.wrapperDepth);
            }
        }

        private async Task CompleteBatch(bool flushBatch, CancellationToken token)
        {
            int desiredBatchSize = batchEstimator.RecomendedBatchSize;
            if (flushBatch || currentBatch.WrittenCount >= desiredBatchSize)
            {
                foreach (var bw in fifow.AddWorkItem(currentBatch, token))
                    await BatchToStreamAsync(bw, token).ConfigureAwait(false);
                currentBatch = objPoolOutputBatch.Get();
            }
        }

        private BatchWithBufferWriters HandleWorkerOutput(ArrayPoolBufferWriter<T> batch, CancellationToken token)
        {
            try
            {
                BatchWithBufferWriters batchOut = new BatchWithBufferWriters();
                batchOut.concatenatedBodies = objPoolBufferWriterBodies.Get();
                batchOut.lengths = objPoolBufferWriterBodyLengths.Get();
                MessagePackWriter writerBody = new MessagePackWriter(batchOut.concatenatedBodies)
                {
                    OldSpec = w_opts.OldSpec ?? false,
                    CancellationToken = token
                };
                var spanIn = batch.WrittenSpan;
                int prevWrittenBytesCount = 0;
                for (int ix = 0; ix < spanIn.Length; ix++)
                {
                    formatterT.Serialize(ref writerBody, spanIn[ix], w_opts);
                    writerBody.Flush();
                    int objLen = batchOut.concatenatedBodies.WrittenCount - prevWrittenBytesCount;
                    prevWrittenBytesCount = batchOut.concatenatedBodies.WrittenCount;
                    batchOut.lengths.GetSpan(1)[0] = objLen;
                    batchOut.lengths.Advance(1);
                }
                return batchOut;
            }
            finally
            {
                objPoolOutputBatch.Return(batch);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteHeader(Stream stream, uint value)
        {
            Span<byte> buffer = stackalloc byte[headerMaxLength];
            int resLen = WriteHeader(buffer, value);
            stream.Write(buffer.Slice(0, resLen));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int WriteHeader(Span<byte> buffer, uint value)
        {
            buffer[0] = fmtCode_fixArraySize2;
            return WriteUInt(buffer.Slice(1), value) + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUInt(Span<byte> span, uint value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                return WriteUint8Small(span, value);
            }
            else if (value <= byte.MaxValue)
            {
                return WriteUint8Large(span, value);
            }
            else if (value <= UInt16.MaxValue)
            {
                return WriteUInt16(span, (UInt16)value);
            }
            else
            {
                return WriteUInt32(span, (UInt32)value);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUint8Large(Span<byte> span, uint value)
        {
            span[0] = MessagePackCode.UInt8;
            span[1] = unchecked((byte)value);
            return 2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUint8Small(Span<byte> span, uint value)
        {
            span[0] = unchecked((byte)value);
            return 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUInt32(Span<byte> span, UInt32 value)
        {
            span[0] = MessagePackCode.UInt32;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), value);
            return 5;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteUInt16(Span<byte> span, UInt16 value)
        {
            span[0] = MessagePackCode.UInt16;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), value);
            return 3;
        }

        private async Task BatchToStreamAsync(BatchWithBufferWriters batch, CancellationToken token)
        {
            try
            {
                int GetItemLength(int lenghtAtIx)
                {
                    ReadOnlySpan<int> lengths = batch.lengths.WrittenSpan;
                    return lengths[lenghtAtIx];
                }
                int batchSize = batch.lengths.WrittenCount;
                for (int ix = 0, bodyStartIx = 0; ix < batchSize; ix++)
                {
                    int itemLen = GetItemLength(ix);

                    WriteHeader(stream, (uint)itemLen);
                    await stream.WriteAsync(batch.concatenatedBodies.WrittenMemory.Slice(bodyStartIx, itemLen), token);

                    bodyStartIx += itemLen;
                }
            }
            finally
            {
                objPoolBufferWriterBodies.Return(batch.concatenatedBodies);
                objPoolBufferWriterBodyLengths.Return(batch.lengths);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FlushAsync().Wait();
                fifow?.Dispose();
                if (currentBatch != null)
                    objPoolOutputBatch?.Return(currentBatch);
            }
            w_opts = null;
            objPoolBufferWriterBodies = null;
            objPoolBufferWriterBodyLengths = null;
            objPoolOutputBatch = null;
            fifow = null;
            batchEstimator = null;
            currentBatch = null;
            formatterT = null;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await FlushAsync().ConfigureAwait(false);
            fifow?.Dispose();
            if (currentBatch != null)
                objPoolOutputBatch?.Return(currentBatch);
        }
    }
}
