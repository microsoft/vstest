// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;

    /// <summary>
    /// Factory for creating the datacollection manager.
    /// </summary>
    internal class DataCollectionManagerFactory : IDataCollectionManagerFactory
    {
        /// <inheritdoc />
        public IDataCollectionManager Create(IMessageSink messageSink)
        {
            return new DataCollectionManager(messageSink);
        }
    }
}
