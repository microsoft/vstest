// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System.ComponentModel;
    using TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Wrapper for <see cref="Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection.DataCollectionSink"/>
    /// to make the collector testable.
    /// </summary>
    internal interface IDataCollectionSink
    {
        event AsyncCompletedEventHandler SendFileCompleted;

        void SendFileAsync(DataCollectionContext context, string path, bool deleteFile);

        void SendFileAsync(DataCollectionContext context, string path, string displayName, bool deleteFile);

        void SendFileAsync(FileTransferInformation fileInformation);
    }
}