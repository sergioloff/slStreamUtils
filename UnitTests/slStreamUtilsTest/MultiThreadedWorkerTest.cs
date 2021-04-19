/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using NUnit.Framework;
using slStreamUtils;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [TestFixture]
    public class MultiThreadedWorkerTest
    {
        [SetUp]
        public void Setup()
        {
        }

        private MultiThreadedWorkerConfig GetConfig(int totThreads, int maxQueuedItems)
        {
            return new MultiThreadedWorkerConfig(totThreads, maxQueuedItems);
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


        public class MockWorker
        {
            public ConcurrentQueue<Tuple<bool, MockWorkIn>> doneWork = new ConcurrentQueue<Tuple<bool, MockWorkIn>>();
            public TaskCompletionSource<object> taskBlocker1 = new TaskCompletionSource<object>();
            public TaskCompletionSource<object> taskBlocker2 = new TaskCompletionSource<object>();
            public TaskCompletionSource<object> taskBlocker3 = new TaskCompletionSource<object>();
            public int workDurationMs;
            public MockWorker(int workDurationMs = 10)
            {
                this.workDurationMs = workDurationMs;
            }

            public async Task DoMockWork_SimpleAsync(MockWorkIn work, CancellationToken token)
            {
                try
                {
                    await Task.Delay(workDurationMs, token);
                    token.ThrowIfCancellationRequested();
                }
                catch
                {
                    doneWork.Enqueue(new Tuple<bool, MockWorkIn>(true, work));
                    throw;
                }
                doneWork.Enqueue(new Tuple<bool, MockWorkIn>(false, work));
            }

            public async Task DoMockWorkBlockingAsync(MockWorkIn work, CancellationToken token)
            {
                taskBlocker1.SetResult(null);
                try
                {
                    await taskBlocker3.Task;
                    try
                    {
                        await Task.Delay(workDurationMs, token);
                        token.ThrowIfCancellationRequested();
                    }
                    catch
                    {
                        doneWork.Enqueue(new Tuple<bool, MockWorkIn>(true, work));
                        throw;
                    }
                    doneWork.Enqueue(new Tuple<bool, MockWorkIn>(false, work));
                }
                finally
                {
                    taskBlocker2.SetResult(null);
                }
            }

            public async Task TriggerOnBlockedWorkAsync(Action callback)
            {
                await taskBlocker1.Task;
                callback();
                taskBlocker3.SetResult(null);
                await taskBlocker2.Task;
            }

            public void DoMockWork_Simple(MockWorkIn work, CancellationToken token)
            {
                try
                {
                    Task.Delay(workDurationMs, token).Wait();
                    token.ThrowIfCancellationRequested();
                }
                catch
                {
                    doneWork.Enqueue(new Tuple<bool, MockWorkIn>(true, work));
                    throw;
                }
                doneWork.Enqueue(new Tuple<bool, MockWorkIn>(false, work));
            }

            public void DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                taskBlocker1.SetResult(null);
                try
                {
                    taskBlocker3.Task.Wait();
                    try
                    {
                        Task.Delay(workDurationMs, token).Wait();
                        token.ThrowIfCancellationRequested();
                    }
                    catch
                    {
                        doneWork.Enqueue(new Tuple<bool, MockWorkIn>(true, work));
                        throw;
                    }
                    doneWork.Enqueue(new Tuple<bool, MockWorkIn>(false, work));
                }
                finally
                {
                    taskBlocker2.SetResult(null);
                }
            }

            public void TriggerOnBlockedWork(Action callback)
            {
                taskBlocker1.Task.Wait();
                callback();
                taskBlocker3.SetResult(null);
                taskBlocker2.Task.Wait();
            }
        }

        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        public void Dispose_CancelsPendingTasks(int totThreads, bool cancelsDuringWork)
        {
            TaskCompletionSource<object> taskBlocker1 = new TaskCompletionSource<object>();
            bool completed = false;
            void DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                taskBlocker1.Task.Wait();
                Task.Delay(1, token).Wait();
                completed = true;
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            var cfg = new MultiThreadedWorkerConfig(totThreads);
            MultiThreadedWorker<MockWorkIn> mtw = new MultiThreadedWorker<MockWorkIn>(cfg, DoMockWorkBlocking);
            mtw.AddWorkItem(new MockWorkIn(1), ts.Token);
            if (cancelsDuringWork)
                ts.Cancel();
            taskBlocker1.SetResult(null);

            Assert.DoesNotThrow(mtw.Dispose);

            Assert.AreEqual(!cancelsDuringWork, completed);
        }


        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Dispose_ExceptionInWorkerPropagates(int totThreads)
        {
            TaskCompletionSource<object> taskBlocker1 = new TaskCompletionSource<object>();
            void DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                taskBlocker1.Task.Wait();
                throw new MockException();
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            var cfg = new MultiThreadedWorkerConfig(totThreads);
            MultiThreadedWorker<MockWorkIn> mtw = new MultiThreadedWorker<MockWorkIn>(cfg, DoMockWorkBlocking);
            mtw.AddWorkItem(new MockWorkIn(1), ts.Token);

            TestDelegate disposeDel = delegate ()
            {
                try
                {
                    taskBlocker1.SetResult(null);
                    mtw.Dispose();
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
        public void AddWorkItem_Cancels(int totThreads)
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            var cfg = new MultiThreadedWorkerConfig(totThreads);
            MultiThreadedWorker<MockWorkIn> mtw = new MultiThreadedWorker<MockWorkIn>(cfg, mw.DoMockWorkBlocking);
            mtw.AddWorkItem(new MockWorkIn(1), ts.Token);

            mw.TriggerOnBlockedWorkAsync(() => ts.Cancel()).Wait();

            Assert.AreEqual(1, mw.doneWork.Count(f => f.Item1));
            Assert.AreEqual(1, mw.doneWork.Count());
        }


        [Test]
        public void AddWorkItemAsync_OneItemProcessed()
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            int inputIx = 1;
            int totThreads = 1;
            int maxQueuedItems = 1;
            var cfg = GetConfig(totThreads, maxQueuedItems);

            using (MultiThreadedWorker<MockWorkIn> mtw = new MultiThreadedWorker<MockWorkIn>(cfg, mw.DoMockWork_Simple))
            {
                mtw.AddWorkItem(new MockWorkIn(inputIx), ts.Token);
            }

            Assert.AreEqual(1, mw.doneWork.Count);
            Assert.AreEqual(false, mw.doneWork.First().Item1);
            Assert.AreEqual(inputIx, mw.doneWork.First().Item2.ix);
        }


        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(1, 2, 2)]
        [TestCase(2, 1, 1)]
        [TestCase(2, 2, 2)]
        [TestCase(10, 2, 2)]
        [TestCase(10, 2, 4)]
        [TestCase(100, 2, 4)]
        [TestCase(1000, 20, 40)]
        public void AddWorkItem_ItemsProcessed(int totItems, int totThreads, int maxQueuedItems)
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            var cfg = GetConfig(totThreads, maxQueuedItems);

            using (MultiThreadedWorker<MockWorkIn> mtw = new MultiThreadedWorker<MockWorkIn>(cfg, mw.DoMockWork_Simple))
            {
                foreach (int inputIx in Enumerable.Range(1, totItems))
                    mtw.AddWorkItem(new MockWorkIn(inputIx), ts.Token);
                mtw.Flush(ts.Token);
            }

            Assert.AreEqual(Enumerable.Range(1, totItems), mw.doneWork.Where(f => !f.Item1).OrderBy(f => f.Item2.ix).Select(f => f.Item2.ix));
        }
    }
}