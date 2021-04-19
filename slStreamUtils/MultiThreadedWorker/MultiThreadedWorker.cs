/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtils
{
    public delegate void ProcessWorkItemDelegate<TWorkItemIn>(TWorkItemIn item, CancellationToken token);
    public sealed class MultiThreadedWorker<TWorkItemIn> : IDisposable
    {
        private readonly ProcessWorkItemDelegate<TWorkItemIn> processWorkItem;
        private readonly MultiThreadedWorkerConfig config;
        private SemaphoreSlim threadLimiter;
        private List<Task> pendingTasks;
        private bool disposed = false;

        public MultiThreadedWorker(MultiThreadedWorkerConfig config, ProcessWorkItemDelegate<TWorkItemIn> processWorkItem)
        {
            if (config.MaxQueuedItems <= 0)
                throw new ArgumentOutOfRangeException(nameof(config.MaxQueuedItems));
            if (config.MaxConcurrentTasks < 0)
                throw new ArgumentOutOfRangeException(nameof(config.MaxConcurrentTasks));
            if (config.MaxConcurrentTasks > config.MaxQueuedItems)
                throw new ArgumentOutOfRangeException(nameof(config.MaxConcurrentTasks), $"{nameof(config.MaxConcurrentTasks)} mustn't exceed {nameof(config.MaxQueuedItems)}");
            this.processWorkItem = processWorkItem;
            this.config = config;
            threadLimiter = new SemaphoreSlim(config.MaxConcurrentTasks);
            pendingTasks = new List<Task>();
        }

        public void AddWorkItem(TWorkItemIn inputWorkItem, CancellationToken token)
        {
            if (config.MaxConcurrentTasks == 0)
            {
                processWorkItem(inputWorkItem, token);
                return;
            }
            if (pendingTasks.Count >= config.MaxQueuedItems)
                ProcessOnePendingItem(token);

            pendingTasks.Add(Task.Run(() => InputProcessor(inputWorkItem, token), cancellationToken: token));
        }

        public void Flush(CancellationToken token)
        {
            if (pendingTasks.Count > 0)
            {
                Task.WaitAll(pendingTasks.ToArray(), token);
                pendingTasks.Clear();
            }
        }

        private void ProcessOnePendingItem(CancellationToken token)
        {
            int taskIx = Task.WaitAny(pendingTasks.ToArray(), token);
            pendingTasks.RemoveAt(taskIx);
            token.ThrowIfCancellationRequested();
        }

        private void InputProcessor(TWorkItemIn item, CancellationToken token)
        {
            threadLimiter.Wait(token);
            try
            {
                token.ThrowIfCancellationRequested();
                processWorkItem(item, token);
            }
            finally
            {
                threadLimiter.Release();
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

    }
}
