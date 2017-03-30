// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.TestDoubles
{
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

    internal class TestableDataCollectionRequestHandler : DataCollectionRequestHandler
    {
        public TestableDataCollectionRequestHandler(ICommunicationManager communicationManager, IMessageSink messageSink, IDataCollectionManager dataCollectionManager, IDataCollectionTestCaseEventHandler dataCollectionTestCaseEventHandler)
            : base(communicationManager, messageSink, dataCollectionManager, dataCollectionTestCaseEventHandler)
        {
        }
    }
}
