// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    /// <summary>
    /// ParallelDiscoveryDataAggregator aggregates discovery data from parallel discovery managers
    /// </summary>
    internal class ParallelDiscoveryDataAggregator
    {
        #region PrivateFields
                
        private object dataUpdateSyncObject = new object();

        #endregion

        public ParallelDiscoveryDataAggregator()
        {
            IsAborted = false;
            TotalTests = 0;
        }

        #region Public Properties

        /// <summary>
        /// Set to true if any of the request is aborted
        /// </summary>
        public bool IsAborted { get; private set; }

        /// <summary>
        /// Aggregate total test count
        /// </summary>
        public long TotalTests { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Aggregate discovery data 
        /// Must be thread-safe as this is expected to be called by parallel managers
        /// </summary>
        public void Aggregate(long totalTests, bool isAborted)
        {
            lock (dataUpdateSyncObject)
            {
                this.IsAborted = this.IsAborted || isAborted;

                if (!isAborted)
                {
                    // Do not aggregate tests count if test discovery is aborted. It is mandated by
                    // platform that tests count is negative for discovery abort event.
                    // See `DiscoveryCompleteEventArgs`.
                    this.TotalTests = this.TotalTests + totalTests;
                }
            }
        }

        #endregion
    }
}
