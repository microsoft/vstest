// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Event arguments used when test discovery starts
    /// </summary>
    public class DiscoveryStartEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor for creating event args object
        /// </summary>
        /// <param name="discoveryCriteria"> Discovery criteria to be used for test discovery. </param>
        /// <param name="filterExpression"> Test case filter expression. </param>
        public DiscoveryStartEventArgs(DiscoveryCriteria discoveryCriteria, ITestCaseFilterExpression filterExpression)
        {
            DiscoveryCriteria = discoveryCriteria;
            FilterExpression = filterExpression;
        }

        /// <summary>
        /// Discovery criteria to be used for test discovery
        /// </summary>
        public DiscoveryCriteria DiscoveryCriteria { get; private set; }

        /// <summary>
        /// Test case filter expression
        /// </summary>
        public ITestCaseFilterExpression FilterExpression { get; private set; }
    }
}
