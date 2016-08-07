// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection
{
    using System;
    using System.ComponentModel;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Base class for all message used in file transfer.
    /// </summary>
    internal class DataCollectorDataMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorDataMessage"/> class. 
        /// </summary>
        /// <param name="context">Data collection context.
        /// </param>
        /// <param name="uri">Uri of data collector
        /// </param>
        /// <param name="friendlyName">Friendly name of data collector
        /// </param>
        internal DataCollectorDataMessage(DataCollectionContext context, Uri uri, string friendlyName)
        {
            ValidateArg.NotNull(context, "context");
            ValidateArg.NotNull(uri, "uri");
            ValidateArg.NotNullOrEmpty(friendlyName, "friendlyName");

            this.DataCollectionContext = context;
            this.Uri = uri;
            this.FriendlyName = friendlyName;
        }

        /// <summary>
        /// Gets data collection context in which transfer is initiated.
        /// </summary>
        internal DataCollectionContext DataCollectionContext
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets Uri of data collector initiating the data transfer
        /// </summary>
        internal Uri Uri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets friendly name of data collector initiating the data transfer.
        /// </summary>
        internal string FriendlyName
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Used for initiating a file transfer. This message includes all of the relevant information
    /// about the file transfer.
    /// </summary>
    internal sealed class FileDataHeaderMessage : DataCollectorDataMessage
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDataHeaderMessage"/> class. 
        /// Initializes with all of the relevant information about the file transfer.
        /// </summary>
        /// <param name="context">
        /// Content that the file is being sent for.
        /// </param>
        /// <param name="fileName">
        /// Path to the file.
        /// </param>
        /// <param name="description">
        /// A short description of the file.
        /// </param>
        /// <param name="deleteFile">
        /// Indicates if the file on the remote machine will be deleted after the file transfer is completed.
        /// </param>
        /// <param name="userToken">User token
        /// </param>
        /// <param name="fileTransferCompletedHandler">
        /// Handler that should be called when transfer is complete.
        /// </param>
        /// <param name="collectorUri">
        /// Uri of data collector initiating the transfer
        /// </param>
        /// <param name="collectorFriendlyName">
        /// Friendly name of the collector initiating the transfer
        /// </param>
        internal FileDataHeaderMessage(
            DataCollectionContext context, 
            string fileName, 
            string description, 
            bool deleteFile, 
            object userToken, 
            AsyncCompletedEventHandler fileTransferCompletedHandler, 
            Uri collectorUri, 
            string collectorFriendlyName)
            : base(context, collectorUri, collectorFriendlyName)
        {
            ValidateArg.NotNullOrEmpty(fileName, "fileName");
            ValidateArg.NotNull(description, "description");

            this.FileName = fileName;
            this.PerformCleanup = deleteFile;
            this.Description = description;
            this.UserToken = userToken;
            this.FileTransferCompletedHandler = fileTransferCompletedHandler;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the original path to the file on the client machine.
        /// </summary>
        public string FileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets display name to use for the file.
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the file on the remote machine will be deleted after the file transfer is completed.
        /// </summary>
        public bool PerformCleanup
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets called when file transfer is completed.
        /// </summary>
        public AsyncCompletedEventHandler FileTransferCompletedHandler
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the user token.
        /// </summary>
        public object UserToken
        {
            get;
            private set;
        }
        #endregion
    }
}