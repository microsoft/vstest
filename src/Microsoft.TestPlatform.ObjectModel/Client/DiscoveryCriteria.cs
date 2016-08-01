// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines the discovery criterion
    /// </summary>
    public class DiscoveryCriteria
    {
        /// <summary>
        /// Criteria used for test discovery
        /// </summary>
        /// <param name="sources">Sources from which the tests should be discovered</param>
        /// <param name="frequencyOfDiscoveredTestsEvent">Frequency of discovered test event</param>
        /// <param name="runSettings">Run Settings for the discovery.</param>
        public DiscoveryCriteria(IEnumerable<string> sources, long frequencyOfDiscoveredTestsEvent, string testSettings)
            : this(sources, frequencyOfDiscoveredTestsEvent, TimeSpan.MaxValue, testSettings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryCriteria"/> class.
        /// </summary>
        /// <param name="sources"> The sources. </param>
        /// <param name="frequencyOfDiscoveredTestsEvent"> The frequency of discovered tests event. </param>
        /// <param name="discoveredTestEventTimeout"> The discovered test event timeout. </param>
        /// <param name="runSettings"> The run settings. </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        [JsonConstructor]
        public DiscoveryCriteria(Dictionary<string, IEnumerable<string>> adapterSourceMap, long frequencyOfDiscoveredTestsEvent, TimeSpan discoveredTestEventTimeout, string runSettings)
        {
            ValidateArg.NotNullOrEmpty(adapterSourceMap, "adapterSourceMap");
            if (frequencyOfDiscoveredTestsEvent <= 0) throw new ArgumentOutOfRangeException("frequencyOfDiscoveredTestsEvent", Resources.NotificationFrequencyIsNotPositive);
            if (discoveredTestEventTimeout <= TimeSpan.MinValue) throw new ArgumentOutOfRangeException("discoveredTestEventTimeout", Resources.NotificationTimeoutIsZero);

            this.AdapterSourceMap = adapterSourceMap;
            this.FrequencyOfDiscoveredTestsEvent = frequencyOfDiscoveredTestsEvent;
            this.DiscoveredTestEventTimeout = discoveredTestEventTimeout;

            this.RunSettings = runSettings;
        }

        /// <summary>
        /// Criteria used for test discovery
        /// </summary>
        /// <param name="sources">Sources from which the tests should be discovered</param>
        /// <param name="frequencyOfDiscoveredTestsEvent">Frequency of discovered test event</param>
        /// <param name="discoveredTestEventTimeout">Timeout that triggers the discovered test event regardless of cache size.</param>
        /// <param name="runSettings">Run Settings for the discovery.</param>
        public DiscoveryCriteria(IEnumerable<string> sources, long frequencyOfDiscoveredTestsEvent, TimeSpan discoveredTestEventTimeout, string runSettings)
        {
            ValidateArg.NotNullOrEmpty(sources, "sources");
            if (frequencyOfDiscoveredTestsEvent <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "frequencyOfDiscoveredTestsEvent",
                    Resources.NotificationFrequencyIsNotPositive);
            }

            if (discoveredTestEventTimeout <= TimeSpan.MinValue)
            {
                throw new ArgumentOutOfRangeException("discoveredTestEventTimeout", Resources.NotificationTimeoutIsZero);
            }

            this.AdapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            this.AdapterSourceMap.Add(Constants.UnspecifiedAdapterPath, sources);
            this.FrequencyOfDiscoveredTestsEvent = frequencyOfDiscoveredTestsEvent;
            this.DiscoveredTestEventTimeout = discoveredTestEventTimeout;

            this.RunSettings = runSettings;
        }

        /// <summary>
        /// Test Containers (e.g. DLL/EXE/artifacts to scan)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Sources
        {
            get
            {
                IEnumerable<string> sources = new List<string>();
                return this.AdapterSourceMap.Values.Aggregate(sources, (current, enumerable) => current.Concat(enumerable));
            }
        }
        
        /// <summary>
        /// The test adapter and source map which would look like below:
        /// { C:\temp\testAdapter1.dll : [ source1.dll, source2.dll ], C:\temp\testadapter2.dll : [ source3.dll, source2.dll ]
        /// </summary>
        public Dictionary<string, IEnumerable<string>> AdapterSourceMap
        {
            get; private set;
        }

        /// <summary>
        /// Defines the frequency of discovered test event. 
        /// </summary>
        /// <remarks>
        /// Discovered test event will be raised after discovering these number of tests. 
        /// Note that this event is raised asynchronously and the underlying discovery process is not 
        /// paused during the listener invocation. So if the event handler, you try to query the 
        /// next set of tests, you may get more than 'FrequencyOfDiscoveredTestsEvent'.
        /// </remarks>        
        public long FrequencyOfDiscoveredTestsEvent { get; private set; }

        /// <summary>
        /// Timeout that triggers the discovered test event regardless of cache size.
        /// </summary>
        public TimeSpan DiscoveredTestEventTimeout { get; private set; }

        /// <summary>
        /// Settings used for the discovery request. 
        /// </summary>
        public string RunSettings { get; private set; }
    }
}
