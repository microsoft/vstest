// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName. This is a generic type.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Generic queue for processing jobs on a background thread.
    /// </summary>
    /// <typeparam name="T">The type of the job that is being processed.</typeparam>
    public class JobQueue<T> : IDisposable
    {
        #region Fields

        /// <summary>
        /// Handler which processes the individual jobs.
        /// </summary>
        private Action<T> processJob;

        /// <summary>
        /// Name used when displaying information or reporting errors about this queue.
        /// </summary>
        private string displayName;

        /// <summary>
        /// The queue of jobs.
        /// </summary>
        private Queue<Job<T>> jobsQueue;

        /// <summary>
        /// Signaled when a job is added to the queue.  Used to wakeup the background thread.
        /// </summary>
        private ManualResetEvent jobAdded;

        /// <summary>
        /// The maximum number of jobs the job queue may hold.
        /// </summary>
        private int maxNumberOfJobsInQueue;

        /// <summary>
        /// The maximum total size of jobs the job queue may hold.
        /// </summary>
        private int maxBytesQueueCanHold;

        /// <summary>
        /// Gives the approximate total size of objects in the queue.
        /// </summary>
        private int currentNumberOfBytesQueueIsHolding;

        /// <summary>
        /// Tells whether the queue should be bounded on size and no of events.
        /// </summary>
        private bool enableBoundsOnQueue;

        /// <summary>
        /// Used to pause and resume processing of the queue.  By default the manual reset event is
        /// set so the queue can continue processing.
        /// </summary>
        private ManualResetEvent queueProcessing;

        /// <summary>
        /// The background thread which is processing the jobs.  Used when disposing to wait
        /// for the thread to complete.
        /// </summary>
        private Task backgroundJobProcessor;

        /// <summary>
        /// Keeps track of if we are disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Logs to this action any exception when processing jobs.
        /// </summary>
        private Action<string> exceptionLogger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueue{T}"/> class.
        /// </summary>
        /// <param name="processJob">Action to handle the processing of the job.</param>
        /// <param name="displayName">Name to used when displaying information about this queue.</param>
        /// <param name="maxQueueLength">The max Queue Length.</param>
        /// <param name="maxQueueSize">The max Queue Size.</param>
        /// <param name="enableBounds">The enable Bounds.</param>
        /// <param name="exceptionLogger">The exception Logger.</param>
        public JobQueue(Action<T> processJob, string displayName, int maxQueueLength, int maxQueueSize, bool enableBounds, Action<string> exceptionLogger)
        {
            if (processJob == null)
            {
                throw new ArgumentNullException("processJob");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(Resources.CannotBeNullOrEmpty, "displayName");
            }

            if (maxQueueLength < 1)
            {
                throw new ArgumentOutOfRangeException("maxQueueLength");
            }

            if (maxQueueSize < 1)
            {
                throw new ArgumentOutOfRangeException("maxQueueSize");
            }

            this.maxNumberOfJobsInQueue = maxQueueLength;
            this.maxBytesQueueCanHold = maxQueueSize;
            this.enableBoundsOnQueue = enableBounds;

            // Initialize defaults.
            this.jobsQueue = new Queue<Job<T>>();
            this.jobAdded = new ManualResetEvent(false);
            this.queueProcessing = new ManualResetEvent(true);
            this.currentNumberOfBytesQueueIsHolding = 0;
            this.isDisposed = false;

            // Save off the arguments.
            this.processJob = processJob;
            this.displayName = displayName;
            this.exceptionLogger = exceptionLogger;

            // Setup the background thread to process the jobs.
            this.backgroundJobProcessor = Task.Run(() => this.BackgroundJobProcessor());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a job to the queue.
        /// </summary>
        /// <param name="job"> Job to add to the queue. </param>
        /// <param name="jobSize"> The job Size. </param>
        public void QueueJob(T job, int jobSize)
        {
            this.CheckDisposed();

            Debug.Assert(jobSize >= 0, "Job size should never be negative");

            // Add the job and signal that a new job is available.
            this.InternalQueueJob(new Job<T>(job, jobSize));
        }

        /// <summary>
        /// Pause the processing of queued jobs.
        /// </summary>
        public void Pause()
        {
            this.CheckDisposed();

            // Do not allow any jobs to be processed.
            this.queueProcessing.Reset();
        }

        /// <summary>
        /// Resume the processing of queued jobs.
        /// </summary>
        public void Resume()
        {
            this.CheckDisposed();

            // Resume processing of jobs.
            this.queueProcessing.Set();
        }

        /// <summary>
        /// Waits for all current jobs in the queue to be processed and then returns.
        /// </summary>
        public void Flush()
        {
            this.CheckDisposed();

            // Create the wait job.
            using (var waitEvent = new ManualResetEvent(false))
            {
                var waitJob = Job<T>.CreateWaitJob(waitEvent);

                // Queue the wait job and wait for it to be processed.
                this.InternalQueueJob(waitJob);

                waitEvent.WaitOne();
            }
        }

        /// <summary>
        /// Waits for all pending jobs to complete and shutdown the background thread.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            // If the queue is paused, then throw.
            if (!this.queueProcessing.WaitOne(0))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentUICulture, Resources.QueuePausedDisposeError, this.displayName));
            }

            this.isDisposed = true;

            // Disable bounds on the queue so that any waiting threads can proceed.
            lock (this.jobsQueue)
            {
                this.enableBoundsOnQueue = false;
                Monitor.PulseAll(this.jobsQueue);
            }

            // Flag the queue as being shutdown and wake up the background thread.
            this.InternalQueueJob(Job<T>.ShutdownJob);

            // Wait for the background thread to shutdown.
            this.backgroundJobProcessor.Wait();

            // Cleanup
            this.jobAdded.Dispose();
            this.queueProcessing.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Block the queue call.
        /// A separate protected virtual method had to be made so that it can be over-ridden when writing unit test to check
        /// if bounds on the queue are applied correctly.
        /// </summary>
        /// <returns>True if the queue is empty.</returns>
        protected virtual bool WaitForQueueToGetEmpty()
        {
            EqtTrace.Verbose("blocking on over filled queue.");
            return Monitor.Wait(this.jobsQueue);
        }

        /// <summary>
        /// Queue the job and signal the background thread.
        /// </summary>
        /// <param name="job">Job to be queued.</param>
        private void InternalQueueJob(Job<T> job)
        {
            // Add the job and signal that a new job is available.
            lock (this.jobsQueue)
            {
                // If the queue is getting over filled wait till the background processor releases the thread.
                while (this.enableBoundsOnQueue
                        &&
                      ((this.jobsQueue.Count >= this.maxNumberOfJobsInQueue)
                          ||
                       (this.currentNumberOfBytesQueueIsHolding >= this.maxBytesQueueCanHold)))
                {
                    this.WaitForQueueToGetEmpty();
                }

                this.jobsQueue.Enqueue(job);
                this.currentNumberOfBytesQueueIsHolding += job.Size;
                this.jobAdded.Set();
            }
        }

        /// <summary>
        /// Throws wen the queue has been disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(
                    string.Format(CultureInfo.CurrentUICulture, Resources.QueueAlreadyDisposed, this.displayName));
            }
        }

        /// <summary>
        /// Method which processes the jobs on the background thread.
        /// </summary>
        private void BackgroundJobProcessor()
        {
            bool shutdown = false;

            do
            {
                this.jobAdded.WaitOne();

                // Pull all of the current jobs out of the queue.
                List<Job<T>> jobs = new List<Job<T>>();
                lock (this.jobsQueue)
                {
                    while (this.jobsQueue.Count != 0)
                    {
                        var job = this.jobsQueue.Dequeue();
                        this.currentNumberOfBytesQueueIsHolding -= job.Size;

                        // If this is a shutdown job, signal shutdown and stop adding jobs.
                        if (job.Shutdown)
                        {
                            shutdown = true;
                            break;
                        }

                        jobs.Add(job);
                    }

                    // Reset the manual reset event so we get notified of new jobs that are added.
                    this.jobAdded.Reset();

                    // Releases a thread waiting on the queue to get empty, to continue with the enquing process.
                    if (this.enableBoundsOnQueue)
                    {
                        Monitor.PulseAll(this.jobsQueue);
                    }
                }

                // Process the jobs
                foreach (var job in jobs)
                {
                    // Wait for the queue to be open (not paused) and process the job.
                    this.queueProcessing.WaitOne();

                    // If this is a wait job, signal the manual reset event and continue.
                    if (job.WaitManualResetEvent != null)
                    {
                        job.WaitManualResetEvent.Set();
                    }
                    else
                    {
                        this.SafeProcessJob(job.Payload);
                    }
                }
            }
            while (!shutdown);
        }

        /// <summary>
        /// Executes the process job handler and logs any exceptions which occur.
        /// </summary>
        /// <param name="job">Job to be executed.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "unknown action could throw all kinds of exceptions.")]
        private void SafeProcessJob(T job)
        {
            try
            {
                this.processJob(job);
            }
            catch (Exception e)
            {
                this.exceptionLogger(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        Resources.ExceptionFromJobProcessor,
                        this.displayName,
                        e));
            }
        }

        #endregion
    }
}
