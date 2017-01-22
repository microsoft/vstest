// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Contains all information required for attachment transfer once scheduled from data collector.
    /// </summary>
    internal class AttachmentRequest : IDisposable
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
        /// Initializes a new instance of the <see cref="AttachmentRequest"/> class. 
        /// </summary>
        /// <param name="baseDirectory">
        /// Base directory for creating collection log destination file path.
        /// </param>
        /// <param name="fileTransferInfo">
        /// Object containing all information for file transfer.
        /// </param>
        internal AttachmentRequest(string baseDirectory, FileTransferInformationExtension fileTransferInfo)
        {
            this.requestCompleted = new ManualResetEvent(false);
            this.FileTransferInfo = fileTransferInfo;
            this.LocalFileName = Path.GetFileName(fileTransferInfo.FileName);

            // Testcase specific collection should be created in testcase directorty.
            var directoryPath = Path.Combine(
                baseDirectory,
                fileTransferInfo.Context.HasTestCase ? this.FileTransferInfo.Context.TestExecId.Id.ToString() : string.Empty);

            this.LocalFilePath = Path.Combine(directoryPath, this.LocalFileName);
        }

        #endregion

        /// <summary>
        /// Gets FileName with extension
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
        /// Gets file transfer related info sent by data collector for file transfer
        /// </summary>
        internal FileTransferInformationExtension FileTransferInfo
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
                    this.requestCompleted.Dispose();
                }

                this.disposed = true;
            }
        }

        #endregion
    }
}
