// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System.ComponentModel;
    using TestPlatform.ObjectModel.DataCollection;

    internal sealed class DataCollectionSinkWrapper : IDataCollectionSink
    {
        private readonly DataCollectionSink wrapped;

        public DataCollectionSinkWrapper(DataCollectionSink wrapped)
        {
            this.wrapped = wrapped;
        }

        #region IDataCollectionSink Members

        event AsyncCompletedEventHandler IDataCollectionSink.SendFileCompleted
        {
            add { this.wrapped.SendFileCompleted += value; }
            remove { this.wrapped.SendFileCompleted -= value; }
        }

        void IDataCollectionSink.SendFileAsync(DataCollectionContext context, string path, bool deleteFile)
        {
            this.wrapped.SendFileAsync(context, path, deleteFile);
        }

        void IDataCollectionSink.SendFileAsync(
            DataCollectionContext context,
            string path,
            string description,
            bool deleteFile)
        {
            this.wrapped.SendFileAsync(context, path, description, deleteFile);
        }

        void IDataCollectionSink.SendFileAsync(FileTransferInformation fileInformation)
        {
            this.wrapped.SendFileAsync(fileInformation);
        }

        #endregion
    }
}