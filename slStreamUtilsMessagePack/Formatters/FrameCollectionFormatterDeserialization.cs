/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */

using MessagePack;
using MessagePack.Formatters;
using slStreamUtils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace slStreamUtilsMessagePack.Formatters
{
    internal abstract partial class FrameCollectionFormatter<TFrameList, T>
    {
        public TFrameList Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return (TFrameList)(IList<T>)null;

            Interlocked.Increment(ref ParallelGatekeeperSingleton.wrapperDepth);
            try
            {
                options.Security.DepthStep(ref reader);
                try
                {
                    FrameFormatterSerializationOptions frameOptions = options.GetOptionParams();
                    if (frameOptions.MthWorkerConfig.MaxConcurrentTasks == 1 || ParallelGatekeeperSingleton.wrapperDepth > 1)
                        return DeserializeSynchronous(ref reader, options);

                    var readerBackup = reader.CreatePeekReader();
                    int count = reader.ReadArrayHeader();
                    if (count == 0)
                    {
                        reader = readerBackup;
                        return DeserializeSynchronous(ref reader, options);
                    }
                    var peekreader = reader.CreatePeekReader();
                    if (FrameItemFormatter<T>.ReadElementHeader(ref peekreader) == Frame<T>.unassigned)
                    {
                        if (frameOptions.ThrowOnUnnasignedFrameDeserialization)
                            throw new StreamSerializationException($"Unassigned buffer length found during parallel deserialize for {nameof(TFrameList)}");
                        reader = readerBackup;
                        return DeserializeSynchronous(ref reader, options);
                    }

                    IMessagePackFormatter<T> formatterT = options.Resolver.GetFormatterWithVerify<T>();
                    ListFrameWrapper valueWrapper = GetTFrameListWrapper(count);
                    Frame<T>[] resItems = valueWrapper.AsFrameArray();
                    BatchSizeEstimator batchEstimator = new BatchSizeEstimator(frameOptions.BatchSizeEstimatorConfig);

                    void ProcessBatch(BatchWithBufferWritersAndElementOffset batch, CancellationToken token)
                    {
                        try
                        {
                            ReadOnlySpan<int> lengths = batch.buffers.lengths.WrittenSpan;
                            ReadOnlyMemory<byte> bodies = batch.buffers.concatenatedBodies.WrittenMemory;
                            int batchSize = batch.buffers.lengths.WrittenCount;
                            var destSpan = resItems.AsSpan(batch.offset, batchSize);

                            for (int ix = 0, bodyStartIx = 0; ix < batchSize; ix++)
                            {
                                int itemLen = lengths[ix];
                                ReadOnlyMemory<byte> body = bodies.Slice(bodyStartIx, itemLen);
                                MessagePackReader tmpReader = new MessagePackReader(body) { CancellationToken = token };
                                T res = formatterT.Deserialize(ref tmpReader, options);
                                destSpan[ix] = new Frame<T>((int)body.Length, res);
                                bodyStartIx += itemLen;
                            }
                        }
                        finally
                        {
                            objPoolBufferWriterBodies.Return(batch.buffers.concatenatedBodies);
                            objPoolBufferWriterBodyLengths.Return(batch.buffers.lengths);
                        }
                    }

                    using (var mtw = new MultiThreadedWorker<BatchWithBufferWritersAndElementOffset>(
                        frameOptions.MthWorkerConfig, ProcessBatch))
                    {
                        int i = 0;
                        while (i < count)
                        {
                            int batchSize = Math.Min(count - i, batchEstimator.RecomendedBatchSize);
                            var currentBatch = new BatchWithBufferWritersAndElementOffset()
                            {
                                offset = i,
                                buffers = new BatchWithBufferWriters()
                                {
                                    concatenatedBodies = objPoolBufferWriterBodies.Get(),
                                    lengths = objPoolBufferWriterBodyLengths.Get()
                                }
                            };
                            for (int seqIx = 0; seqIx < batchSize; seqIx++)
                            {
                                int itemLength = FrameItemFormatter<T>.ReadElementHeader(ref reader);
                                if (itemLength == Frame<T>.unassigned)
                                    throw new StreamSerializationException($"Unassigned buffer length found during parallel deserialize for {nameof(TFrameList)}");
                                currentBatch.buffers.lengths.GetSpan(1)[0] = itemLength;
                                currentBatch.buffers.lengths.Advance(1);
                                ReadOnlySequence<byte> raw = reader.ReadRaw(itemLength);
                                raw.CopyTo(currentBatch.buffers.concatenatedBodies.GetSpan(itemLength));
                                currentBatch.buffers.concatenatedBodies.Advance(itemLength);
                                batchEstimator.UpdateEstimate(itemLength);
                            }
                            mtw.AddWorkItem(currentBatch, reader.CancellationToken);
                            i += batchSize;
                        }
                    }
                    return valueWrapper.AsFrameList();
                }
                finally
                {
                    reader.Depth--;
                }
            }
            finally
            {
                Interlocked.Decrement(ref ParallelGatekeeperSingleton.wrapperDepth);
            }
        }

        private TFrameList DeserializeSynchronous(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            int count = reader.ReadArrayHeader();
            ListFrameWrapper valueWrapper = GetTFrameListWrapper(count);
            if (count > 0)
            {
                FrameFormatterSerializationOptions frameOptions = options.GetOptionParams();
                Frame<T>[] resItems = valueWrapper.AsFrameArray();
                var formatter = options.Resolver.GetFormatterWithVerify<T>();
                for (int i = 0; i < count; i++)
                {
                    resItems[i] = FrameItemFormatter<T>.Deserialize(ref reader, options, frameOptions, formatter);
                }
            }
            return valueWrapper.AsFrameList();
        }
    }
}
