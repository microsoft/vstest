// Copyright(c) Microsoft.All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Wrapper class around a job used to send additional information to the background thread.
    /// </summary>
    /// <typeparam name="TPayload">The type of the job.</typeparam>
    internal class Job<TPayload>
    {
        #region Constructor

        /// <summary>
        /// Initializes with the job to be processed.
        /// </summary>
        /// <param name="job"> Job to be processed. </param>
        /// <param name="size"> The size. </param>
        public Job(TPayload job, int size)
        {
            this.Payload = job;
            this.Size = size;
        }

        /// <summary>
        /// Constructor used for creating special jobs.
        /// </summary>
        private Job()
        {
            this.Size = 0;
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets a special job that indicates the queue should shutdown.
        /// </summary>
        public static Job<TPayload> ShutdownJob
        {
            get
            {
                var shutdownJob = new Job<TPayload>();
                shutdownJob.Shutdown = true;

                return shutdownJob;
            }
        }

        /// <summary>
        /// Gets the job to be processed.
        /// </summary>
        public TPayload Payload { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the background thread should shutdown.
        /// </summary>
        public bool Shutdown { get; private set; }

        /// <summary>
        /// Gets the signal that this job is being processed.
        /// </summary>
        public ManualResetEvent WaitManualResetEvent { get; private set; }

        /// <summary>
        /// Gets the size of this job instance. This is used to manage the total size of Job Queue.
        /// </summary>
        public int Size { get; private set; }

        #endregion

        #region Static Methods

        /// <summary>
        /// Creates a job with a manual reset event that will be set when the job is processed.
        /// </summary>
        /// <param name="waitEvent"> The wait Event. </param>
        /// <returns> The wait job. </returns>
        public static Job<TPayload> CreateWaitJob(ManualResetEvent waitEvent)
        {
            ValidateArg.NotNull<ManualResetEvent>(waitEvent, "waitEvent");
            var waitJob = new Job<TPayload>();
            waitJob.WaitManualResetEvent = waitEvent;

            return waitJob;
        }

        #endregion
    }
}
