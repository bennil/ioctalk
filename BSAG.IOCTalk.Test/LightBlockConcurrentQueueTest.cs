using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using BSAG.IOCTalk.Communication.Common.Collections;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BSAG.IOCTalk.Test
{
    [TestClass]
    public class LightBlockConcurrentQueueTest
    {
        [TestMethod]
        public void EnqueueDequeueTest()
        {
            LightBlockConcurrentQueue<int> queue = new LightBlockConcurrentQueue<int>();

            var t1 = new Thread(() =>
            {
                for (int i = 0; i < 1000000; i++)
                {
                    if ((i % 10000) == 0)
                    {
                        Thread.Sleep(5);
                    }

                    queue.Enqueue(i);
                }
            });


            var t2 = new Thread(() =>
            {
                int number;
                for (int i = 0; i < 1000000; i++)
                {
                    if (queue.TryDequeueOrWaitForItem(out number))
                    {
                        Assert.AreEqual<int>(i, number);
                    }
                    else
                    {
                        // queue canceled
                        break;
                    }
                }

                Thread.Sleep(100);
                Assert.AreEqual<int>(0, queue.Count);
            });

            t1.Start();
            t2.Start();

            Assert.IsTrue(t2.Join(10000));
        }



        [TestMethod]
        public void EnqueueDequeueMultipleReadersTest()
        {
            try
            {
                LightBlockConcurrentQueue<int> queue = new LightBlockConcurrentQueue<int>();

                long enqueueCount = 3000000;
                long currentDequeueCount = 0;

                var tWriter = new Thread(() =>
                {
                    for (int i = 0; i < enqueueCount; i++)
                    {
                        if ((i % 10000) == 0)
                        {
                            Thread.Sleep(5);
                        }

                        queue.Enqueue(i);
                    }
                });

                ThreadStart readQueue = new ThreadStart(() =>
                {
                    int dequeueCount = 0;
                    int number;
                    while (Interlocked.Read(ref currentDequeueCount) < enqueueCount)
                    {
                        if (queue.TryDequeueOrWaitForItem(out number))
                        {
                            Interlocked.Increment(ref currentDequeueCount);
                            dequeueCount++;
                        }
                        else
                        {
                            // queue canceled
                            break;
                        }
                    }

                    // stop other wait threads
                    queue.Cancel();

                    Debug.WriteLine("Dequeue count: " + dequeueCount);
                });

                var tReader1 = new Thread(readQueue);
                var tReader2 = new Thread(readQueue);
                var tReader3 = new Thread(readQueue);

                tWriter.Start();
                tReader1.Start();
                tReader2.Start();
                tReader3.Start();

                Assert.IsTrue(tReader1.Join(20000));
                Assert.IsTrue(tReader2.Join(20000));
                Assert.IsTrue(tReader3.Join(20000));

                Assert.AreEqual<int>(0, queue.Count);
                Assert.AreEqual<long>(enqueueCount, currentDequeueCount);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        [TestMethod]
        public void EnqueueDequeueMultipleReadersAndWritersTest()
        {
            try
            {
                LightBlockConcurrentQueue<int> queue = new LightBlockConcurrentQueue<int>();

                long enqueueCount = 3000000;
                long expectedDequeueCount = enqueueCount * 3;
                long currentDequeueCount = 0;

                var writeQueue = new ThreadStart(() =>
                {
                    for (int i = 0; i < enqueueCount; i++)
                    {
                        if ((i % 10000) == 0)
                        {
                            Thread.Sleep(5);
                        }

                        queue.Enqueue(i);
                    }
                });

                ThreadStart readQueue = new ThreadStart(() =>
                {
                    int dequeueCount = 0;
                    foreach (var number in queue.GetConsumingEnumerable())
                    {
                        Interlocked.Increment(ref currentDequeueCount);
                        dequeueCount++;
                    }

                    Debug.WriteLine("Dequeue count: " + dequeueCount);
                });

                var tWriter1 = new Thread(writeQueue);
                var tWriter2 = new Thread(writeQueue);
                var tWriter3 = new Thread(writeQueue);

                var tReader1 = new Thread(readQueue);
                var tReader2 = new Thread(readQueue);
                var tReader3 = new Thread(readQueue);

                tWriter1.Start();
                tReader1.Start();
                tReader2.Start();
                tWriter2.Start();

                tReader3.Start();

                tWriter3.Start();

                ThreadStart stopConsuming = new ThreadStart(() =>
                {
                    while (currentDequeueCount < expectedDequeueCount)
                    {
                        Thread.Sleep(100);
                    }

                    // stop other wait threads
                    queue.Cancel();

                    Debug.WriteLine("Canceled");
                });
                new Thread(stopConsuming).Start();

                Assert.IsTrue(tReader1.Join(20000));
                Assert.IsTrue(tReader2.Join(20000));
                Assert.IsTrue(tReader3.Join(20000));

                Assert.AreEqual<int>(0, queue.Count);
                Assert.AreEqual<long>(expectedDequeueCount, currentDequeueCount);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
