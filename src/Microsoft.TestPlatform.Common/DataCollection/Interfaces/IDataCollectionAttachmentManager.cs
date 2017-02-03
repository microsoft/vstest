// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The DataCollectionAttachmentManager Interface.
    /// </summary>
    internal interface IDataCollectionAttachmentManager
    {
        /// <summary>
        /// Initializes session, output directory and data collection log.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="outputDirectory">
        /// The output directory.
        /// </param>
        /// <param name="messageSink">
        /// The message Sink.
        /// </param>
        void Initialize(SessionId id, string outputDirectory, IMessageSink messageSink);

        /// <summary>
        /// Gets attachment sets associated with given collection context.
        /// </summary>
        /// <param name="dataCollectionContext">
        /// The data collection context.
        /// </param>
        /// <param name="isCancelled">
        /// Value specifying whether the test run is cancelled or not.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        List<AttachmentSet> GetAttachments(DataCollectionContext dataCollectionContext, bool isCancelled = false);

        /// <summary>
        /// Adds new attachment to current context
        /// </summary>
        /// <param name="fileTransferInfo">
        /// The file Transfer Info.
        /// </param>
        /// <param name="sendFileCompletedCallback">
        /// The send File Completed Callback.
        /// </param>
        /// <param name="typeUri">
        /// The type Uri.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly Name.
        /// </param>
        void AddAttachment(FileTransferInformation fileTransferInfo, AsyncCompletedEventHandler sendFileCompletedCallback, Uri typeUri, string friendlyName);

        /// <summary>
        /// Stops processing further transfer requests as test run is cancelled.
        /// </summary>
        void Cancel();
    }
}