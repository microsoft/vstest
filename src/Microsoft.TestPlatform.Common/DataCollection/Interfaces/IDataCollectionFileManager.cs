// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The DataCollectionFileManager interface.
    /// </summary>
    internal interface IDataCollectionFileManager : IDisposable
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
        /// <param name="sessionId">
        /// The session Id.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        List<AttachmentSet> GetData(DataCollectionContext sessionId);

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
    }
}