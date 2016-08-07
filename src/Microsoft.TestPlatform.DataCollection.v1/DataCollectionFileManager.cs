// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.DataCollection.V1.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using CollectorDataEntry = Microsoft.VisualStudio.TestPlatform.ObjectModel.AttachmentSet;
    using DataCollectionContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionContext;
    using SessionId = Microsoft.VisualStudio.TestTools.Common.SessionId;

    /// <summary>
    /// Manages file transfer from data collector to test runner service.
    /// </summary>
    internal class DataCollectionFileManager : IDisposable
    {
        #region Fields
        /// <summary>
        /// Max number of file transfer jobs.
        /// </summary>
        private const int MaxJobCount = 1024;


        /// <summary>
        /// Default results directory to be used when user didn't specifiy.
        /// </summary>
        private const string DefaultOutputDirectoryName = "TestPlatformResults";


        /// <summary>
        /// Specifies whether the object is disposed or not. 
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Configuration information about each active session. 
        /// (Currently there should be only one active session)
        /// </summary>
        private IDictionary<SessionId, DataCollectionSessionConfiguration> sessionInfo;

        /// <summary>
        /// Active file copy requests in the system.
        /// </summary>
        private IDictionary<DataCollectionContext, List<CopyRequestData>> copyfiles;


        /// <summary>
        /// Background job processor for moving file from source to destination.
        /// </summary>
        private TestTools.Common.BackgroundJobProcessor<CopyRequestData> backgroundFileCopier;

        // todo : why is this required?
        /// <summary>
        /// Logger for data collection messages
        /// </summary>
        private IDataCollectionLog dataCollectionLog;

        #endregion        

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        internal DataCollectionFileManager(IDataCollectionLog dataCollectionLog)
        {
            Debug.Assert(dataCollectionLog != null, "dataCollectionLog  cannot be null.");
            this.dataCollectionLog = dataCollectionLog;
            sessionInfo = new Dictionary<SessionId, DataCollectionSessionConfiguration>();
            copyfiles = new Dictionary<DataCollectionContext, List<CopyRequestData>>();

            //Background job processor for file copy/move operation(s).
            backgroundFileCopier = new TestTools.Common.BackgroundJobProcessor<CopyRequestData>(
                "DataCollectionFileManager",
                OnFileCopyRequest,
                MaxJobCount);
        }
        #endregion

        #region Properties
        internal IDictionary<SessionId, DataCollectionSessionConfiguration> SessionInfo
        {
            get
            {
                return sessionInfo;
            }
        }

        internal IDictionary<DataCollectionContext, List<CopyRequestData>> Copyfiles
        {
            get
            {
                return copyfiles;
            }
        }

        #endregion

        #region internal methods
        /// <summary>
        /// Creates and stores session configuration for specified session id.
        /// </summary>
        /// <param name="id">session id</param>
        /// <param name="outputDirectory">base output directory for session</param>
        internal void ConfigureSession(SessionId id, string outputDirectory)
        {
            ValidateArg.NotNull<SessionId>(id, "id");

            string sessionOutputDirectory = string.Empty;
            if (string.IsNullOrEmpty(outputDirectory))
            {
                sessionOutputDirectory = Path.Combine(Path.GetTempPath(), DefaultOutputDirectoryName);
                sessionOutputDirectory = Path.Combine(sessionOutputDirectory, id.Id.ToString());
            }
            else
            {
                //Create a session specific directory under base output directory.
                string expandedOutputDirectory = Environment.ExpandEnvironmentVariables(outputDirectory);
                string absolutePath = Path.GetFullPath(expandedOutputDirectory);
                sessionOutputDirectory = Path.Combine(absolutePath, id.Id.ToString());
            }

            //Create the output directory if it doesn't exist.
            if (!Directory.Exists(sessionOutputDirectory))
            {
                Directory.CreateDirectory(sessionOutputDirectory);
            }

            DataCollectionSessionConfiguration sessionConfiguration = new DataCollectionSessionConfiguration(id, sessionOutputDirectory);

            lock (sessionInfo)
            {
                if (sessionInfo.ContainsKey(id))
                {
                    Debug.Fail(string.Format(CultureInfo.CurrentCulture, "Session with Id '{0}' is already configured", id.Id));
                    sessionInfo[id] = sessionConfiguration;
                }
                else
                {
                    sessionInfo.Add(id, sessionConfiguration);
                }
            }
        }

        /// <summary>
        /// Gets CollectorData associated with given collection context.
        /// </summary>
        /// <param name="collectionContext"></param>
        /// <returns></returns>
        internal List<CollectorDataEntry> GetData(DataCollectionContext collectionContext)
        {
            List<CollectorDataEntry> entries = new List<CollectorDataEntry>();
            List<CopyRequestData> copyRequests;
            lock (copyfiles)
            {
                if (!copyfiles.TryGetValue(collectionContext, out copyRequests))
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("FileDataManager.GetData: Called for a context(SessionId:'{0}' TestCaseExecId:'{1}') which is not registered",
                            collectionContext.SessionId.Id.ToString(), collectionContext.HasTestCase ? collectionContext.TestExecId.Id.ToString() : "No TestCase");
                    }
                    return entries;
                }
                copyfiles.Remove(collectionContext);
            }
            foreach (CopyRequestData copyRequest in copyRequests)
            {
                copyRequest.WaitForCopyComplete();
                if (null != copyRequest.Error)
                {
                    //If there was error in processing the request, lets log it.
                    Guid testCaseId = collectionContext.HasTestCase ? collectionContext.TestExecId.Id : Guid.Empty;
                    LogError(copyRequest.Error.Message, copyRequest.FileDataHeaderMessage.Uri, copyRequest.FileDataHeaderMessage.FriendlyName, testCaseId);
                }
                else
                {
                    //Create collectorDataEntry for each collected log.
                    CollectorDataEntry entry = entries.FirstOrDefault<CollectorDataEntry>(e => Uri.Equals(e.Uri, copyRequest.FileDataHeaderMessage.Uri));
                    if (null == entry)
                    {
                        entry = new CollectorDataEntry(copyRequest.FileDataHeaderMessage.Uri, copyRequest.FileDataHeaderMessage.FriendlyName);
                        entries.Add(entry);
                    }
                    entry.Attachments.Add(new UriDataAttachment(new Uri(copyRequest.LocalFilePath), copyRequest.FileDataHeaderMessage.Description));
                }
                copyRequest.Dispose();
            }
            return entries;
        }


        /// <summary>
        /// Perform cleanup for the session.
        /// All session related entries are deleted/disposed.
        /// </summary>
        /// <param name="id"></param>
        internal void CloseSession(SessionId id)
        {
            ValidateArg.NotNull<SessionId>(id, "id");

            List<CopyRequestData> requestsToDispose = new List<CopyRequestData>();
            lock (copyfiles)
            {
                List<DataCollectionContext> contextToRemove = new List<DataCollectionContext>();
                foreach (KeyValuePair<DataCollectionContext, List<CopyRequestData>> kvp in copyfiles)
                {
                    if (kvp.Key.SessionId.Equals(id))
                    {
                        requestsToDispose.AddRange(kvp.Value);
                        contextToRemove.Add(kvp.Key);
                    }
                }
                foreach (DataCollectionContext context in contextToRemove)
                {
                    copyfiles.Remove(context);
                }
            }
            //Now dispose all requests.
            requestsToDispose.ForEach(request => request.Dispose());
        }


        /// <summary>
        /// Process the message
        /// </summary>
        /// <param name="collectorDataMessage">Data transfer message</param>
        internal void DispatchMessage(DataCollectorDataMessage collectorDataMessage)
        {
            FileDataHeaderMessage headerMessage = collectorDataMessage as FileDataHeaderMessage;
            if (null != headerMessage)
            {
                AddNewFileTransfer(headerMessage);
            }
            else
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectionFileManager.DispatchMessage: Got unexpected message of type '{0}'.", collectorDataMessage.GetType());
                }
            }
        }

        #endregion

        #region private methods
        /// <summary>
        /// Add a new file transfer (either copy/move) request.
        /// </summary>
        /// <param name="headerMessage"></param>
        private void AddNewFileTransfer(FileDataHeaderMessage headerMessage)
        {
            DataCollectionSessionConfiguration sessionConfiguration;
            DataCollectionContext context = headerMessage.DataCollectionContext;
            Debug.Assert(context != null, "DataCollectionManager.AddNewFileTransfer: FileDataHeaderMessage with null context.");
            lock (sessionInfo)
            {

                if (!sessionInfo.TryGetValue(context.SessionId, out sessionConfiguration))
                {
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "DataCollectionManager.AddNewFileTransfer: File transfer requested for unknown session '{0}'",
                        context.SessionId));
                    return;
                }
            }
            string outputDirectory = sessionConfiguration.OutputDirectory;

            List<CopyRequestData> requestedCopies;
            lock (copyfiles)
            {
                if (!copyfiles.TryGetValue(context, out requestedCopies))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("DataCollectionFileManager.AddNewFileTransfer: No copy request for collection context ({0}:{1}).",
                            context.SessionId.Id.ToString(), context.HasTestCase ? context.TestExecId.Id.ToString() : "NoTestCase");
                    }
                    requestedCopies = new List<CopyRequestData>();
                    copyfiles.Add(context, requestedCopies);
                }
                else
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("DataCollectionFileManager.AddNewFileTransfer: Found existing copy request(s) for collection context ({0}:{1}).",
                            context.SessionId.Id.ToString(), context.HasTestCase ? context.TestExecId.Id.ToString() : "NoTestCase");
                    }
                }
            }

            CopyRequestData requestCopy = null;
            lock (requestedCopies)
            {
                requestCopy = new CopyRequestData(outputDirectory, headerMessage);
                //TODO::dhruvk::Detect duplicate file names
                //TODO::dhruvk::File name needs to be reserved to avoid duplicate conflicts.
                requestedCopies.Add(requestCopy);
            }

            if (null != requestCopy)
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectionFileManager.AddNewFileTransfer: Enqueing for transfer.");
                }
                backgroundFileCopier.Enqueue(requestCopy);
            }

        }


        /// <summary>
        /// Background job processor.
        /// </summary>
        /// <param name="fileCopyRequest"></param>
        /// <param name="queuedJobs"></param>
        private void OnFileCopyRequest(CopyRequestData fileCopyRequest, TestTools.Common.IQueuedJobs<CopyRequestData> queuedJobs)
        {
            if (fileCopyRequest.FileDataHeaderMessage.PerformCleanup)
            {
                FileMove(fileCopyRequest);
            }
            else
            {
                FileCopy(fileCopyRequest);
            }
        }


        /// <summary>
        /// Do a file copy for the specified request
        /// </summary>
        /// <param name="copyRequest"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all exception type to send  as data collection error to client.")]
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
                //Let collector know of transfer completed if it has registered transferCompletedHandler.
                TriggerCallback(copyRequest.FileDataHeaderMessage.FileTransferCompletedHandler, copyRequest.FileDataHeaderMessage.UserToken, error, copyRequest.FileDataHeaderMessage.FileName);
                CompleteFileTransfer(copyRequest, error);
            }
        }


        /// <summary>
        /// Sanity checks on CopyRequestData 
        /// </summary>
        /// <param name="fileCopyRequest"></param>
        private static void Validate(CopyRequestData fileCopyRequest)
        {
            if (!File.Exists(fileCopyRequest.FileDataHeaderMessage.FileName))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture,
                    "Could not find source file '{0}'.", fileCopyRequest.FileDataHeaderMessage.FileName));
            }
            string directoryName = Path.GetDirectoryName(fileCopyRequest.LocalFilePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            else if (File.Exists(fileCopyRequest.LocalFilePath))
            {
                File.Delete(fileCopyRequest.LocalFilePath);
            }
        }


        /// <summary>
        /// Make a callback indicating the file transfer is complete.
        /// Needed when file copy is requested (as data collector might use requested file after transfer is complete).
        /// </summary>
        /// <param name="transferCompletedCallback"></param>
        /// <param name="userToken"></param>
        /// <param name="exception"></param>
        /// <param name="path"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignorable error which shouldn't crash the data collection framework.")]
        private void TriggerCallback(AsyncCompletedEventHandler transferCompletedCallback, object userToken, Exception exception, string path)
        {
            Debug.Assert(!String.IsNullOrEmpty(path), "null or empty path");
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
                        EqtTrace.Error("DataCollectionFileManager.TriggerCallBack: Error occurred while raising the file transfer completed callback for {0}. Error: {1}", path, e.ToString());
                    }
                }
            }
        }


        /// <summary>
        /// Move the file as specified in request.
        /// </summary>
        /// <param name="copyRequest"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all exception type to send  as data collection error to client.")]
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
                //Let collector know of transfer completed if it has registered transferCompletedHandler.
                TriggerCallback(copyRequest.FileDataHeaderMessage.FileTransferCompletedHandler, copyRequest.FileDataHeaderMessage.UserToken, error, copyRequest.FileDataHeaderMessage.FileName);
                CompleteFileTransfer(copyRequest, error);
            }
        }


        /// <summary>
        /// Mark the request as processed/completed. 
        /// If any error occurred during processing, error information is also sent to copyrequest.
        /// </summary>
        /// <param name="fileCopyRequest"></param>
        /// <param name="e"></param>
        private static void CompleteFileTransfer(CopyRequestData fileCopyRequest, Exception e)
        {
            fileCopyRequest.CompleteRequest(e);
        }


        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="collectorUri"></param>
        /// <param name="collectorFriendlyName"></param>
        /// <param name="testCaseId">Id of testCase if available, null otherwise</param>
        private void LogError(string errorMessage, Uri collectorUri, string collectorFriendlyName, Guid testCaseId)
        {
            Debug.Assert(dataCollectionLog != null, "DataCollectionLog cannot be null");
            DataCollectionMessageEventArgs args = new DataCollectionMessageEventArgs(TestMessageLevel.Error, errorMessage);
            args.Uri = collectorUri;
            args.FriendlyName = collectorFriendlyName;
            if (!Guid.Empty.Equals(testCaseId))
            {
                args.TestCaseId = testCaseId;
            }
            dataCollectionLog.SendDataCollectionMessage(args);
        }

        #endregion

        #region IDisposable
        /// <summary>
        /// Dispose event object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "backgroundFileCopier", Justification = "Bug in clr")]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (null != backgroundFileCopier)
                    {
                        backgroundFileCopier.Abort();

                        // Dont dispose the background job processor as this is leading to 99% cpu on some machines
                        // because of clr bug # 653263. Not disposing it is fine as the dispose is called only on 
                        // process exit and on process exit all handles are disposed automatically. 
                        //
                        // m_backgroundFileCopier.Dispose();
                        backgroundFileCopier = null;
                    }
                }
                disposed = true;
            }
        }
        #endregion

    }
}
