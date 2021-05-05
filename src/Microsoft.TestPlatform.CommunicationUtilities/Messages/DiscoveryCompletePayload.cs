// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// The discovery complete payload.
    /// </summary>
    public class DiscoveryCompletePayload
    {
        /// <summary>
        /// Gets or sets the total number of tests discovered.
        /// </summary>
        public long TotalTests { get; set; }

        /// <summary>
        /// Gets or sets the last chunk of discovered tests.
        /// </summary>
        public IEnumerable<TestCase> LastDiscoveredTests { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether discovery was aborted.
        /// </summary>
        public bool IsAborted { get; set; }

        /// <summary>
        /// Gets or sets the Metrics
        /// </summary>
        public IDictionary<string, object> Metrics { get; set; }

        // List of sources which were fully discovered
        public IList<string> FullyDiscoveredSources { get; set; } = new List<string>();

        // List of sources which were partially discovered (started discover tests, but then discovery aborted)
        public IList<string> PartiallyDiscoveredSources { get; set; } = new List<string>();

        // List of sources which were not discovered at all
        public IList<string> NotDiscoveredSources { get; set; } = new List<string>();
    }
}
