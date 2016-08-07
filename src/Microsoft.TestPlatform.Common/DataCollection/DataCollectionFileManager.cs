// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Manages file transfer from data collector to test runner service.
    /// </summary>
    internal class DataCollectionFileManager : IDataCollectionFileManager
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
        private const string DefaultOutputDirectoryName = "TestPlatformResults";

        /// <summary>
        /// Specifies whether the object is disposed or not. 
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Job queue for moving file from source to destination.
        /// </summary>
        private JobQueue<CopyRequestData> fileCopierJobQueue;

        /// <summary>
        /// Logger for data collection messages
        /// </summary>
        private IDataCollectionLog dataCollectionLog;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionFileManager"/> class.
        /// </summary>
        public DataCollectionFileManager()
            : this(default(IDataCollectionLog))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionFileManager"/> class.
        /// </summary>
        /// <param name="dataCollectionLog">
        /// The data collection log.
        /// </param>
        internal DataCollectionFileManager(IDataCollectionLog dataCollectionLog)
        {
            Debug.Assert(dataCollectionLog != null, "dataCollectionLog  cannot be null.");
            this.dataCollectionLog = dataCollectionLog;
            this.SessionInfo = new Dictionary<SessionId, DataCollectionSessionConfiguration>();
            this.CopyRequestDataDictionary = new Dictionary<DataCollectionContext, List<CopyRequestData>>();

            this.fileCopierJobQueue = new JobQueue<CopyRequestData>(
                this.OnFileCopyRequest,
                "DataCollectionFileManagerQueue",
                MaxQueueLength,
                MaxQueueSize,
                true,
                message => EqtTrace.Error(message));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the session info.
        /// </summary>
        internal IDictionary<SessionId, DataCollectionSessionConfiguration> SessionInfo { get; }

        /// <summary>
        /// Gets the copy request data dictionary.
        /// </summary>
        internal IDictionary<DataCollectionContext, List<CopyRequestData>> CopyRequestDataDictionary { get; }

        #endregion

        #region public methods

        /// <summary>
        /// Creates and stores session configuration for specified session id.
        /// </summary>
        /// <param name="id">session id</param>
        /// <param name="outputDirectory">base output directory for session</param>
        public void ConfigureSession(SessionId id, string outputDirectory)
        {
            ValidateArg.NotNull(id, nameof(id));

            string sessionOutputDirectory;

            if (string.IsNullOrEmpty(outputDirectory))
            {
                sessionOutputDirectory = Path.Combine(Path.GetTempPath(), DefaultOutputDirectoryName);
                sessionOutputDirectory = Path.Combine(sessionOutputDirectory, id.Id.ToString());
            }
            else
            {
                // Create a session specific directory under base output directory.
                var expandedOutputDirectory = Environment.ExpandEnvironmentVariables(outputDirectory);
                var absolutePath = Path.GetFullPath(expandedOutputDirectory);
                sessionOutputDirectory = Path.Combine(absolutePath, id.Id.ToString());
            }

            // Create the output directory if it doesn't exist.
            if (!Directory.Exists(sessionOutputDirectory))
            {
                Directory.CreateDirectory(sessionOutputDirectory);
            }

            var sessionConfiguration = new DataCollectionSessionConfiguration(id, sessionOutputDirectory);

            lock (this.SessionInfo)
            {
                if (this.SessionInfo.ContainsKey(id))
                {
                    Debug.Fail(
                        string.Format(CultureInfo.CurrentCulture, "Session with Id '{0}' is already configured", id.Id));
                    this.SessionInfo[id] = sessionConfiguration;
                }
                else
                {
                    this.SessionInfo.Add(id, sessionConfiguration);
                }
            }
        }

        /// <summary>
        /// Gets CollectorData associated with given collection context.
        /// </summary>
        /// <param name="dataCollectionContext">
        /// The data collection context.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public List<AttachmentSet> GetData(DataCollectionContext dataCollectionContext)
        {
            var entries = new List<AttachmentSet>();
            List<CopyRequestData> copyRequests;
            lock (this.CopyRequestDataDictionary)
            {
                if (!this.CopyRequestDataDictionary.TryGetValue(dataCollectionContext, out copyRequests))
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning(
                            "FileDataManager.GetData: Called for a context(SessionId:'{0}' TestCaseExecId:'{1}') which is not registered",
                            dataCollectionContext.SessionId.Id.ToString(),
                            dataCollectionContext.HasTestCase
                                ? dataCollectionContext.TestExecId.Id.ToString()
                                : "No TestCase");
                    }

                    return entries;
                }

                this.CopyRequestDataDictionary.Remove(dataCollectionContext);
            }

            foreach (var copyRequest in copyRequests)
            {
                copyRequest.WaitForCopyComplete();
                if (null != copyRequest.Error)
                {
                    // If there was error in processing the request, lets log it.
                    var testCaseId = dataCollectionContext.HasTestCase
                                         ? dataCollectionContext.TestExecId.Id
                                         : Guid.Empty;
                    this.LogError(
                        copyRequest.Error.Message,
                        copyRequest.FileDataHeaderMessage.Uri,
                        copyRequest.FileDataHeaderMessage.FriendlyName,
                        testCaseId);
                }
                else
                {
                    // Create collectorDataEntry for each collected log.
                    var entry =
                        entries.FirstOrDefault(
                            e => Uri.Equals(e.Uri, copyRequest.FileDataHeaderMessage.Uri));
                    if (null == entry)
                    {
                        entry = new AttachmentSet(
                            copyRequest.FileDataHeaderMessage.Uri,
                            copyRequest.FileDataHeaderMessage.FriendlyName);
                        entries.Add(entry);
                    }

                    entry.Attachments.Add(
                        new UriDataAttachment(
                            new Uri(copyRequest.LocalFilePath),
                            copyRequest.FileDataHeaderMessage.Description));
                }

                copyRequest.Dispose();
            }

            return entries;
        }

        /// <summary>
        /// Perform cleanup for the session.
        /// All session related entries are deleted/disposed.
        /// </summary>
        /// <param name="sessionId">Session Id.</param>
        public void CloseSession(SessionId sessionId)
        {
            ValidateArg.NotNull(sessionId, nameof(sessionId));

            var requestsToDispose = new List<CopyRequestData>();
            lock (this.CopyRequestDataDictionary)
            {
                var contextToRemove = new List<DataCollectionContext>();
                foreach (var kvp in this.CopyRequestDataDictionary)
                {
                    if (kvp.Key.SessionId.Equals(sessionId))
                    {
                        requestsToDispose.AddRange(kvp.Value);
                        contextToRemove.Add(kvp.Key);
                    }
                }

                foreach (var context in contextToRemove)
                {
                    this.CopyRequestDataDictionary.Remove(context);
                }
            }

            // Dispose all requests.
            requestsToDispose.ForEach(request => request.Dispose());
        }

        /// <summary>
        /// Process the message
        /// </summary>
        /// <param name="collectorDataMessage">Data transfer message</param>
        public void DispatchMessage(DataCollectorDataMessage collectorDataMessage)
        {
            var headerMessage = collectorDataMessage as FileDataHeaderMessage;
            if (null != headerMessage)
            {
                this.AddNewFileTransfer(headerMessage);
            }
            else
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error(
                        "DataCollectionFileManager.DispatchMessage: Got unexpected message of type '{0}'.",
                        collectorDataMessage.GetType());
                }
            }
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

        #endregion

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "backgroundFileCopier", Justification = "Bug in clr")]
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (null != this.fileCopierJobQueue)
                    {
                        this.fileCopierJobQueue.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        #region private methods

        /// <summary>
        /// Mark the request as processed/completed. 
        /// If any error occurred during processing, error information is also sent to copy request.
        /// </summary>
        /// <param name="fileCopyRequest">
        /// The file copy request.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void CompleteFileTransfer(CopyRequestData fileCopyRequest, Exception e)
        {
            fileCopyRequest.CompleteRequest(e);
        }

        /// <summary>
        /// Sanity checks on CopyRequestData 
        /// </summary>
        /// <param name="copyRequest">
        /// The copy Request.
        /// </param>
        private static void Validate(CopyRequestData copyRequest)
        {
            if (!File.Exists(copyRequest.FileDataHeaderMessage.FileName))
            {
                throw new FileNotFoundException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Could not find source file '{0}'.",
                        copyRequest.FileDataHeaderMessage.FileName));
            }

            var directoryName = Path.GetDirectoryName(copyRequest.LocalFilePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            else if (File.Exists(copyRequest.LocalFilePath))
            {
                File.Delete(copyRequest.LocalFilePath);
            }
        }

        /// <summary>
        /// Add a new file transfer (either copy/move) request.
        /// </summary>
        /// <param name="headerMessage">File data header message.</param>
        private void AddNewFileTransfer(FileDataHeaderMessage headerMessage)
        {
            DataCollectionSessionConfiguration sessionConfiguration;
            var context = headerMessage.DataCollectionContext;
            Debug.Assert(
                context != null,
                "DataCollectionManager.AddNewFileTransfer: FileDataHeaderMessage with null context.");
            lock (this.SessionInfo)
            {
                if (!this.SessionInfo.TryGetValue(context.SessionId, out sessionConfiguration))
                {
                    Debug.Fail(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "DataCollectionManager.AddNewFileTransfer: File transfer requested for unknown session '{0}'",
                            context.SessionId));
                    return;
                }
            }

            var outputDirectory = sessionConfiguration.OutputDirectory;

            List<CopyRequestData> requestedCopies;
            lock (this.CopyRequestDataDictionary)
            {
                if (!this.CopyRequestDataDictionary.TryGetValue(context, out requestedCopies))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "DataCollectionFileManager.AddNewFileTransfer: No copy request for collection context ({0}:{1}).",
                            context.SessionId.Id.ToString(),
                            context.HasTestCase ? context.TestExecId.Id.ToString() : "NoTestCase");
                    }

                    requestedCopies = new List<CopyRequestData>();
                    this.CopyRequestDataDictionary.Add(context, requestedCopies);
                }
                else
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "DataCollectionFileManager.AddNewFileTransfer: Found existing copy request(s) for collection context ({0}:{1}).",
                            context.SessionId.Id.ToString(),
                            context.HasTestCase ? context.TestExecId.Id.ToString() : "NoTestCase");
                    }
                }
            }

            CopyRequestData requestCopy;
            lock (requestedCopies)
            {
                requestCopy = new CopyRequestData(outputDirectory, headerMessage);
                requestedCopies.Add(requestCopy);
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionFileManager.AddNewFileTransfer: Enqueing for transfer.");
            }

            this.fileCopierJobQueue.QueueJob(requestCopy, 0);
        }

        /// <summary>
        /// Background job processor.
        /// </summary>
        /// <param name="fileCopyRequest">Copy request data.
        /// </param>
        private void OnFileCopyRequest(CopyRequestData fileCopyRequest)
        {
            if (fileCopyRequest.FileDataHeaderMessage.PerformCleanup)
            {
                this.FileMove(fileCopyRequest);
            }
            else
            {
                this.FileCopy(fileCopyRequest);
            }
        }

        /// <summary>
        /// Do a file copy for the specified request
        /// </summary>
        /// <param name="copyRequest">Copy request data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Need to catch all exception type to send  as data collection error to client.")]
        private void FileCopy(CopyRequestData copyRequest)
        {
            Exception error = null;
            try
            {
                Validate(copyRequest);
                File.Copy(copyRequest.FileDataHeaderMessage.FileName, copyRequest.LocalFilePath);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                // Let collector know of transfer completed if it has registered transferCompletedHandler.
                this.TriggerCallback(
                    copyRequest.FileDataHeaderMessage.FileTransferCompletedHandler,
                    copyRequest.FileDataHeaderMessage.UserToken,
                    error,
                    copyRequest.FileDataHeaderMessage.FileName);
                CompleteFileTransfer(copyRequest, error);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Ignorable error which shouldn't crash the data collection framework.")]
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
                            "DataCollectionFileManager.TriggerCallBack: Error occurred while raising the file transfer completed callback for {0}. Error: {1}",
                            path,
                            e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Move the file as specified in request.
        /// </summary>
        /// <param name="copyRequest">Copy request data</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Need to catch all exception type to send  as data collection error to client.")]
        private void FileMove(CopyRequestData copyRequest)
        {
            Exception error = null;
            try
            {
                Validate(copyRequest);
                File.Move(copyRequest.FileDataHeaderMessage.FileName, copyRequest.LocalFilePath);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                // Let collector know of transfer completed if it has registered transferCompletedHandler.
                this.TriggerCallback(
                    copyRequest.FileDataHeaderMessage.FileTransferCompletedHandler,
                    copyRequest.FileDataHeaderMessage.UserToken,
                    error,
                    copyRequest.FileDataHeaderMessage.FileName);
                CompleteFileTransfer(copyRequest, error);
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
            Debug.Assert(this.dataCollectionLog != null, "DataCollectionLog cannot be null");
            var args = new DataCollectionMessageEventArgs(TestMessageLevel.Error, errorMessage)
            {
                Uri = collectorUri,
                FriendlyName = collectorFriendlyName
            };

            if (!testCaseId.Equals(Guid.Empty))
            {
                args.TestCaseId = testCaseId;
            }

            this.dataCollectionLog.SendDataCollectionMessage(args);
        }

        #endregion
    }
}
