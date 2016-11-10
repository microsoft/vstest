// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// The test run stats payload.
    /// </summary>
    public class TestRunStatsPayload
    {
        /// <summary>
        /// Gets or sets the test run changed event args.
        /// </summary>
        public TestRunChangedEventArgs TestRunChangedArgs { get; set; }

        /// <summary>
        /// Gets or sets the in progress test cases.
        /// </summary>
        public IEnumerable<TestCase> InProgressTestCases { get; set; }
    }
}
