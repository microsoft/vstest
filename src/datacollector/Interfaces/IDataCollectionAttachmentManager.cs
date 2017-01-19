

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The DataCollectionAttachmentManager Interface.
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