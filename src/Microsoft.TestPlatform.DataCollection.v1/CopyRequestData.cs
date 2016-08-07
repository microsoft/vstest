// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Contains all information required for file transfer once scheduled from data collector.
    /// </summary>
    internal class CopyRequestData : IDisposable
    {
        #region Properties & Fields
        /// <summary>
        /// Specifies whether the object is disposed or not. 
        /// </summary>
        private bool disposed;


        /// <summary>
        /// Set when this request is processed/completed.
        /// </summary>
        private ManualResetEvent requestCompleted;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyRequestData"/> class. 
        /// </summary>
        /// <param name="baseDirectory">
        /// Base directory for creating collection log destination file path.
        /// </param>
        /// <param name="headerMessage">
        /// Header message containing all information for file transfer.
        /// </param>
        internal CopyRequestData(string baseDirectory, FileDataHeaderMessage headerMessage)
        {
            this.requestCompleted = new ManualResetEvent(false);
            this.FileDataHeaderMessage = headerMessage;
            this.LocalFileName = Path.GetFileName(headerMessage.FileName);

            // Testcase specific collection should be created in testcase directorty.
            var directoryPath = Path.Combine(
                baseDirectory,
                headerMessage.DataCollectionContext.HasTestCase ? this.FileDataHeaderMessage.DataCollectionContext.TestExecId.Id.ToString() : string.Empty);

            this.LocalFilePath = Path.Combine(directoryPath, this.LocalFileName);
        }

        #endregion

        /// <summary>
        /// Gets fileName with extension
        /// </summary>
        internal string LocalFileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets path to LocalFileName (including file name)
        /// </summary>
        internal string LocalFilePath
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets DataCollectionMessage sent by data collector for file transfer
        /// </summary>
        internal FileDataHeaderMessage FileDataHeaderMessage
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets error in processing the request.
        /// </summary>
        internal Exception Error
        {
            get;
            private set;
        }

        /// <summary>
        /// Dispose event object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        #region internal methods
        /// <summary>
        /// Wait for file transfer to complete.
        /// </summary>
        internal void WaitForCopyComplete()
        {
            this.requestCompleted.WaitOne();
        }

        /// <summary>
        /// Mark this request as completed by setting the event.
        /// Store any error information for further processing.
        /// </summary>
        /// <param name="error">
        /// The error.
        /// </param>
        internal void CompleteRequest(Exception error)
        {
            this.Error = error;
            this.requestCompleted.Set();
        }
        #endregion

        #region IDisposable

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.requestCompleted.Close();
                }

                this.disposed = true;
            }
        }
        #endregion
    }
}
