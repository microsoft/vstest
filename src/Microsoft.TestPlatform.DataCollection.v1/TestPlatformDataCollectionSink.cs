// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using DataCollectionSink = Microsoft.VisualStudio.TestTools.Execution.DataCollectionSink;
    using DataCollectorInformation = Microsoft.VisualStudio.TestTools.Execution.DataCollectorInformation;
    using FileTransferInformation = Microsoft.VisualStudio.TestTools.Execution.FileTransferInformation;
    using StreamTransferInformation = Microsoft.VisualStudio.TestTools.Execution.StreamTransferInformation;

    /// <summary>
    /// The test platform data collection sink.
    /// </summary>
    internal sealed class TestPlatformDataCollectionSink : DataCollectionSink
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatformDataCollectionSink"/> class. 
        /// Creates a data collector sink for data transfer.
        /// </summary>
        /// <param name="messageSink">Message sink
        /// </param>
        /// <param name="dataCollectorInfo">Data collector info.
        /// </param>
        internal TestPlatformDataCollectionSink(IMessageSink messageSink, DataCollectorInformation dataCollectorInfo)
        {
            ValidateArg.NotNull(messageSink, "messageSink");
            ValidateArg.NotNull(dataCollectorInfo, "dataCollectorInfo");
            this.CollectorInfo = dataCollectorInfo;
            this.MessageSink = messageSink;
        }

        /// <summary>
        /// Raised when asynchronous file transfer is complete.
        /// </summary>
        public override event AsyncCompletedEventHandler SendFileCompleted;
#pragma warning disable 0067
        /// <summary>
        /// Raised when asynchronous stream transfer is complete.
        /// </summary>
        public override event AsyncCompletedEventHandler SendStreamCompleted;

        /// <summary>
        /// Gets or sets message sink to transfer collection message.
        /// </summary>
        private IMessageSink MessageSink
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets dataCollector with which this data sink is associated.
        /// </summary>
        private DataCollectorInformation CollectorInfo
        {
            get; set;
        }

#pragma warning restore 0067

        /// <summary>
        /// Sends a file asynchronously.
        /// </summary>
        /// <param name="fileTransferInformation">Information about the file being transferred.</param>
        public override void SendFileAsync(FileTransferInformation fileTransferInformation)
        {
            ValidateArg.NotNull(fileTransferInformation, "fileTransferInformation");

            var transferCompletedHandler = this.SendFileCompleted;

            Debug.Assert(System.IO.File.Exists(fileTransferInformation.Path), "DataCollector file '" + fileTransferInformation.Path + "' does not exist!");

            var headerMsg = new FileDataHeaderMessage(
                ObjectConversionHelper.ToDataCollectionConetxt(fileTransferInformation.Context),
                fileTransferInformation.ClientFileName,
                fileTransferInformation.Description,
                fileTransferInformation.DeleteFile,
                fileTransferInformation.UserToken,
                transferCompletedHandler,
                this.CollectorInfo.TypeUri,
                this.CollectorInfo.FriendlyName);

            this.MessageSink.SendMessage(headerMsg);
        }

        /// <summary>
        /// Sends a stream asynchronously.
        /// </summary>
        /// <param name="streamTransferInformation">Information about stream being transferred.</param>
        public override void SendStreamAsync(StreamTransferInformation streamTransferInformation)
        {
            throw new NotSupportedException(Resource.SupportedTransferTypeIsFileTransfer);
        }
    }
}
