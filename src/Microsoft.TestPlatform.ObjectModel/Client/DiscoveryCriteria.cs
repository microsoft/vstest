// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;

    /// <summary>
    /// Defines the discovery criterion.
    /// </summary>
    [DataContract]
    public class DiscoveryCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryCriteria"/> class.
        /// </summary>
        /// <remarks>This constructor doesn't perform any parameter validation, it is meant to be used for serialization."/></remarks>
        public DiscoveryCriteria()
        {
            // Parameterless constructor is used for Serialization.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources from which the tests should be discovered.
        /// </param>
        /// <param name="frequencyOfDiscoveredTestsEvent">
        /// Frequency of discovered test event. This is used for batching discovered tests.
        /// </param>
        /// <param name="testSettings">
        /// Test configuration provided by user.
        /// </param>
        public DiscoveryCriteria(
            IEnumerable<string> sources,
            long frequencyOfDiscoveredTestsEvent,
            string testSettings)
            : this(
                  sources,
                  frequencyOfDiscoveredTestsEvent,
                  TimeSpan.MaxValue,
                  testSettings)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources from which the tests should be discovered
        /// </param>
        /// <param name="frequencyOfDiscoveredTestsEvent">
        /// Frequency of discovered test event. This is used for batching discovered tests.
        /// </param>
        /// <param name="discoveredTestEventTimeout">
        /// Timeout that triggers the discovered test event regardless of cache size.
        /// </param>
        /// <param name="runSettings">
        /// Run Settings for the discovery.
        /// </param>
        public DiscoveryCriteria(
            IEnumerable<string> sources,
            long frequencyOfDiscoveredTestsEvent,
            TimeSpan discoveredTestEventTimeout,
            string runSettings)
            : this(
                sources,
                frequencyOfDiscoveredTestsEvent,
                discoveredTestEventTimeout,
                runSettings,
                null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources from which the tests should be discovered
        /// </param>
        /// <param name="frequencyOfDiscoveredTestsEvent">
        /// Frequency of discovered test event. This is used for batching discovered tests.
        /// </param>
        /// <param name="discoveredTestEventTimeout">
        /// Timeout that triggers the discovered test event regardless of cache size.
        /// </param>
        /// <param name="runSettings">
        /// Run Settings for the discovery.
        /// </param>
        /// <param name="testSessionInfo">The test session info object.</param>
        public DiscoveryCriteria(
            IEnumerable<string> sources,
            long frequencyOfDiscoveredTestsEvent,
            TimeSpan discoveredTestEventTimeout,
            string runSettings,
            TestSessionInfo testSessionInfo)
        {
            ValidateArg.NotNullOrEmpty(sources, "sources");
            if (frequencyOfDiscoveredTestsEvent <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frequencyOfDiscoveredTestsEvent),
                    Resources.NotificationFrequencyIsNotPositive);
            }

            if (discoveredTestEventTimeout <= TimeSpan.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(discoveredTestEventTimeout), Resources.NotificationTimeoutIsZero);
            }

            this.AdapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            this.AdapterSourceMap.Add(Constants.UnspecifiedAdapterPath, sources);
            this.FrequencyOfDiscoveredTestsEvent = frequencyOfDiscoveredTestsEvent;
            this.DiscoveredTestEventTimeout = discoveredTestEventTimeout;

            this.RunSettings = runSettings;
            this.TestSessionInfo = testSessionInfo;
        }

        /// <summary>
        /// Gets the test Containers (e.g. DLL/EXE/artifacts to scan)
        /// </summary>
        [IgnoreDataMember]
        public IEnumerable<string> Sources
        {
            get
            {
                IEnumerable<string> sources = new List<string>();
                return this.AdapterSourceMap.Values.Aggregate(sources, (current, enumerable) => current.Concat(enumerable));
            }
        }

        /// <summary>
        /// Gets the test Containers (e.g. .appx, .appxrecipie)
        /// </summary>
        [DataMember]
        public string Package { get; set; }

        /// <summary>
        /// Gets the test adapter and source map which would look like below:
        /// <code>
        /// { C:\temp\testAdapter1.dll : [ source1.dll, source2.dll ], C:\temp\testadapter2.dll : [ source3.dll, source2.dll ]
        /// </code>
        /// </summary>
        [DataMember]
        public Dictionary<string, IEnumerable<string>> AdapterSourceMap { get; private set; }

        /// <summary>
        /// Gets the frequency of discovered test event. 
        /// </summary>
        /// <remarks>
        /// Discovered test event will be raised after discovering these number of tests. 
        /// Note that this event is raised asynchronously and the underlying discovery process is not 
        /// paused during the listener invocation. So if the event handler, you try to query the 
        /// next set of tests, you may get more than 'FrequencyOfDiscoveredTestsEvent'.
        /// </remarks>        
        [DataMember]
        public long FrequencyOfDiscoveredTestsEvent { get; private set; }

        /// <summary>
        /// Gets the timeout that triggers the discovered test event regardless of cache size.
        /// </summary>
        [DataMember]
        public TimeSpan DiscoveredTestEventTimeout { get; private set; }

        /// <summary>
        /// Gets the test settings used for the discovery request. 
        /// </summary>
        [DataMember]
        public string RunSettings { get; private set; }

        /// <summary>
        /// Gets or sets the criteria for filtering test cases.
        /// </summary>
        [DataMember]
        public string TestCaseFilter { get; set; }

        /// <summary>
        /// Gets or sets the test session info object.
        /// </summary>
        public TestSessionInfo TestSessionInfo { get; set; }
    }
}
