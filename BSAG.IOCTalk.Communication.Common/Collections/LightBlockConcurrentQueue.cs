using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BSAG.IOCTalk.Communication.Common.Collections
{
    /// <summary>
    /// The <see cref="LightBlockConcurrentQueue{T}"/> class extends the <see cref="ConcurrentQueue{T}"/> implementation with lightweight block capabilities.
    /// This implementation uses about 4 times less cpu resources than <see cref="BlockingCollection{T}"/>.Take method (depending on the scenario).
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2015-12-16
    /// </remarks>
    /// <typeparam name="T">The queue object type</typeparam>
    public class LightBlockConcurrentQueue<T> : ConcurrentQueue<T>
    {
        #region fields

        private ManualResetEvent waitHandle = new ManualResetEvent(false);
        private volatile int waitHandleProcessCount = 0;
        private volatile bool isCanceled = false;

        #endregion

        #region constructors

        /// <summary>
        /// Creates a new instance of the <see cref="LightBlockConcurrentQueue{T}"/> class.
        /// </summary>
        public LightBlockConcurrentQueue()
        {
        }

        /// <summary>
        /// Cleanup wait handle resources
        /// </summary>
        ~LightBlockConcurrentQueue()
        {
            waitHandle.Dispose();
        }

        #endregion

        #region properties

        #endregion

        #region methods

        /// <summary>
        /// Enqueues the given object to the end of the queue.
        /// </summary>
        /// <exception cref="OperationCanceledException">Occurs when the queue is already cancelled.</exception>
        /// <param name="item">Add object</param>
        public new void Enqueue(T item)
        {
            if (isCanceled)
            {
                throw new OperationCanceledException("Queue is cancelled. No more enqueues allowed!");
            }

            base.Enqueue(item);

            if (waitHandleProcessCount > 0)
            {
                waitHandle.Set();
                Interlocked.Decrement(ref waitHandleProcessCount);
            }
        }


        /// <summary>
        /// Attempts to remove and return the object at the beginning of the queue. If the queue is empty the method will block until an item is available.
        /// The method uses a combination of lightweight spin blocking (pull) and if no item is received within a certain period the method will block using a resource friendly wait handle mechanism (push).
        /// </summary>
        /// <returns>Returns <c>false</c> if the queue is cancelled; otherwise <c>true</c>.</returns>
        public bool TryDequeueOrWaitForItem(out T item)
        {
            if (isCanceled)
            {
                item = default(T);
                return false;
            }

            if (base.TryDequeue(out item))
            {
                return true;
            }
            else
            {
                SpinWait spinWait = new SpinWait();
                spinWait.SpinOnce();

                while (true)
                {
                    if (base.TryDequeue(out item))
                    {
                        return true;
                    }
                    else
                    {
                        if (spinWait.NextSpinWillYield)
                        {
                            Thread.Sleep(0);

                            // check if an item was enqueued during yield
                            if (base.TryDequeue(out item))
                            {
                                return true;
                            }

                            // still no dequeue item -> setup wait handle
                            // increment wait handle count
                            Interlocked.Increment(ref waitHandleProcessCount);  // use Interlocked to be thread safe in multi processor enviroments

                            // check again for thread safety reasons
                            if (base.TryDequeue(out item))
                            {
                                return true;
                            }

                            // Use wait handle to block thread without using polling resources
                            waitHandle.WaitOne();

                            if (isCanceled)
                            {
                                return false;
                            }

                            if (base.TryDequeue(out item))
                            {
                                return true;
                            }
                            else
                            {
                                // signal without dequeue item
                                // reset wait handle and try read again
                                waitHandle.Reset();

                                // pause because immediate rechecking is not necessary
                                Thread.Sleep(0);
                            }
                        }
                        else
                        {
                            spinWait.SpinOnce();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Provides a consuming blocking iteration for new queue items.
        /// The iteration will be released only when the queue is canceled.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generics.IEnumerable{T}"/> that removes and returns items from the queue.</returns>
        public IEnumerable<T> GetConsumingEnumerable()
        {
            while (true)
            {
                T item;
                if (TryDequeueOrWaitForItem(out item))
                {
                    yield return item;
                }
                else
                {
                    // queue canceled
                    break;
                }
            }
        }

        /// <summary>
        /// Aborts waiting queue processes and stops enqueue.
        /// </summary>
        /// <exception cref="TimeoutException">Cancel timeout occured</exception>
        public void Cancel()
        {
            if (!isCanceled)
            {
                this.isCanceled = true;

                waitHandle.Set();
            }
        }

        #endregion

    }
}
