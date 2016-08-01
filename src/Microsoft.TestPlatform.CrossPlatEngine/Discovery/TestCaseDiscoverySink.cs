// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// The test case discovery sink.
    /// </summary>
    internal class TestCaseDiscoverySink : ITestCaseDiscoverySink
    {
        private DiscoveryResultCache discoveryRequestCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDiscoverySink"/> class.
        /// </summary>
        /// <param name="discoveryRequestCache"> The discovery request cache. </param>
        internal TestCaseDiscoverySink(DiscoveryResultCache discoveryRequestCache)
        {
            this.discoveryRequestCache = discoveryRequestCache;
        }

        /// <summary>
        /// Sends the test case to the discovery cache.
        /// </summary>
        /// <param name="discoveredTest"> The discovered test. </param>
        public void SendTestCase(TestCase discoveredTest)
        {
            if (this.discoveryRequestCache != null)
            {
                this.discoveryRequestCache.AddTest(discoveredTest);
            }
        }
    }
}
