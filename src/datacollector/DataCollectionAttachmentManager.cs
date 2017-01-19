// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Manages file transfer from data collector to test runner service.
    /// </summary>
    internal class DataCollectionAttachmentManager : IDataCollectionAttachmentManager
    {
        #region Fields

        /// <summary>
        /// Max number of file transfer jobs.
        /// </summary>
        private const int MaxQueueLength = 500;

        /// <summary>
        /// Max queue size
        /// </summary>
        private const int MaxQueueSize = 25000000;

        /// <summary>
        /// Default results directory to be used when user didn't specify.
        /// </summary>
        private const string DefaultOutputDirectoryName = "TestResults";

        /// <summary>
        /// Specifies whether the object is disposed or not. 
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Job queue for moving file from source to destination.
        /// </summary>
        private JobQueue<AttachmentRequest> fileCopierJobQueue;

        /// <summary>
        /// Logger for data collection messages
        /// </summary>
        private IMessageSink messageSink;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionAttachmentManager"/> class.
        /// </summary>
        public DataCollectionAttachmentManager()
        {
            this.AttachmentRequests = new List<AttachmentRequest>();

            this.fileCopierJobQueue = new JobQueue<AttachmentRequest>(
                this.OnAttachmentRequest,
                "DataCollectionAttachmentManagerQueue",
                MaxQueueLength,
                MaxQueueSize,
                true,
                message => EqtTrace.Error(message));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the session output directory.
        /// </summary>
        internal string SessionOutputDirectory { get; private set; }

        /// <summary>
        /// Gets the attachment requests.
        /// </summary>
        internal List<AttachmentRequest> AttachmentRequests { get; private set; }

        #endregion

        #region public methods

        /// <inheritdoc/>
        public void Initialize(SessionId id, string outputDirectory, IMessageSink messageSink)
        {
            ValidateArg.NotNull(id, nameof(id));

            ValidateArg.NotNull(messageSink, nameof(messageSink));

            this.messageSink = messageSink;

            if (string.IsNullOrEmpty(outputDirectory))
            {
                this.SessionOutputDirectory = Path.Combine(Path.GetTempPath(), DefaultOutputDirectoryName);
                this.SessionOutputDirectory = Path.Combine(this.SessionOutputDirectory, id.Id.ToString());
            }
            else
            {
                // Create a session specific directory under base output directory.
                var expandedOutputDirectory = Environment.ExpandEnvironmentVariables(outputDirectory);
                var absolutePath = Path.GetFullPath(expandedOutputDirectory);
                this.SessionOutputDirectory = Path.Combine(absolutePath, id.Id.ToString());
            }

            // Create the output directory if it doesn't exist.
            if (!Directory.Exists(this.SessionOutputDirectory))
            {
                Directory.CreateDirectory(this.SessionOutputDirectory);
            }
        }

        /// <inheritdoc/>
        public List<AttachmentSet> GetAttachments(DataCollectionContext dataCollectionContext)
        {
            var entries = new List<AttachmentSet>();

            foreach (var attachmentRequest in this.AttachmentRequests)
            {
                attachmentRequest.WaitForCopyComplete();
                if (attachmentRequest.Error != null)
                {
                    // If there was error in processing the request, lets log it.
                    var testCaseId = dataCollectionContext.HasTestCase
                                         ? dataCollectionContext.TestExecId.Id
                                         : Guid.Empty;
                    this.LogError(
                        attachmentRequest.Error.Message,
                        attachmentRequest.FileTransferInfo.Uri,
                        attachmentRequest.FileTransferInfo.FriendlyName,
                        testCaseId);
                }
                else
                {
                    // Create collectorDataEntry for each collected log.
                    var entry =
                        entries.FirstOrDefault(
                            e => Uri.Equals(e.Uri, attachmentRequest.FileTransferInfo.Uri));
                    if (entry == null)
                    {
                        entry = new AttachmentSet(
                            attachmentRequest.FileTransferInfo.Uri,
                            attachmentRequest.FileTransferInfo.FriendlyName);
                        entries.Add(entry);
                    }

                    entry.Attachments.Add(
                        new UriDataAttachment(
                            new Uri(attachmentRequest.LocalFilePath),
                            attachmentRequest.FileTransferInfo.Description));
                }

                attachmentRequest.Dispose();
            }

            return entries;
        }

        /// <inheritdoc/>
        public void AddAttachment(FileTransferInformationExtension fileTransferInfo)
        {
            ValidateArg.NotNull(fileTransferInfo, nameof(fileTransferInfo));

            if (string.IsNullOrEmpty(this.SessionOutputDirectory))
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error(
                        "DataCollectionAttachmentManager.AddAttachment: Initialize not invoked.");
                }

                return;
            }

            if (fileTransferInfo != null)
            {
                this.AddNewFileTransfer(fileTransferInfo);
            }
            else
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionAttachmentManager.AddAttachment: Got unexpected message of type FileTransferInformationExtension.");
                }
            }
        }

        /// <summary>
        /// Dispose event object
        /// </summary>
        public void Dispose()
        {
            // Dispose all attachment requests..
            this.AttachmentRequests.ForEach(request => request.Dispose());
            this.AttachmentRequests.Clear();
            this.SessionOutputDirectory = null;

            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        #endregion

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
                    this.fileCopierJobQueue?.Dispose();
                }

                this.disposed = true;
            }
        }

        #region private methods

        /// <summary>
        /// Mark the request as processed/completed. 
        /// If any error occurred during processing, error information is also sent to copy request.
        /// </summary>
        /// <param name="attachmentRequest">
        /// The file copy request.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void CompleteFileTransfer(AttachmentRequest attachmentRequest, Exception e)
        {
            attachmentRequest.CompleteRequest(e);
        }

        /// <summary>
        /// Sanity checks on CopyRequestData 
        /// </summary>
        /// <param name="attachmentRequest">
        /// The copy Request.
        /// </param>
        private static void Validate(AttachmentRequest attachmentRequest)
        {
            if (!File.Exists(attachmentRequest.FileTransferInfo.FileName))
            {
                throw new FileNotFoundException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Could not find source file '{0}'.",
                        attachmentRequest.FileTransferInfo.FileName));
            }

            var directoryName = Path.GetDirectoryName(attachmentRequest.LocalFilePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            else if (File.Exists(attachmentRequest.LocalFilePath))
            {
                File.Delete(attachmentRequest.LocalFilePath);
            }
        }

        /// <summary>
        /// Add a new file transfer (either copy/move) request.
        /// </summary>
        /// <param name="fileTransferInfo">
        /// The file Transfer Info.
        /// </param>
        private void AddNewFileTransfer(FileTransferInformationExtension fileTransferInfo)
        {
            var context = fileTransferInfo.Context;
            Debug.Assert(
                context != null,
                "DataCollectionManager.AddNewFileTransfer: FileDataHeaderMessage with null context.");

            AttachmentRequest requestCopy;
            lock (this.AttachmentRequests)
            {
                requestCopy = new AttachmentRequest(this.SessionOutputDirectory, fileTransferInfo);
                this.AttachmentRequests.Add(requestCopy);
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionAttachmentManager.AddNewFileTransfer: Enqueing for transfer.");
            }

            this.fileCopierJobQueue.QueueJob(requestCopy, 0);
        }

        /// <summary>
        /// Background job processor.
        /// </summary>
        /// <param name="attachmentReqeust">Copy request data.
        /// </param>
        private void OnAttachmentRequest(AttachmentRequest attachmentReqeust)
        {
            if (attachmentReqeust.FileTransferInfo.PerformCleanup)
            {
                this.MoveAttachment(attachmentReqeust);
            }
            else
            {
                this.CopyAttachment(attachmentReqeust);
            }
        }

        /// <summary>
        /// Copy attachment.
        /// </summary>
        /// <param name="attachmentRequest">Copy request data.</param>
        private void CopyAttachment(AttachmentRequest attachmentRequest)
        {
            Exception error = null;
            try
            {
                Validate(attachmentRequest);
                File.Copy(attachmentRequest.FileTransferInfo.FileName, attachmentRequest.LocalFilePath);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                // Let collector know of transfer completed if it has registered transferCompletedHandler.
                this.TriggerCallback(
                    attachmentRequest.FileTransferInfo.FileTransferCompletedHandler,
                    attachmentRequest.FileTransferInfo.UserToken,
                    error,
                    attachmentRequest.FileTransferInfo.FileName);
                CompleteFileTransfer(attachmentRequest, error);
            }
        }

        /// <summary>
        /// Make a callback indicating the file transfer is complete.
        /// Needed when file copy is requested (as data collector might use requested file after transfer is complete).
        /// </summary>
        /// <param name="transferCompletedCallback">
        /// The transfer completed callback.
        /// </param>
        /// <param name="userToken">
        /// The user token.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        /// <param name="path">
        /// The path.
        /// </param>
        private void TriggerCallback(
            AsyncCompletedEventHandler transferCompletedCallback,
            object userToken,
            Exception exception,
            string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path), "null or empty path");
            if (transferCompletedCallback != null)
            {
                try
                {
                    transferCompletedCallback(this, new AsyncCompletedEventArgs(exception, false, userToken));
                }
                catch (Exception e)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error(
                            "DataCollectionAttachmentManager.TriggerCallBack: Error occurred while raising the file transfer completed callback for {0}. Error: {1}",
                            path,
                            e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Move attachment.
        /// </summary>
        /// <param name="attachmentRequest">Attachment request</param>
        private void MoveAttachment(AttachmentRequest attachmentRequest)
        {
            Exception error = null;
            try
            {
                Validate(attachmentRequest);
                File.Move(attachmentRequest.FileTransferInfo.FileName, attachmentRequest.LocalFilePath);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                // Let collector know of transfer completed if it has registered transferCompletedHandler.
                this.TriggerCallback(
                    attachmentRequest.FileTransferInfo.FileTransferCompletedHandler,
                    attachmentRequest.FileTransferInfo.UserToken,
                    error,
                    attachmentRequest.FileTransferInfo.FileName);
                CompleteFileTransfer(attachmentRequest, error);
            }
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="errorMessage">
        /// The error message.
        /// </param>
        /// <param name="collectorUri">
        /// The collector uri.
        /// </param>
        /// <param name="collectorFriendlyName">
        /// The collector friendly name.
        /// </param>
        /// <param name="testCaseId">
        /// Id of testCase if available, null otherwise.
        /// </param>
        private void LogError(string errorMessage, Uri collectorUri, string collectorFriendlyName, Guid testCaseId)
        {
            //Debug.Assert(this.messageSink != null, "DataCollectionLog cannot be null");
            var args = new DataCollectionMessageEventArgs(TestMessageLevel.Error, errorMessage)
            {
                Uri = collectorUri,
                FriendlyName = collectorFriendlyName
            };

            if (!testCaseId.Equals(Guid.Empty))
            {
                args.TestCaseId = testCaseId;
            }

            this.messageSink.SendMessage(args);
        }

        #endregion
    }
}
