// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Event arguments used on completion of discovery
    /// </summary>
    public class DiscoveryCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor for creating event args object
        /// </summary>
        /// <param name="totalTests">Total tests which got discovered</param>
        /// <param name="isAborted">Specifies if discovery has been aborted.</param>
        public DiscoveryCompleteEventArgs(long totalTests, bool isAborted)
        {
            // This event is always raised from the client side, while the total count of tests is maintained
            // only at the testhost end. In case of a discovery abort (various reasons including crash), it is
            // not possible to get a list of total tests from testhost. Hence we enforce a -1 count.
            Debug.Assert((isAborted ? -1 == totalTests : true), "If discovery request is aborted totalTest should be -1.");

            this.TotalCount = totalTests;
            this.IsAborted = isAborted;
        }

        /// <summary>
        ///   Indicates the total tests which got discovered in this request.
        /// </summary>
        public long TotalCount { get; private set; }

        /// <summary>
        /// Specifies if discovery has been aborted. If true TotalCount is also set to -1.
        /// </summary>
        public bool IsAborted { get; private set; }

        /// <summary>
        /// Metrics
        /// </summary>
        public IDictionary<string, object> Metrics { get; set; }
    }
}
