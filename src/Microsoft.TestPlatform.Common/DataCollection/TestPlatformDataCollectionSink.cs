// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System.ComponentModel;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The test platform data collection sink.
    /// </summary>
    internal class TestPlatformDataCollectionSink : DataCollectionSink
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatformDataCollectionSink"/> class. 
        /// Creates a data collector sink for data transfer.
        /// </summary>
        /// <param name="attachmentManager">
        /// The attachment Manager.
        /// </param>
        /// <param name="dataCollectorConfig">
        /// Data collector info.
        /// </param>
        internal TestPlatformDataCollectionSink(IDataCollectionAttachmentManager attachmentManager, DataCollectorConfig dataCollectorConfig)
        {
            ValidateArg.NotNull(attachmentManager, nameof(attachmentManager));
            ValidateArg.NotNull(dataCollectorConfig, nameof(dataCollectorConfig));

            this.DataCollectorConfig = dataCollectorConfig;
            this.AttachmentManager = attachmentManager;
        }

        /// <summary>
        /// Event handler for handling file transfer completed event.
        /// </summary>
        public override event AsyncCompletedEventHandler SendFileCompleted;

        /// <summary>
        /// Gets or sets message sink to transfer collection message.
        /// </summary>
        private IDataCollectionAttachmentManager AttachmentManager
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets dataCollector with which this data sink is associated.
        /// </summary>
        private DataCollectorConfig DataCollectorConfig
        {
            get; set;
        }

        /// <summary>
        /// Sends a file asynchronously.
        /// </summary>
        /// <param name="fileTransferInformation">Information about the file being transferred.</param>
        public override void SendFileAsync(FileTransferInformation fileTransferInformation)
        {
            ValidateArg.NotNull(fileTransferInformation, "fileTransferInformation");

            this.AttachmentManager.AddAttachment(fileTransferInformation, this.SendFileCompleted, this.DataCollectorConfig.TypeUri, this.DataCollectorConfig.FriendlyName);
        }
    }
}
