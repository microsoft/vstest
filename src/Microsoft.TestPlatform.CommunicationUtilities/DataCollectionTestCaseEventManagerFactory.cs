// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

    /// <summary>
    /// Factory for creating sender and handler for interaction with datacollector process and test exectuion process.
    /// </summary>
    internal class DataCollectionTestCaseEventManagerFactory : IDataCollectionTestCaseEventManagerFactory
    {
        /// <inheritdoc/>
        public IDataCollectionTestCaseEventSender GetTestCaseDataCollectionRequestSender()
        {
            throw new NotImplementedException();
        }
        
        /// <inheritdoc/>
        public IDataCollectionTestCaseEventHandler GetTestCaseDataCollectionRequestHandler()
        {
            return new DataCollectionTestCaseEventHandler();
        }
    }
}
