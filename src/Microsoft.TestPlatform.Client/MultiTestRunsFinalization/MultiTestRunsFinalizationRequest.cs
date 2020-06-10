using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using System;
using System.Threading;

namespace Microsoft.VisualStudio.TestPlatform.Client.MultiTestRunsFinalization
{
    public class MultiTestRunsFinalizationRequest : IMultiTestRunsFinalizationRequest
    {
        public event EventHandler<string> OnRawMessageReceived;

        public void Abort()
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("MultiTestRunsFinalizationRequest.Abort: Aborting.");
            }

            lock (this.syncObject)
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("MultiTestRunsFinalizationRequest");
                }

                if (this.finalizationInProgress)
                {
                    this.finalizationManager.Abort();
                }
                else
                {
                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("MultiTestRunsFinalizationRequest.Abort: No operation to abort.");
                    }

                    return;
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("MultiTestRunsFinalizationRequest.Abort: Aborted.");
            }
        }

        public void FinalizeMultiTestRunsAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Wait for discovery completion
        /// </summary>
        /// <param name="timeout"> The timeout. </param>
        bool IRequest.WaitForCompletion(int timeout)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("MultiTestRunsFinalizationRequest.WaitForCompletion: Waiting with timeout {0}.", timeout);
            }

            if (this.disposed)
            {
                throw new ObjectDisposedException("MultiTestRunsFinalizationRequest");
            }

            // This method is not synchronized as it can lead to dead-lock
            // (the discoveryCompletionEvent cannot be raised unless that lock is released)
            if (this.finalizationCompleted != null)
            {
                return this.finalizationCompleted.WaitOne(timeout);
            }

            return true;
        }

        #region IDisposable implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("MultiTestRunsFinalizationRequest.Dispose: Starting.");
            }

            lock (this.syncObject)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        if (this.finalizationCompleted != null)
                        {
                            this.finalizationCompleted.Dispose();
                        }
                    }

                    // Indicate that object has been disposed
                    this.finalizationCompleted = null;
                    this.disposed = true;
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DiscoveryRequest.Dispose: Completed.");
            }
        }

        #endregion

        #region privates fields

        /// <summary>
        /// If this request has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// It get set when current discovery request is completed.
        /// </summary>
        private ManualResetEvent finalizationCompleted = new ManualResetEvent(false);

        /// <summary>
        /// Sync object for various operations
        /// </summary>
        private object syncObject = new Object();

        /// <summary>
        /// Whether or not the test discovery is in progress.
        /// </summary>
        private bool finalizationInProgress;

        /// <summary>
        /// Discovery Start Time
        /// </summary>
        private DateTime finalizationStartTime;

        /// <summary>
        /// Finalization manager
        /// </summary>
        private IMultiTestRunsFinalizationManager finalizationManager;

        #endregion
    }
}
