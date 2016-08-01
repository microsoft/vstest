// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System.Collections.Generic;

    /// <summary>
    /// The data collection parameters.
    /// </summary>
    public class DataCollectionParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionParameters"/> class.
        /// </summary>
        /// <param name="isDataCollectionStarted">
        /// The is data collection started.
        /// </param>
        /// <param name="areTestCaseLevelEventsRequired">
        /// The are test case level events required.
        /// </param>
        /// <param name="environmentVariables">
        /// The environment variables.
        /// </param>
        /// <param name="dataCollectionEventsPort">
        /// The data Collection Events Port.
        /// </param>
        public DataCollectionParameters(
            bool isDataCollectionStarted,
            bool areTestCaseLevelEventsRequired,
            IDictionary<string, string> environmentVariables,
            int dataCollectionEventsPort)
        {
            this.IsDataCollectionStarted = isDataCollectionStarted;
            this.AreTestCaseLevelEventsRequired = areTestCaseLevelEventsRequired;
            this.EnvironmentVariables = environmentVariables;
            this.DataCollectionEventsPort = dataCollectionEventsPort;
        }

        /// <summary>
        /// Gets a value indicating whether DataCollection is started 
        /// </summary>
        public bool IsDataCollectionStarted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether any of the enabled data collectors
        /// registered for test case level events
        /// </summary>
        public bool AreTestCaseLevelEventsRequired { get; private set; }

        /// <summary>
        /// Gets BeforeTestRunStart Call on the DataCollectors can yield/return a set of environment variables
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; private set; }

        /// <summary>
        /// Gets the data collection events port.
        /// </summary>
        public int DataCollectionEventsPort { get; private set; }

        /// <summary>
        /// The create default parameter instance.
        /// </summary>
        /// <returns>
        /// The <see cref="DataCollectionParameters"/>.
        /// </returns>
        public static DataCollectionParameters CreateDefaultParameterInstance()
        {
            return new DataCollectionParameters(isDataCollectionStarted: false, areTestCaseLevelEventsRequired: false, environmentVariables: null, dataCollectionEventsPort: 0);
        }
    }
}