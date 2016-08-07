// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1.Interfaces
{
    using System.Collections.Generic;

    using CollectorDataEntry = Microsoft.VisualStudio.TestPlatform.ObjectModel.AttachmentSet;
    using DataCollectionContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionContext;
    using SessionId = Microsoft.VisualStudio.TestTools.Common.SessionId;

    /// <summary>
    /// The DataCollectionFileManager interface.
    /// </summary>
    internal interface IDataCollectionFileManager
    {
        /// <summary>
        /// The configure session.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="outputDirectory">
        /// The output directory.
        /// </param>
        void ConfigureSession(SessionId id, string outputDirectory);

        /// <summary>
        /// The get data.
        /// </summary>
        /// <param name="collectionContext">
        /// The collection context.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        List<CollectorDataEntry> GetData(DataCollectionContext collectionContext);

        /// <summary>
        /// The close session.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        void CloseSession(SessionId id);

        /// <summary>
        /// The dispatch message.
        /// </summary>
        /// <param name="collectorDataMessage">
        /// The collector data message.
        /// </param>
        void DispatchMessage(DataCollectorDataMessage collectorDataMessage);

        /// <summary>
        /// The dispose.
        /// </summary>
        void Dispose();
    }
}