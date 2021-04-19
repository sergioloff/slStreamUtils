/* Copyright (c) 2021, Sergio Loff
All rights reserved.
This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. */
using NUnit.Framework;
using slStreamUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slStreamUtilsTest
{
    [TestFixture]
    public class FIFOWorkerTest
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
            public TaskCompletionSource<object> taskBlocker1 = new TaskCompletionSource<object>();
            public TaskCompletionSource<object> taskBlocker2 = new TaskCompletionSource<object>();
            public TaskCompletionSource<object> taskBlocker3 = new TaskCompletionSource<object>();
            public int workDurationMs;
            public MockWorker(int workDurationMs = 10)
            {
                this.workDurationMs = workDurationMs;
            }


            public MockWorkOut DoMockWork_Simple(MockWorkIn work, CancellationToken token)
            {
                try
                {
                    Task.Delay(workDurationMs, token).Wait();
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

            public MockWorkOut DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                taskBlocker1.SetResult(null);
                try
                {
                    taskBlocker3.Task.Wait();
                    try
                    {
                        Task.Delay(workDurationMs, token).Wait();
                    }
                    catch (AggregateException eag)
                    {
                        doneWork.Enqueue(new Tuple<bool, MockWorkIn, MockWorkOut>(true, work, null));
                        throw eag.InnerException;
                    }
                    catch (Exception)
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

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Dispose_CancelsPendingTasks(int totThreads)
        {
            TaskCompletionSource<object> taskBlocker1 = new TaskCompletionSource<object>();
            TaskCompletionSource<object> taskBlocker2 = new TaskCompletionSource<object>();
            bool completed = false;

            int totBlockCalled = 0;

            MockWorkOut DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                Interlocked.Increment(ref totBlockCalled);
                taskBlocker2.SetResult(null);
                taskBlocker1.Task.Wait();
                Task.Delay(1, token).Wait();
                completed = true;
                return new MockWorkOut(work);
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, DoMockWorkBlocking);
            int count = fifo.AddWorkItem(new MockWorkIn(1), ts.Token).Count();
            taskBlocker2.Task.Wait();
            taskBlocker1.SetResult(null);


            Assert.AreEqual(1, totBlockCalled);
            Assert.AreEqual(0, count);
            Assert.DoesNotThrow(fifo.Dispose);
            Assert.AreEqual(true, completed);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Dispose_ThrowsOnCancelledTasks(int totThreads)
        {
            TaskCompletionSource<object> taskBlocker1 = new TaskCompletionSource<object>();
            TaskCompletionSource<object> taskBlocker2 = new TaskCompletionSource<object>();
            bool completed = false;

            int totBlockCalled = 0;

            MockWorkOut DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                Interlocked.Increment(ref totBlockCalled);
                taskBlocker2.SetResult(null);
                taskBlocker1.Task.Wait();
                Task.Delay(1, token).Wait();
                completed = true;
                return new MockWorkOut(work);
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, DoMockWorkBlocking);
            int count = fifo.AddWorkItem(new MockWorkIn(1), ts.Token).Count();
            taskBlocker2.Task.Wait();
            ts.Cancel();

            Assert.Throws<TaskCanceledException>(() =>
            {
                try
                {
                    taskBlocker1.SetResult(null);
                    fifo.Dispose();
                }
                catch (AggregateException ag)
                {
                    throw ag.GetBaseException();
                }
            });
            Assert.AreEqual(1, totBlockCalled);
            Assert.AreEqual(0, count);
            Assert.AreEqual(false, completed);
        }


        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Dispose_ExceptionInWorkerPropagates(int totThreads)
        {
            TaskCompletionSource<object> taskBlocker1 = new TaskCompletionSource<object>();
            MockWorkOut DoMockWorkBlocking(MockWorkIn work, CancellationToken token)
            {
                taskBlocker1.Task.Wait();
                throw new MockException();
            }
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, DoMockWorkBlocking);
            int count = fifo.AddWorkItem(new MockWorkIn(1), ts.Token).Count();

            Assert.Throws<MockException>(() =>
            {
                try
                {
                    taskBlocker1.SetResult(null);
                    fifo.Dispose();
                }
                catch (AggregateException ag)
                {
                    throw ag.GetBaseException();
                }
            });
        }



        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void AddWorkItem_Cancels(int totThreads)
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = new FIFOWorkerConfig(totThreads);
            FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, mw.DoMockWorkBlocking);
            int countTask = fifo.AddWorkItem(new MockWorkIn(1), ts.Token).Count();

            mw.TriggerOnBlockedWork(() => ts.Cancel());

            Assert.Throws<TaskCanceledException>(() =>
            {
                try
                {
                    fifo.Dispose();
                }
                catch (AggregateException ag)
                {
                    throw ag.GetBaseException();
                }
            });
            Assert.AreEqual(1, mw.doneWork.Count(f => f.Item1));
            Assert.AreEqual(1, mw.doneWork.Count());
        }


        [Test]
        public void AddWorkItem_OneItemProcessed()
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
                foreach (var outItem in fifo.AddWorkItem(new MockWorkIn(inputIx), ts.Token).Concat(fifo.Flush(ts.Token)))
                    doneWork.Add(outItem);
            }

            Assert.AreEqual(1, doneWork.Count);
            Assert.AreEqual(inputIx, doneWork.First().originalInputItem.ix);
            Assert.AreEqual(1, mw.doneWork.Count);
            Assert.AreEqual(false, mw.doneWork.First().Item1);
            Assert.AreEqual(inputIx, mw.doneWork.First().Item2.ix);
            Assert.AreEqual(inputIx, mw.doneWork.First().Item3.originalInputItem.ix);
        }


        [TestCase(1, 1, 1)]
        [TestCase(1, 2, 2)]
        [TestCase(2, 1, 1)]
        [TestCase(2, 2, 2)]
        [TestCase(10, 2, 2)]
        [TestCase(10, 2, 4)]
        [TestCase(100, 2, 4)]
        [TestCase(10000, 20, 40)]
        public void AddWorkItem_ItemsProcessed(int totItems, int totThreads, int maxQueuedItems)
        {
            MockWorker mw = new MockWorker();
            CancellationTokenSource ts = new CancellationTokenSource();
            List<MockWorkOut> doneWork = new List<MockWorkOut>();
            var cfg = GetConfig(totThreads, maxQueuedItems);

            using (FIFOWorker<MockWorkIn, MockWorkOut> fifo = new FIFOWorker<MockWorkIn, MockWorkOut>(cfg, mw.DoMockWork_Simple))
            {
                foreach (int inputIx in Enumerable.Range(1, totItems))
                    foreach (var outItem in fifo.AddWorkItem(new MockWorkIn(inputIx), ts.Token))
                        doneWork.Add(outItem);
                foreach (var outItem in fifo.Flush(ts.Token))
                    doneWork.Add(outItem);
            }

            Assert.AreEqual(Enumerable.Range(1, totItems), doneWork.Select(f => f.originalInputItem.ix));
            Assert.AreEqual(Enumerable.Range(1, totItems), mw.doneWork.Where(f => !f.Item1).OrderBy(f => f.Item2.ix).Select(f => f.Item2.ix));
        }
    }
}