/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.HighPerformance;
using ProtoBuf.Meta;
using slStreamUtils;
using slStreamUtils.ObjectPoolPolicy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsProtobuf
{
    public class CollectionSerializerAsync<T> : IDisposable, IAsyncDisposable
    {
        private readonly Stream stream;
        private FIFOWorker<List<T>, MemoryStream> fifow;
        private BatchSizeEstimator batchEstimator;
        private ObjectPool<List<T>> objPoolList;
        private ObjectPool<MemoryStream> objPoolMemoryStream;
        private List<T> currentBatch;
        private int desiredBatchSize;
        private TypeModel typeModel;

        public CollectionSerializerAsync(Stream stream, FIFOWorkerConfig fifowConfig) :
            this(stream, fifowConfig, new BatchSizeEstimatorConfig(), RuntimeTypeModel.Default)
        { }

        public CollectionSerializerAsync(Stream stream, FIFOWorkerConfig fifowConfig, TypeModel typeModel) :
            this(stream, fifowConfig, new BatchSizeEstimatorConfig(), typeModel)
        { }

        public CollectionSerializerAsync(Stream stream, FIFOWorkerConfig fifowConfig, BatchSizeEstimatorConfig estimatorConfig, TypeModel typeModel)
        {

            this.stream = stream;
            fifow = new FIFOWorker<List<T>, MemoryStream>(fifowConfig, HandleWorkerOutput);
            this.typeModel = typeModel;
            typeModel.SetupParallelServices<T>();
            batchEstimator = new BatchSizeEstimator(estimatorConfig);
            objPoolList = new DefaultObjectPool<List<T>>(new ListObjectPoolPolicy<T>(64), fifowConfig.MaxQueuedItems);
            objPoolMemoryStream = new DefaultObjectPool<MemoryStream>(
                new MemoryStreamObjectPoolPolicy(Math.Max(1024 * 64, estimatorConfig.DesiredBatchSize_bytes)),
                fifowConfig.MaxQueuedItems);
            desiredBatchSize = 1;
            currentBatch = objPoolList.Get();
        }

        public Task SerializeAsync(Frame<T> obj, CancellationToken token = default)
        {
            return SerializeAsync(obj.Item, token);
        }
        public async Task SerializeAsync(T t, CancellationToken token = default)
        {
            currentBatch.Add(t);
            await CompleteBatch(false, token).ConfigureAwait(false);
        }

        public async Task FlushAsync(CancellationToken token = default)
        {
            await CompleteBatch(true, token).ConfigureAwait(false);
            foreach (var ms in fifow.Flush(token))
                await BatchToStreamAsync(ms, token).ConfigureAwait(false);
        }

        private async Task CompleteBatch(bool flushBatch, CancellationToken token)
        {
            if (flushBatch || currentBatch.Count >= desiredBatchSize)
            {
                foreach (var bw in fifow.AddWorkItem(currentBatch, token))
                    await BatchToStreamAsync(bw, token).ConfigureAwait(false);
                currentBatch = objPoolList.Get();
                desiredBatchSize = batchEstimator.RecomendedBatchSize;
            }
        }

        private MemoryStream HandleWorkerOutput(List<T> batch, CancellationToken token)
        {
            try
            {
                MemoryStream ms = objPoolMemoryStream.Get();

                typeModel.Serialize(ms, new ParallelServices_ListWrapper<T>(batch));
                if (batch.Count > 0)
                    batchEstimator.UpdateEstimate((float)ms.Position / (float)batch.Count); // update with avg instead of updating for every loop item. It's not exact, but it's faster

                return ms;
            }
            finally
            {
                objPoolList.Return(batch);
            }
        }

        private async Task BatchToStreamAsync(MemoryStream batchOut, CancellationToken token)
        {
            try
            {
                await stream.WriteAsync(batchOut.GetBuffer().AsMemory(0, (int)batchOut.Position), token);
            }
            finally
            {
                objPoolMemoryStream.Return(batchOut);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FlushAsync().Wait();
                fifow.Dispose();
            }
            fifow = null;
            batchEstimator = null;
            objPoolList = null;
            objPoolMemoryStream = null;
            currentBatch = null;
            typeModel = null;
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
            fifow.Dispose();
        }
    }
}
