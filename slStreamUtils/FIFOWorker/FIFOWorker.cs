/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils.FIFOWorker
{
    public delegate Task<TWorkItemOut> ProcessOutOfOrderWorkItemDelegateAsync<TWorkItemIn, TWorkItemOut>(TWorkItemIn item, CancellationToken token);

    public sealed class FIFOWorker<TWorkItemIn, TWorkItemOut> : IDisposable, IAsyncDisposable
    {
        private readonly ProcessOutOfOrderWorkItemDelegateAsync<TWorkItemIn, TWorkItemOut> processOutOfOrderWorkItemAsync;
        private readonly FIFOWorkerConfig config;
        private readonly System.Collections.IDictionary finishedUnsortedBuffers;
        private SemaphoreSlim threadLimiter;
        private int nextWorkIx;
        private int nextExpectedWorkIx;
        private List<Task<Tuple<int, TWorkItemOut>>> pendingTasks;
        private bool disposed = false;

        public FIFOWorker(FIFOWorkerConfig config, ProcessOutOfOrderWorkItemDelegateAsync<TWorkItemIn, TWorkItemOut> processOutOfOrderWorkItemAsync)
        {
            if (config.MaxQueuedItems <= 0)
                throw new ArgumentOutOfRangeException(nameof(config.MaxQueuedItems));
            if (config.TotThreads < 0)
                throw new ArgumentOutOfRangeException(nameof(config.TotThreads));
            if (config.TotThreads > config.MaxQueuedItems)
                throw new ArgumentOutOfRangeException(nameof(config.TotThreads), $"{nameof(config.TotThreads)} mustn't exceed {nameof(config.MaxQueuedItems)}");
            this.processOutOfOrderWorkItemAsync = processOutOfOrderWorkItemAsync;
            this.config = config;
            threadLimiter = new SemaphoreSlim(config.MaxQueuedItems);
            nextWorkIx = 0;
            nextExpectedWorkIx = 0;
            pendingTasks = new List<Task<Tuple<int, TWorkItemOut>>>();
            if (config.TotThreads >= 10)
                finishedUnsortedBuffers = new Dictionary<int, TWorkItemOut>();
            else
                finishedUnsortedBuffers = new System.Collections.Specialized.ListDictionary();
        }

        public async IAsyncEnumerable<TWorkItemOut> AddWorkItemAsync(TWorkItemIn inputWorkItem, [EnumeratorCancellation] CancellationToken token)
        {
            int itemIx = nextWorkIx++;
            if (config.TotThreads == 0)
            {
                yield return await processOutOfOrderWorkItemAsync(inputWorkItem, token).ConfigureAwait(false);
                yield break;
            }
            if (pendingTasks.Count >= config.MaxQueuedItems)
                await foreach (var outputWorkItem in ProcessOnePendingItem(token).ConfigureAwait(false))
                    yield return outputWorkItem;
            pendingTasks.Add(Task.Run(() => InputToOutputProcessorAsync(itemIx, inputWorkItem, token), cancellationToken: token));
        }

        public async IAsyncEnumerable<TWorkItemOut> FlushAsync([EnumeratorCancellation] CancellationToken token)
        {
            while (pendingTasks.Count > 0)
                await foreach (var outputWorkItem in ProcessOnePendingItem(token).ConfigureAwait(false))
                    yield return outputWorkItem;
        }

        private async IAsyncEnumerable<TWorkItemOut> ProcessOnePendingItem([EnumeratorCancellation] CancellationToken token)
        {
            Task<Tuple<int, TWorkItemOut>> outOfOrderTask = await Task.WhenAny(pendingTasks.ToArray()).WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            Tuple<int, TWorkItemOut> finishedUnsortedItem;
            try
            {
                finishedUnsortedItem = await outOfOrderTask.WaitAsync(token).ConfigureAwait(false);
            }
            finally
            {
                pendingTasks.Remove(outOfOrderTask);
            }
            token.ThrowIfCancellationRequested();
            foreach (var workItem in AppendOutputSorted(finishedUnsortedItem.Item1, finishedUnsortedItem.Item2))
                yield return workItem;
        }

        private async Task<Tuple<int, TWorkItemOut>> InputToOutputProcessorAsync(int itemIx, TWorkItemIn item, CancellationToken token)
        {
            try
            {
                await threadLimiter.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    return new Tuple<int, TWorkItemOut>(itemIx, await processOutOfOrderWorkItemAsync(item, token).ConfigureAwait(false));
                }
                finally
                {
                    threadLimiter.Release();
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken.Equals(token) && token.IsCancellationRequested)
                    return await Task.FromCanceled<Tuple<int, TWorkItemOut>>(token).ConfigureAwait(false);
                throw;
            }
        }

        private IEnumerable<TWorkItemOut> AppendOutputSorted(int workIx, TWorkItemOut finishedUnsortedItem)
        {
            if (workIx == nextExpectedWorkIx)
            {
                nextExpectedWorkIx++;
                yield return finishedUnsortedItem;
            }
            else
            {
                finishedUnsortedBuffers.Add(workIx, finishedUnsortedItem);
            }

            while (finishedUnsortedBuffers.Contains(nextExpectedWorkIx))
            {
                TWorkItemOut finishedWork = (TWorkItemOut)finishedUnsortedBuffers[nextExpectedWorkIx];
                finishedUnsortedBuffers.Remove(nextExpectedWorkIx);
                nextExpectedWorkIx++;
                yield return finishedWork;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Task.WaitAll(pendingTasks.ToArray());
                    }
                    catch (AggregateException ag)
                    {
                        foreach (var e in ag.InnerExceptions)
                            if (!(e is TaskCanceledException))
                                throw;
                    }
                    if (pendingTasks != null)
                        foreach (var t in pendingTasks)
                            t.Dispose();
                    threadLimiter?.Dispose();
                }
                disposed = true;
            }
            pendingTasks = null;
            threadLimiter = null;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        private async ValueTask DisposeAsyncCore()
        {
            try
            {
                await Task.WhenAll(pendingTasks.ToArray()).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            if (pendingTasks != null)
                foreach (var t in pendingTasks)
                    t.Dispose();
            threadLimiter?.Dispose();
        }
    }
}
