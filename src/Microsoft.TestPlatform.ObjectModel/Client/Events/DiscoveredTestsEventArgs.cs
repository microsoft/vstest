// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Event arguments used to notify the availability of new tests
    /// </summary>
    public partial class DiscoveredTestsEventArgs : EventArgs
    {
        public DiscoveredTestsEventArgs(IEnumerable<TestCase> discoveredTestCases)
        {
            DiscoveredTestCases = discoveredTestCases;
        }
        /// <summary>
        /// Tests discovered in this discovery request
        /// </summary>
        public IEnumerable<TestCase> DiscoveredTestCases { get; private set; }
    }
}
