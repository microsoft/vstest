// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The discovery result cache.
    /// </summary>
    internal class DiscoveryResultCache
    {
        #region private members
        
        /// <summary>
        /// Callback used when cache is full. 
        /// </summary>
        private OnReportTestCases onReportTestCases;

        /// <summary>
        /// Total tests discovered in this request
        /// </summary>
        private long totalDiscoveredTests;

        /// <summary>
        /// Max size of the test case buffer
        /// </summary>
        private long cacheSize;

        /// <summary>
        /// Timeout that triggers sending test cases regardless of cache size.
        /// </summary>
        private TimeSpan cacheTimeout;

        /// <summary>
        /// Last time test cases were sent.
        /// </summary>
        private DateTime lastUpdate;

        /// <summary>
        /// Test case buffer
        /// </summary>
        private List<TestCase> tests;

        /// <summary>
        /// Sync object 
        /// </summary>
        private object syncObject = new object();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryResultCache"/> class.
        /// </summary>
        /// <param name="cacheSize"> The cache size. </param>
        /// <param name="discoveredTestEventTimeout"> The discovered test event timeout. </param>
        /// <param name="onReportTestCases"> The on report test cases. </param>
        public DiscoveryResultCache(long cacheSize, TimeSpan discoveredTestEventTimeout, OnReportTestCases onReportTestCases)
        {
            Debug.Assert(cacheSize > 0, "Buffer size cannot be less than zero");
            Debug.Assert(onReportTestCases != null, "Callback which listens for cache size limit cannot be null.");
            Debug.Assert(discoveredTestEventTimeout > TimeSpan.MinValue, "The cache timeout must be greater than min value.");

            this.cacheSize = cacheSize;
            this.onReportTestCases = onReportTestCases;
            this.lastUpdate = DateTime.Now;
            this.cacheTimeout = discoveredTestEventTimeout;

            this.tests = new List<TestCase>();
            this.totalDiscoveredTests = 0;
        }
        
        /// <summary>
        /// Called when the cache is ready to report some discovered test cases.
        /// </summary>
        public delegate void OnReportTestCases(ICollection<TestCase> tests);
        
        /// <summary>
        /// Gets the tests present in the cache currently
        /// </summary>
        public IList<TestCase> Tests
        {
            get
            {
                // This needs to new list to avoid concurrency issues.
                return new List<TestCase>(this.tests);
            }
        }

        /// <summary>
        /// Gets the total discovered tests
        /// </summary>
        public long TotalDiscoveredTests
        {
            get
            {
                return this.totalDiscoveredTests;
            }
        }

        /// <summary>
        /// Adds a test to the cache.
        /// </summary>
        /// <param name="test"> The test. </param>
        public void AddTest(TestCase test)
        {
            Debug.Assert(null != test, "DiscoveryResultCache.AddTest called with no new test.");

            if (test == null)
            {
                EqtTrace.Warning("DiscoveryResultCache.AddTest: An attempt was made to add a 'null' test");
                return;
            }

            lock (this.syncObject)
            {
                this.tests.Add(test);
                this.totalDiscoveredTests++;

                // Send test cases when the specified cache size has been reached or 
                // after the specified cache timeout has been hit.
                var timeDelta = DateTime.Now - this.lastUpdate;
                if (this.tests.Count >= this.cacheSize || (timeDelta > this.cacheTimeout && this.tests.Count > 0))
                {
                    // Pass on the buffer to the listener and clear the old one
                    this.onReportTestCases(this.tests);
                    this.tests = new List<TestCase>();
                    this.lastUpdate = DateTime.Now;

                    EqtTrace.Verbose("DiscoveryResultCache.AddTest: Notified the onReportTestCases callback.");
                }
            }
        }
    }
}
