/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using MessagePack;
using MessagePack.Formatters;
using Microsoft.Toolkit.HighPerformance.Buffers;
using slStreamUtils;
using System;
using System.Threading;

namespace slStreamUtilsMessagePack.Formatters
{
    internal abstract partial class FrameCollectionFormatter<TFrameList, T>
    {
        public void Serialize(ref MessagePackWriter writer, TFrameList value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            Interlocked.Increment(ref ParallelGatekeeperSingleton.wrapperDepth);
            try
            {
                FrameFormatterSerializationOptions frameOptions = options.GetOptionParams();

                if (frameOptions.FIFOWorkerConfig.MaxConcurrentTasks < 1 || ParallelGatekeeperSingleton.wrapperDepth != 1)
                {
                    SerializeSynchronous(ref writer, value, options);
                    return;
                }
                int count = value.Count;
                writer.WriteArrayHeader(count);
                BatchSizeEstimator batchEstimator = new BatchSizeEstimator(frameOptions.BatchSizeEstimatorConfig);
                IMessagePackFormatter<T> formatterT = options.Resolver.GetFormatterWithVerify<T>();
                bool isOldSpec = writer.OldSpec;

                BatchWithBufferWriters ProcessItems(ArraySegment<Frame<T>> batch, CancellationToken token)
                {
                    BatchWithBufferWriters batchOut = new BatchWithBufferWriters();
                    batchOut.concatenatedBodies = objPoolBufferWriterBodies.Get();
                    batchOut.lengths = objPoolBufferWriterBodyLengths.Get();
                    MessagePackWriter writerBody = new MessagePackWriter(batchOut.concatenatedBodies) { OldSpec = isOldSpec, CancellationToken = token };
                    var spanIn = batch.AsSpan();
                    int prevWrittenBytesCount = 0;
                    for (int ix = 0; ix < spanIn.Length; ix++)
                    {
                        formatterT.Serialize(ref writerBody, spanIn[ix], options);
                        writerBody.Flush();
                        int objLen = batchOut.concatenatedBodies.WrittenCount - prevWrittenBytesCount;
                        prevWrittenBytesCount = batchOut.concatenatedBodies.WrittenCount;
                        batchOut.lengths.GetSpan(1)[0] = objLen;
                        batchOut.lengths.Advance(1);
                        batchEstimator.UpdateEstimate(objLen);
                    }
                    return batchOut;
                }

                ListFrameWrapper valueWrapper = GetTFrameListWrapper(value);

                Frame<T>[] valueArray = valueWrapper.AsFrameArray();
                using (var fifow = new FIFOWorker<ArraySegment<Frame<T>>, BatchWithBufferWriters>(frameOptions.FIFOWorkerConfig, ProcessItems))
                {
                    int i = 0;
                    while (i < count)
                    {
                        int batchSize = Math.Min(count - i, batchEstimator.RecomendedBatchSize);
                        if (batchSize <= 0)
                            throw new StreamSerializationException($"Invalid batch sequence length: {batchSize}");
                        ArraySegment<Frame<T>> sourceSegment = new ArraySegment<Frame<T>>(valueArray, i, batchSize);
                        foreach (BatchWithBufferWriters batchOutput in fifow.AddWorkItem(sourceSegment, writer.CancellationToken))
                            BatchToStream(ref writer, batchOutput);

                        i += batchSize;
                    }
                    foreach (BatchWithBufferWriters batchOutput in fifow.Flush(writer.CancellationToken))
                        BatchToStream(ref writer, batchOutput);
                }

            }
            finally
            {
                Interlocked.Decrement(ref ParallelGatekeeperSingleton.wrapperDepth);
            }
        }

        private void BatchToStream(ref MessagePackWriter writer, BatchWithBufferWriters batch)
        {
            try
            {
                int GetItemLength(int lenghtAtIx)
                {
                    ReadOnlySpan<int> lengths = batch.lengths.WrittenSpan;
                    return lengths[lenghtAtIx];
                }
                int batchSize = batch.lengths.WrittenCount;
                ReadOnlySpan<byte> bodySpan = batch.concatenatedBodies.WrittenMemory.Span;
                for (int ix = 0, bodyStartIx = 0; ix < batchSize; ix++)
                {
                    int itemLen = GetItemLength(ix);

                    writer.WriteArrayHeader(Frame<T>.parallelItemTotElems);
                    writer.Write((uint)itemLen);
                    writer.WriteRaw(bodySpan.Slice(bodyStartIx, itemLen));
                    bodyStartIx += itemLen;
                }
            }
            finally
            {
                objPoolBufferWriterBodies.Return(batch.concatenatedBodies);
                objPoolBufferWriterBodyLengths.Return(batch.lengths);
            }
        }

        private void SerializeSynchronous(ref MessagePackWriter writer, TFrameList list, MessagePackSerializerOptions options)
        {
            int count = list.Count;
            writer.WriteArrayHeader(count);
            var formatterT = options.Resolver.GetFormatterWithVerify<T>();
            ArrayPoolBufferWriter<byte> bodyWriter = objPoolBufferWriterBodies.Get();
            try
            {
                foreach (Frame<T> item in list)
                {
                    FrameItemFormatter<T>.Serialize(ref writer, item, options, bodyWriter, formatterT);
                }
            }
            finally
            {
                objPoolBufferWriterBodies.Return(bodyWriter);
            }
        }


    }
}
