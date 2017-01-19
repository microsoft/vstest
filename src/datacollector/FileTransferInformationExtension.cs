// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.ComponentModel;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Extends FileTransferInformation and adds more functionality.
    /// </summary>
    internal class FileTransferInformationExtension : FileTransferInformation
    {
        /// <inheritdoc/>
        public FileTransferInformationExtension(DataCollectionContext context, string path, string description, bool deleteFile, object userToken, AsyncCompletedEventHandler fileTransferCompletedHandler, Uri uri, string friendlyName) : base(context, path, deleteFile)
        {
            this.Uri = uri;
            this.FriendlyName = friendlyName;
            this.FileTransferCompletedHandler = fileTransferCompletedHandler;
            this.Description = description;
            this.UserToken = userToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileTransferInformationExtension"/> class.
        /// </summary>
        /// <param name="fileTransferInformation">
        /// The file transfer information.
        /// </param>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name.
        /// </param>
        /// <param name="fileTransferCompletedHandler">
        /// The file transfer completed handler.
        /// </param>
        public FileTransferInformationExtension(FileTransferInformation fileTransferInformation, Uri uri, string friendlyName, AsyncCompletedEventHandler fileTransferCompletedHandler) :
            this(fileTransferInformation.Context, fileTransferInformation.Path, fileTransferInformation.Description, fileTransferInformation.PerformCleanup, fileTransferInformation.UserToken, fileTransferCompletedHandler, uri, friendlyName)
        {
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
        /// Gets Uri of data collector initiating the data transfer
        /// </summary>
        public Uri Uri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets friendly name of data collector initiating the data transfer.
        /// </summary>
        public string FriendlyName
        {
            get;
            private set;
        }
    }
}
