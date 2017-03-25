// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.TestDoubles
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

    internal class TestableDataCollectionTestCaseEventSender : DataCollectionTestCaseEventSender
    {
        public TestableDataCollectionTestCaseEventSender(ICommunicationManager communicationManager)
            : base(communicationManager)
        {
        }
    }
}
