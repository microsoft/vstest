// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    /// <summary>
    /// Factory interface for providing the TestCaseDataCollectionCommunication.
    /// </summary>
    internal interface IDataCollectionTestCaseEventManagerFactory
    {
        /// <summary>
        /// The get test case data collection request sender.
        /// </summary>
        /// <returns>
        /// The <see cref="IDataCollectionTestCaseEventSender"/>.
        /// </returns>
        IDataCollectionTestCaseEventSender GetTestCaseDataCollectionRequestSender();

        /// <summary>
        /// The get test case data collection request handler.
        /// </summary>
        /// <returns>
        /// The <see cref="IDataCollectionTestCaseEventHandler"/>.
        /// </returns>
        IDataCollectionTestCaseEventHandler GetTestCaseDataCollectionRequestHandler();
    }
}
