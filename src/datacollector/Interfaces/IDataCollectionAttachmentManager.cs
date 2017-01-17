// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The DataCollectionFileManager interface.
    /// </summary>
    internal interface IDataCollectionAttachmentManager : IDisposable
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
        /// <param name="dataCollectionLog">
        /// The data collection log.
        /// </param>
        void Initialize(SessionId id, string outputDirectory, IDataCollectionLog dataCollectionLog);

        /// <summary>
        /// Gets attachment sets associated with given collection context.
        /// </summary>
        /// <param name="dataCollectionContext">
        /// The data collection context.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        List<AttachmentSet> GetAttachments(DataCollectionContext dataCollectionContext);

        /// <summary>
        /// Adds attachment.
        /// </summary>
        /// <param name="fileTransferInfo">
        /// The file Transfer Info.
        /// </param>
        void AddAttachment(FileTransferInformationExtension fileTransferInfo);
    }
}