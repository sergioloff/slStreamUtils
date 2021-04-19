/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using NUnit.Framework;
using slStreamUtils.FIFOWorker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [TestFixture]
    public class FIFOWorkerEnumTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private FIFOWorkerConfig GetConfig(int totThreads, int maxQueuedItems)
        {
            return new FIFOWorkerConfig(totThreads, maxQueuedItems);
        }
        public class MockException : Exception
        { }
        public class MockWorkIn
        {
            public int ix;

            public MockWorkIn(int ix)
            {
                this.ix = ix;
            }
            public override string ToString()
            {
                return $"{nameof(ix)}={ix}";
            }
        }

        public class MockWorkOut
        {
            public MockWorkIn originalInputItem;

            public MockWorkOut(MockWorkIn originalInputItem)
            {
                this.originalInputItem = originalInputItem;
            }
            public override string ToString()
            {
                return $"{nameof(originalInputItem)}.{nameof(originalInputItem.ix)}={originalInputItem.ix}";
            }
        }

        public class MockWorker
        {
            public ConcurrentQueue<Tuple<bool, MockWorkIn, MockWorkOut>> doneWork = new ConcurrentQueue<Tuple<bool, MockWorkIn, MockWorkOut>>();
            public TaskCompletionSource<object> getBarAsyncReady1 = new TaskCompletionSource<object>();
            public TaskCompletionSource<object> getBarAsyncReady2 = new TaskCompletionSource<object>();
            public TaskCompletionSource<object> getBarAsyncContinue1 = new TaskCompletionSource<object>();
            public int workDurationMs;
            public MockWorker(int workDurationMs = 10)
            {
                this.workDurationMs = workDurationMs;
            }

            public async Task<MockWorkOut> DoMockWork_Simple(MockWorkIn work, CancellationToken token)
            {
                try
                {
                    await Task.Delay(workDurationMs, token);
                    token.ThrowIfCancellationRequested();
                }
                catch
                {
                    doneWork.Enqueue(new Tuple<bool, MockWorkIn, MockWorkOut>(true, work, null));
                    throw;
                }
                var done = new MockWorkOut(work);
                doneWork.Enqueue(new Tuple<bool, MockWorkIn, MockWorkOut>(false, work, done));
                return done;
            }

            public async Task<MockWorkOut> DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                getBarAsyncReady1.SetResult(null);
                try
                {
                    await getBarAsyncContinue1.Task;
                    try
                    {
                        await Task.Delay(workDurationMs, token);
                        token.ThrowIfCancellationRequested();
                    }
                    catch
                    {
                        doneWork.Enqueue(new Tuple<bool, MockWorkIn, MockWorkOut>(true, work, null));
                        throw;
                    }
                    var done = new MockWorkOut(work);
                    doneWork.Enqueue(new Tuple<bool, MockWorkIn, MockWorkOut>(false, work, done));
                    return done;
                }
                finally
                {
                    getBarAsyncReady2.SetResult(null);
                }
            }

            public async Task TriggerOnBlockedWork(Action callback)
            {
                await getBarAsyncReady1.Task;
                callback();
                getBarAsyncContinue1.SetResult(null);
                await getBarAsyncReady2.Task;
            }
        }

        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        public async Task Dispose_CancelsPendingTasks(int totThreads, bool cancelsDuringWork)
        {
            TaskCompletionSource<object> getBarAsyncReady1 = new TaskCompletionSource<object>();
            bool completed = false;
            async Task<MockWorkOut> DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                await getBarAsyncReady1.Task;
                await Task.Delay(1, token);
                completed = true;
                return new MockWorkOut(work);
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, DoMockWorkBlocking);
            int count = await fifo.AddWorkItemAsync(new MockWorkIn(1), ts.Token).CountAsync(ts.Token);
            if (cancelsDuringWork)
                ts.Cancel();
            getBarAsyncReady1.SetResult(null);

            Assert.DoesNotThrow(fifo.Dispose);

            Assert.AreEqual(0, count);
            Assert.AreEqual(!cancelsDuringWork, completed);
        }


        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task Dispose_ExceptionInWorkerPropagates(int totThreads)
        {
            TaskCompletionSource<object> getBarAsyncReady1 = new TaskCompletionSource<object>();
            async Task<MockWorkOut> DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                await getBarAsyncReady1.Task;
                throw new MockException();
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, DoMockWorkBlocking);
            int count = await fifo.AddWorkItemAsync(new MockWorkIn(1), ts.Token).CountAsync(ts.Token);

            TestDelegate disposeDel = delegate ()
            {
                try
                {
                    getBarAsyncReady1.SetResult(null);
                    fifo.Dispose();
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }
            };

            Assert.Throws<MockException>(disposeDel);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task DisposeAsync_ExceptionInWorkerPropagates(int totThreads)
        {
            TaskCompletionSource<object> getBarAsyncReady1 = new TaskCompletionSource<object>();
            async Task<MockWorkOut> DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                await getBarAsyncReady1.Task;
                throw new MockException();
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, DoMockWorkBlocking);
            int count = await fifo.AddWorkItemAsync(new MockWorkIn(1), ts.Token).CountAsync(ts.Token);

            async Task DisposeDel()
            {
                try
                {
                    getBarAsyncReady1.SetResult(null);
                    await fifo.DisposeAsync();
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }
            };

            Assert.ThrowsAsync<MockException>(DisposeDel);
        }


        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        public async Task DisposeAsync_CancelsPendingTasks(int totThreads, bool cancelsDuringWork)
        {
            TaskCompletionSource<object> getBarAsyncReady1 = new TaskCompletionSource<object>();
            bool completed = false;
            async Task<MockWorkOut> DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                await getBarAsyncReady1.Task;
                await Task.Delay(1, token);
                completed = true;
                return new MockWorkOut(work);
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, DoMockWorkBlocking);
            int count = await fifo.AddWorkItemAsync(new MockWorkIn(1), ts.Token).CountAsync(ts.Token);
            if (cancelsDuringWork)
                ts.Cancel();
            getBarAsyncReady1.SetResult(null);

            Assert.DoesNotThrowAsync(fifo.DisposeAsync().AsTask);

            Assert.AreEqual(0, count);
            Assert.AreEqual(!cancelsDuringWork, completed);
        }


        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task AddWorkItemAsync_Cancels(int totThreads)
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, mw.DoMockWorkBlocking);
            ValueTask<int> countTask = fifo.AddWorkItemAsync(new MockWorkIn(1), ts.Token).CountAsync(ts.Token);

            await mw.TriggerOnBlockedWork(() => ts.Cancel());

            Assert.AreEqual(1, mw.doneWork.Count(f => f.Item1));
            Assert.AreEqual(1, mw.doneWork.Count());
        }


        [Test]
        public async Task AddWorkItemAsync_OneItemProcessed()
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            int inputIx = 1;
            int totThreads = 1;
            int maxQueuedItems = 1;
            var cfg = GetConfig(totThreads, maxQueuedItems);

            using (FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, mw.DoMockWork_Simple))
            {
                await foreach (var outItem in fifo.AddWorkItemAsync(new MockWorkIn(inputIx), ts.Token).Concat(fifo.FlushAsync(ts.Token)))
                    doneWork.Add(outItem);
            }

            Assert.AreEqual(1, doneWork.Count);
            Assert.AreEqual(inputIx, doneWork.First().originalInputItem.ix);
            Assert.AreEqual(1, mw.doneWork.Count);
            Assert.AreEqual(false, mw.doneWork.First().Item1);
            Assert.AreEqual(inputIx, mw.doneWork.First().Item2.ix);
            Assert.AreEqual(inputIx, mw.doneWork.First().Item3.originalInputItem.ix);
        }


        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(1, 2, 2)]
        [TestCase(2, 1, 1)]
        [TestCase(2, 2, 2)]
        [TestCase(10, 2, 2)]
        [TestCase(10, 2, 4)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public async Task AddWorkItemAsync_ItemsProcessed(int totItems, int totThreads, int maxQueuedItems)
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = GetConfig(totThreads, maxQueuedItems);

            await using (FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, mw.DoMockWork_Simple))
            {
                foreach (int inputIx in Enumerable.Range(1, totItems))
                    await foreach (var outItem in fifo.AddWorkItemAsync(new MockWorkIn(inputIx), ts.Token))
                        doneWork.Add(outItem);
                await foreach (var outItem in fifo.FlushAsync(ts.Token))
                    doneWork.Add(outItem);
            }

            Assert.AreEqual(Enumerable.Range(1, totItems), doneWork.Select(f => f.originalInputItem.ix));
            Assert.AreEqual(Enumerable.Range(1, totItems), mw.doneWork.Where(f => !f.Item1).OrderBy(f => f.Item2.ix).Select(f => f.Item2.ix));
        }
    }
}