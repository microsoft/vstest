// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// Stores information about test execution context.
    /// </summary>
    [DataContract]
    public class TestExecutionContext
    {
        #region Constructors

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event.</param>
        /// <param name="runStatsChangeEventTimeout">Timeout that triggers sending results regardless of cache size.</param>
        /// <param name="inIsolation">Whether execution is out of process</param>
        /// <param name="keepAlive">Whether executor process should be kept running after test run completion</param>
        /// <param name="isDataCollectionEnabled">Whether data collection is enabled or not.</param>
        /// <param name="areTestCaseLevelEventsRequired">Indicates whether test case level events are required.</param>
        /// <param name="hasTestRun">True if ExecutionContext is associated with Test run, false otherwise.</param>
        /// <param name="testCaseFilter">Filter criteria string for filtering tests.</param>
        public TestExecutionContext(
            long frequencyOfRunStatsChangeEvent,
                                    TimeSpan runStatsChangeEventTimeout,
                                    bool inIsolation,
                                    bool keepAlive,
                                    bool isDataCollectionEnabled,
                                    bool areTestCaseLevelEventsRequired,
                                    bool hasTestRun,
                                    bool isDebug,
                                    string testCaseFilter)
        {
            this.FrequencyOfRunStatsChangeEvent = frequencyOfRunStatsChangeEvent;
            this.RunStatsChangeEventTimeout = runStatsChangeEventTimeout;
            this.InIsolation = inIsolation;
            this.KeepAlive = keepAlive;
            this.IsDataCollectionEnabled = isDataCollectionEnabled;
            this.AreTestCaseLevelEventsRequired = areTestCaseLevelEventsRequired;
            
            this.IsDebug = isDebug;

            this.HasTestRun = hasTestRun;
            this.TestCaseFilter = testCaseFilter;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event.</param>
        /// <param name="runStatsChangeEventTimeout">Timeout that triggers sending results regardless of cache size.</param>
        /// <param name="inIsolation">Whether execution is out of process</param>
        /// <param name="keepAlive">Whether executor process should be kept running after test run completion</param>
        /// <param name="areTestCaseLevelEventsRequired">Indicates whether test case level events are required.</param>
        /// <param name="isDebug"> Indicates whether the tests are being debugged. </param>
        /// <param name="testCaseFilter">Filter criteria string for filtering tests.</param>
        /// <remarks>This constructor is needed to re-create an instance on deserialization on the test host side.</remarks>
        [JsonConstructor]
        public TestExecutionContext(
            long frequencyOfRunStatsChangeEvent,
                                    TimeSpan runStatsChangeEventTimeout,
                                    bool inIsolation,
                                    bool keepAlive,
                                    bool areTestCaseLevelEventsRequired,
                                    bool isDebug,
                                    string testCaseFilter)
        {
            this.FrequencyOfRunStatsChangeEvent = frequencyOfRunStatsChangeEvent;
            this.RunStatsChangeEventTimeout = runStatsChangeEventTimeout;
            this.InIsolation = inIsolation;
            this.KeepAlive = keepAlive;
            this.AreTestCaseLevelEventsRequired = areTestCaseLevelEventsRequired;

            this.IsDebug = isDebug;
            
            this.TestCaseFilter = testCaseFilter;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets frequency of run stats event.
        /// </summary>
        [DataMember]
        public long FrequencyOfRunStatsChangeEvent
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the timeout that triggers sending results regardless of cache size.
        /// </summary>
        [DataMember]
        public TimeSpan RunStatsChangeEventTimeout
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether execution is out of process.
        /// </summary>
        [DataMember]
        public bool InIsolation
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether executor process should be kept running after test run completion.
        /// </summary>
        [DataMember]
        public bool KeepAlive
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether test case level events need to be sent or not
        /// </summary>
        [DataMember]
        public bool AreTestCaseLevelEventsRequired
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether execution is in debug mode.
        /// </summary>
        [DataMember]
        public bool IsDebug
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the filter criteria for run with sources to filter test cases.
        /// </summary>
        [DataMember]
        public string TestCaseFilter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether data collection is enabled or not.
        /// </summary>
        /// <remarks>This does not need to be serialized over to the test host process.</remarks>
        public bool IsDataCollectionEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether execution context is associated with a test run.
        /// </summary>
        public bool HasTestRun
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a configuration associated with this run. 
        /// </summary>
        /// <remarks>It is not serialized over wcf as the information is available in the runsettings</remarks>
        public RunConfiguration TestRunConfiguration
        {
            get;
            set;
        }


        #endregion
    }
}
