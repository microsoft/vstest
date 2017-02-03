// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces
{
    /// <summary>
    /// Factory interface for creating the datacollection manager.
    /// </summary>
    internal interface IDataCollectionManagerFactory
    {
        /// <summary>
        /// Creates instance of DataCollectionManager.
        /// </summary>
        /// <param name="messageSink">
        /// The message sink.
        /// </param>
        /// <returns>
        /// The <see cref="IDataCollectionManager"/>.
        /// </returns>
        IDataCollectionManager Create(IMessageSink messageSink);
    }
}
