/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils
{
    public delegate TWorkItemOut ProcessOutOfOrderWorkItemDelegate<TWorkItemIn, TWorkItemOut>(TWorkItemIn item, CancellationToken token);

    // single producer/consumer, multiple workers, order preserved
    public sealed class FIFOWorker<TWorkItemIn, TWorkItemOut> : IDisposable
    {
        private class TWorkItemInWrapper
        {
            public int ix;
            public TWorkItemIn item;
            public bool flushRequested;
            public bool abortRequested;
            public CancellationToken token;

            public TWorkItemInWrapper(int ix, TWorkItemIn item, CancellationToken token,
                bool flushRequested = false, bool abortRequested = false)
            {
                this.ix = ix;
                this.item = item;
                this.token = token;
                this.flushRequested = flushRequested;
                this.abortRequested = abortRequested;
            }
        }
        private class TWorkItemOutWrapper
        {
            public TWorkItemInWrapper itemIn;
            public TWorkItemOut itemOut;
            public TWorkItemOutWrapper(TWorkItemInWrapper itemIn, TWorkItemOut itemOut)
            {
                this.itemIn = itemIn;
                this.itemOut = itemOut;
            }
        }

        private readonly ProcessOutOfOrderWorkItemDelegate<TWorkItemIn, TWorkItemOut> processOutOfOrderWorkItem;
        private List<Task> workerTasks;
        private BlockingCollection<TWorkItemInWrapper> inputSortedItems;
        private SemaphoreSlim inputSortedItemsLimiter;
        private Dictionary<int, TWorkItemOutWrapper> outputUnsortedItems;
        private readonly object outputUnsortedItemsLocker = new object();
        private int maxOutputUnsortedQueuedItems;
        private BlockingCollection<TWorkItemOutWrapper> outputSortedItems;
        private int nextWorkIx;
        private int nextExpectedItemIx;
        private volatile bool disposed = false;
        private volatile bool aborted = false;

        public FIFOWorker(FIFOWorkerConfig config, ProcessOutOfOrderWorkItemDelegate<TWorkItemIn, TWorkItemOut> processOutOfOrderWorkItem)
        {
            this.processOutOfOrderWorkItem = processOutOfOrderWorkItem;
            nextWorkIx = 0;
            nextExpectedItemIx = 0;
            inputSortedItems = new BlockingCollection<TWorkItemInWrapper>();
            inputSortedItemsLimiter = new SemaphoreSlim(config.MaxInputSortedQueuedItems); // flush prevents us from using inputSortedItems's boundedCapacity
            outputSortedItems = new BlockingCollection<TWorkItemOutWrapper>(config.MaxOutputSortedQueuedItems);
            maxOutputUnsortedQueuedItems = config.MaxOutputUnsortedQueuedItems;
            outputUnsortedItems = new Dictionary<int, TWorkItemOutWrapper>();
            workerTasks = new List<Task>();
            for (int i = 0; i < config.MaxConcurrentTasks; i++)
                workerTasks.Add(Task.Run(WorkerThreadEntrypoint));
        }

        public IEnumerable<TWorkItemOut> AddWorkItem(TWorkItemIn inputWorkItem, CancellationToken token)
        {
            while (outputSortedItems.Count > 0)
            {
                TWorkItemOut itemOut = default;
                AbortTasksOnTokenCancellation(token, () => { itemOut = outputSortedItems.Take(token).itemOut; });
                yield return itemOut;
            }
            while (inputSortedItemsLimiter.CurrentCount == 0)
            {
                TWorkItemOut itemOut = default;
                AbortTasksOnTokenCancellation(token, () => { itemOut = outputSortedItems.Take(token).itemOut; });
                yield return itemOut;
            }
            AbortTasksOnTokenCancellation(token, () =>
            {
                inputSortedItemsLimiter.Wait(token);
                int itemIx = nextWorkIx++;
                inputSortedItems.Add(new TWorkItemInWrapper(itemIx, inputWorkItem, token), token);
            });
            while (outputSortedItems.Count > 0)
            {
                TWorkItemOut itemOut = default;
                AbortTasksOnTokenCancellation(token, () => { itemOut = outputSortedItems.Take(token).itemOut; });
                yield return itemOut;
            }
        }

        public IEnumerable<TWorkItemOut> Flush(CancellationToken token)
        {
            int itemIx = nextWorkIx++;
            AbortTasksOnTokenCancellation(token, () => { inputSortedItems.Add(new TWorkItemInWrapper(itemIx, default, token, flushRequested: true), token); });
            while (true)
            {
                TWorkItemOutWrapper item = default;
                AbortTasksOnTokenCancellation(token, () => { item = outputSortedItems.Take(token); });
                if (item.itemIn.flushRequested)
                    break;
                yield return item.itemOut;
            }
        }

        private void WorkerThreadEntrypoint()
        {
            try
            {
                while (true)
                {
                    TWorkItemInWrapper itemIn;

                    itemIn = inputSortedItems.Take();
                    if (itemIn.abortRequested)
                    {
                        inputSortedItems.Add(itemIn);
                        return;
                    }
                    if (!itemIn.flushRequested)
                        inputSortedItemsLimiter.Release();

                    TWorkItemOutWrapper itemOut = ProcessItem(itemIn);

                    lock (outputUnsortedItemsLocker)
                    {
                        while (
                            !aborted &&
                            !itemIn.flushRequested &&
                            itemIn.ix != nextExpectedItemIx &&
                            outputUnsortedItems.Count > maxOutputUnsortedQueuedItems)
                        {
                            Monitor.Wait(outputUnsortedItemsLocker);
                        }
                        if (aborted)
                            return;
                        if (itemIn.ix == nextExpectedItemIx)
                        {
                            ClearUpSpaceInUnsortedQueue(itemOut);
                            Monitor.PulseAll(outputUnsortedItemsLocker);
                        }
                        else
                        {
                            outputUnsortedItems.Add(itemIn.ix, itemOut);
                        }
                    }

                }
            }
            catch
            {
                RequestAbort();
                throw;
            }
        }

        private TWorkItemOutWrapper ProcessItem(TWorkItemInWrapper itemIn)
        {
            TWorkItemOutWrapper itemOut;
            if (itemIn.flushRequested)
            {
                itemOut = new TWorkItemOutWrapper(itemIn, default);
            }
            else
            {
                TWorkItemOut processedItem = processOutOfOrderWorkItem(itemIn.item, itemIn.token);
                itemIn.token.ThrowIfCancellationRequested();
                itemOut = new TWorkItemOutWrapper(itemIn, processedItem);
            }
            return itemOut;
        }

        private void ClearUpSpaceInUnsortedQueue(TWorkItemOutWrapper itemOut)
        {
            outputSortedItems.Add(itemOut);
            nextExpectedItemIx++;
            while (outputUnsortedItems.ContainsKey(nextExpectedItemIx))
            {
                itemOut = outputUnsortedItems[nextExpectedItemIx];
                outputUnsortedItems.Remove(nextExpectedItemIx);
                outputSortedItems.Add(itemOut);
                nextExpectedItemIx++;
            }
        }

        void AbortTasksOnTokenCancellation(CancellationToken token, Action act)
        {
            if (aborted)
                throw new FIFOWorkerException("The consumer is in a cancelled state. No further work allowed.");
            try
            {
                act(); // calling this may also throw OperationCanceledException
                token.ThrowIfCancellationRequested();
            }
            catch (InvalidOperationException)
            {
                if (aborted && outputSortedItems.IsCompleted)
                {
                    Dispose();
                    return;
                }
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken.Equals(token))
                    RequestAbort();
                throw;
            }
        }

        private void RequestAbort()
        {
            if (aborted)
                return;
            aborted = true;
            inputSortedItems?.Add(new TWorkItemInWrapper(default, default, default, abortRequested: true));
            lock (outputUnsortedItemsLocker)
                Monitor.PulseAll(outputUnsortedItemsLocker);
            outputSortedItems.CompleteAdding();
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
                disposed = true;
                if (disposing)
                {
                    RequestAbort();
                    Task.WaitAll(workerTasks?.ToArray());
                    workerTasks?.Clear();
                    inputSortedItems?.Dispose();
                    outputSortedItems?.Dispose();
                    inputSortedItemsLimiter?.Dispose();
                }
                workerTasks = null;
                inputSortedItems = null;
                outputUnsortedItems = null;
                outputSortedItems = null;
                inputSortedItemsLimiter = null;
            }
        }
    }
}
