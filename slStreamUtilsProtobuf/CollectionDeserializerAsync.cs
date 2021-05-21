/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using slStreamUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;
using slStreamUtils.ObjectPoolPolicy;
using Microsoft.Extensions.ObjectPool;
using ProtoBuf.Meta;
using ProtoBuf;

namespace slStreamUtilsProtobuf
{
    public class CollectionDeserializerAsync<T> : IDisposable
    {
        private ObjectPool<ArrayPoolBufferWriter<byte>> objPoolBufferWriterSerializedBatch;
        private ObjectPool<List<int>> objPoolList;
        private FIFOWorker<BatchIn, BatchOut> fifow;
        private int desiredBatchSize_bytes;
        private TypeModel typeModel;
        private Type t_ParallelServices_ArrayWrapper;

        private struct BatchIn
        {
            public List<int> Lengths;
            public ArrayPoolBufferWriter<byte> concatenatedBodies;
        }
        private struct BatchOut
        {
            public T[] elements;
        }

        public CollectionDeserializerAsync(FIFOWorkerConfig fifowConfig, int desiredBatchSize_bytes = 1024 * 64) :
            this(fifowConfig, RuntimeTypeModel.Default, desiredBatchSize_bytes)
        {
        }
        public CollectionDeserializerAsync(FIFOWorkerConfig fifowConfig, TypeModel typeModel, int desiredBatchSize_bytes = 1024 * 64)
        {
            this.desiredBatchSize_bytes = desiredBatchSize_bytes;
            fifow = new FIFOWorker<BatchIn, BatchOut>(fifowConfig, HandleWorkerOutput);
            this.typeModel = typeModel;
            t_ParallelServices_ArrayWrapper = typeof(ParallelServices_ArrayWrapper<T>);
            typeModel.SetupParallelServices<T>();
            objPoolBufferWriterSerializedBatch = new DefaultObjectPool<ArrayPoolBufferWriter<byte>>(
                new ArrayPoolBufferWriterObjectPoolPolicy<byte>(Math.Max(1024 * 64, desiredBatchSize_bytes)),
                fifowConfig.MaxQueuedItems);
            objPoolList = new DefaultObjectPool<List<int>>(new ListObjectPoolPolicy<int>(64), fifowConfig.MaxQueuedItems);
        }

        public async IAsyncEnumerable<T> DeserializeAsync(Stream stream, [EnumeratorCancellation] CancellationToken token = default)
        {
            BatchIn currentBatch = new BatchIn();
            int currentBatchTotalSize = 0;
            int currentBatchTotalElements = 0;

            while (TryReadHeader(stream, out int itemLength))
            {
                if (currentBatchTotalSize + itemLength > desiredBatchSize_bytes && currentBatchTotalElements > 0)
                {
                    // send prev batch
                    foreach (T t in IterateOutputBatch(fifow.AddWorkItem(currentBatch, token)))
                        yield return t;
                    currentBatchTotalSize = 0;
                    currentBatchTotalElements = 0;
                }
                if (currentBatchTotalElements == 0)
                {
                    currentBatch.concatenatedBodies = objPoolBufferWriterSerializedBatch.Get();
                    currentBatch.Lengths = objPoolList.Get();
                }
                await BufferFromStreamAsync(stream, currentBatch.concatenatedBodies, itemLength, token).ConfigureAwait(false);
                currentBatchTotalSize += itemLength;
                currentBatchTotalElements++;
                currentBatch.Lengths.Add(itemLength);
            }
            if (currentBatchTotalElements > 0) // send unfinished batch
                foreach (T t in IterateOutputBatch(fifow.AddWorkItem(currentBatch, token)))
                    yield return t;
            foreach (T t in IterateOutputBatch(fifow.Flush(token)))
                yield return t;
        }

        private IEnumerable<T> IterateOutputBatch(IEnumerable<BatchOut> outBatches)
        {
            foreach (BatchOut batch in outBatches)
            {
                for (int itemIx = 0; itemIx < batch.elements.Length; itemIx++)
                    yield return batch.elements[itemIx];
            }
        }

        private bool TryReadHeader(Stream s, out int length)
        {
            length = ProtoReader.ReadLengthPrefix(s, true, PrefixStyle.Base128, out int fieldNumber, out int bytesRead);
            if (bytesRead == 0)
            {
                length = 0;
                return false;
            }
            if (fieldNumber != TypeModel.ListItemTag)
                throw new StreamSerializationException($"invalid proto item tag found: {fieldNumber}, expected {TypeModel.ListItemTag}");
            return true;
        }

        private void WriteHeader(ArrayPoolBufferWriter<byte> buf, int length)
        {
            const int maxBufSizeVarint32 = 5;
            Span<byte> span = buf.GetSpan(maxBufSizeVarint32 + 1);
            span[0] = ProtobufConsts.protoRepeatedTag1;
            int totWritten = LocalWriteVarint32((uint)length, 1, span) + 1;
            buf.Advance(totWritten);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocalWriteVarint32(uint value, int index, Span<byte> span)
        {
            // snippet taken from protobuf-net's int LocalWriteVarint32(uint value)
            int count = 0;
            do
            {
                span[index++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            span[index - 1] &= 0x7F;
            return count;
        }

        private BatchOut HandleWorkerOutput(BatchIn batch, CancellationToken token)
        {
            try
            {
                var obj = typeModel.Deserialize(t_ParallelServices_ArrayWrapper, batch.concatenatedBodies.WrittenSpan);
                if (obj is ParallelServices_ArrayWrapper<T> arrWT)
                    return new BatchOut() { elements = arrWT.Array };
                else throw new StreamSerializationException($"Invalid deserialized element type. Expected {t_ParallelServices_ArrayWrapper}, got [{(obj is null ? "null" : obj.GetType().ToString())}]");
            }
            finally
            {
                objPoolBufferWriterSerializedBatch.Return(batch.concatenatedBodies);
                objPoolList.Return(batch.Lengths);
            }
        }

        private async Task BufferFromStreamAsync(Stream stream, ArrayPoolBufferWriter<byte> buf, int length, CancellationToken token)
        {
            WriteHeader(buf, length);
            Memory<byte> mem = buf.GetMemory(length).Slice(0, length);
            int totRead = await stream.ReadAsync(mem, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (totRead != length)
                throw new StreamSerializationException($"Unexpected length read while deserializing body. Expected {length}, got {totRead}");
            buf.Advance(length);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                fifow.Dispose();
            }
            fifow = null;
            objPoolBufferWriterSerializedBatch = null;
            objPoolList = null;
            typeModel = null;
            t_ParallelServices_ArrayWrapper = null;
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
