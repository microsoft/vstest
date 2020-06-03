// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// Manages file transfer from data collector to test runner service.
    /// </summary>
    internal class DataCollectionAttachmentManager : IDataCollectionAttachmentManager
    {
        private static object attachmentTaskLock = new object();

        #region Fields

        /// <summary>
        /// Default results directory to be used when user didn't specify.
        /// </summary>
        private const string DefaultOutputDirectoryName = "TestResults";

        /// <summary>
        /// Logger for data collection messages
        /// </summary>
        private IMessageSink messageSink;

        /// <summary>
        /// Attachment transfer tasks associated with a given datacollection context.
        /// </summary>
        private Dictionary<DataCollectionContext, List<Task>> attachmentTasks;

        /// <summary>
        /// Use to cancel attachment transfers if test run is canceled.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// File helper instance.
        /// </summary>
        private IFileHelper fileHelper;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionAttachmentManager"/> class.
        /// </summary>
        public DataCollectionAttachmentManager()
            : this(new Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionAttachmentManager"/> class.
        /// </summary>
        /// <param name="fileHelper">File helper instance.</param>
        protected DataCollectionAttachmentManager(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.attachmentTasks = new Dictionary<DataCollectionContext, List<Task>>();
            this.AttachmentSets = new Dictionary<DataCollectionContext, Dictionary<Uri, AttachmentSet>>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the session output directory.
        /// </summary>
        internal string SessionOutputDirectory { get; private set; }

        /// <summary>
        /// Gets the attachment sets for the given datacollection context.
        /// </summary>
        internal Dictionary<DataCollectionContext, Dictionary<Uri, AttachmentSet>> AttachmentSets
        {
            get; private set;
        }
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
                this.SessionOutputDirectory = Path.Combine(Path.GetTempPath(), DefaultOutputDirectoryName, id.Id.ToString());
            }
            else
            {
                // Create a session specific directory under base output directory.
                var expandedOutputDirectory = Environment.ExpandEnvironmentVariables(outputDirectory);
                var absolutePath = Path.GetFullPath(expandedOutputDirectory);
                this.SessionOutputDirectory = Path.Combine(absolutePath, id.Id.ToString());
            }

            try
            {
                // Create the output directory if it doesn't exist.
                if (!Directory.Exists(this.SessionOutputDirectory))
                {
                    Directory.CreateDirectory(this.SessionOutputDirectory);
                }
            }
            catch (UnauthorizedAccessException accessException)
            {
                string accessDeniedMessage = string.Format(CultureInfo.CurrentCulture, Resources.Resources.AccessDenied, accessException.Message);
                ConsoleOutput.Instance.Error(false, accessDeniedMessage);
                throw;
            }

        }

        /// <inheritdoc/>
        public List<AttachmentSet> GetAttachments(DataCollectionContext dataCollectionContext)
        {
            try
            {
                Task.WhenAll(this.attachmentTasks[dataCollectionContext].ToArray()).Wait();
            }
            catch (Exception ex)
            {
                EqtTrace.Error("DataCollectionAttachmentManager.GetAttachments: Fail to get attachments: {0} ", ex);
            }

            List<AttachmentSet> attachments = new List<AttachmentSet>();

            Dictionary<Uri, AttachmentSet> uriAttachmentSetMap;
            if (this.AttachmentSets.TryGetValue(dataCollectionContext, out uriAttachmentSetMap))
            {
                attachments = uriAttachmentSetMap.Values.ToList();
                this.attachmentTasks.Remove(dataCollectionContext);
                this.AttachmentSets.Remove(dataCollectionContext);
            }

            return attachments;
        }

        /// <inheritdoc/>
        public void AddAttachment(FileTransferInformation fileTransferInfo, AsyncCompletedEventHandler sendFileCompletedCallback, Uri uri, string friendlyName)
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

            if (!this.AttachmentSets.ContainsKey(fileTransferInfo.Context))
            {
                var uriAttachmentSetMap = new Dictionary<Uri, AttachmentSet>();
                this.AttachmentSets.Add(fileTransferInfo.Context, uriAttachmentSetMap);
                this.attachmentTasks.Add(fileTransferInfo.Context, new List<Task>());
            }

            if (!this.AttachmentSets[fileTransferInfo.Context].ContainsKey(uri))
            {
                this.AttachmentSets[fileTransferInfo.Context].Add(uri, new AttachmentSet(uri, friendlyName));
            }

            this.AddNewFileTransfer(fileTransferInfo, sendFileCompletedCallback, uri, friendlyName);
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Sanity checks on CopyRequestData 
        /// </summary>
        /// <param name="fileTransferInfo">
        /// The file Transfer Info.
        /// </param>
        /// <param name="localFilePath">
        /// The local File Path.
        /// </param>
        private static void Validate(FileTransferInformation fileTransferInfo, string localFilePath)
        {
            if (!File.Exists(fileTransferInfo.FileName))
            {
                throw new FileNotFoundException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Could not find source file '{0}'.",
                        fileTransferInfo.FileName));
            }

            var directoryName = Path.GetDirectoryName(localFilePath);

            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            else if (File.Exists(localFilePath))
            {
                File.Delete(localFilePath);
            }
        }

        /// <summary>
        /// Add a new file transfer (either copy/move) request.
        /// </summary>
        /// <param name="fileTransferInfo">
        /// The file Transfer Info.
        /// </param>
        /// <param name="sendFileCompletedCallback">
        /// The send File Completed Callback.
        /// </param>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly Name.
        /// </param>
        private void AddNewFileTransfer(FileTransferInformation fileTransferInfo, AsyncCompletedEventHandler sendFileCompletedCallback, Uri uri, string friendlyName)
        {
            var context = fileTransferInfo.Context;
            Debug.Assert(
                context != null,
                "DataCollectionManager.AddNewFileTransfer: FileDataHeaderMessage with null context.");

            var testCaseId = fileTransferInfo.Context.HasTestCase
                                 ? fileTransferInfo.Context.TestExecId.Id.ToString()
                                 : string.Empty;

            var directoryPath = Path.Combine(
                this.SessionOutputDirectory,
                testCaseId);
            var localFilePath = Path.Combine(directoryPath, Path.GetFileName(fileTransferInfo.FileName));

            var task = Task.Factory.StartNew(
                () =>
                {
                    Validate(fileTransferInfo, localFilePath);

                    if (this.cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        this.cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    }

                    try
                    {
                        if (fileTransferInfo.PerformCleanup)
                        {
                            if (EqtTrace.IsInfoEnabled)
                            {
                                EqtTrace.Info("DataCollectionAttachmentManager.AddNewFileTransfer : Moving file {0} to {1}", fileTransferInfo.FileName, localFilePath);
                            }

                            this.fileHelper.MoveFile(fileTransferInfo.FileName, localFilePath);

                            if (EqtTrace.IsInfoEnabled)
                            {
                                EqtTrace.Info("DataCollectionAttachmentManager.AddNewFileTransfer : Moved file {0} to {1}", fileTransferInfo.FileName, localFilePath);
                            }
                        }
                        else
                        {
                            if (EqtTrace.IsInfoEnabled)
                            {
                                EqtTrace.Info("DataCollectionAttachmentManager.AddNewFileTransfer : Copying file {0} to {1}", fileTransferInfo.FileName, localFilePath);
                            }

                            this.fileHelper.CopyFile(fileTransferInfo.FileName, localFilePath);

                            if (EqtTrace.IsInfoEnabled)
                            {
                                EqtTrace.Info("DataCollectionAttachmentManager.AddNewFileTransfer : Copied file {0} to {1}", fileTransferInfo.FileName, localFilePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogError(
                           ex.ToString(),
                           uri,
                           friendlyName,
                           Guid.Parse(testCaseId));

                        throw;
                    }
                },
                this.cancellationTokenSource.Token);

            var continuationTask = task.ContinueWith(
                (t) =>
                {
                    try
                    {
                        if (t.Exception == null)
                        {
                            // Uri doesn't recognize file paths in unix. See https://github.com/dotnet/corefx/issues/1745
                            var attachmentUri = new UriBuilder() { Scheme = "file", Host = "", Path = localFilePath }.Uri;
                            lock (attachmentTaskLock)
                            {
                                this.AttachmentSets[fileTransferInfo.Context][uri].Attachments.Add(new UriDataAttachment(attachmentUri, fileTransferInfo.Description));
                            }
                        }

                        if (sendFileCompletedCallback != null)
                        {
                            sendFileCompletedCallback(this, new AsyncCompletedEventArgs(t.Exception, false, fileTransferInfo.UserToken));
                        }
                    }
                    catch (Exception e)
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error(
                                "DataCollectionAttachmentManager.TriggerCallBack: Error occurred while raising the file transfer completed callback for {0}. Error: {1}",
                                localFilePath,
                                e.ToString());
                        }
                    }
                },
                this.cancellationTokenSource.Token);

            this.attachmentTasks[fileTransferInfo.Context].Add(continuationTask);
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
