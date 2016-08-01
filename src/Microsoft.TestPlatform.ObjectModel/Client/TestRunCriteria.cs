// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines the testRun criterion
    /// </summary>
    public class TestRunCriteria : BaseTestRunCriteria, ITestRunConfiguration
    {
        #region Constructors that take list of source strings

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>          
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent)
            :this(sources, frequencyOfRunStatsChangeEvent, true)
        {
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        /// <param name="keepAlive">Whether the execution process should be kept alive after the run is finished or not.</param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent, bool keepAlive)
            : this(sources, frequencyOfRunStatsChangeEvent, keepAlive, string.Empty)
        {
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        /// <param name="keepAlive">Whether the execution process should be kept alive after the run is finished or not.</param>
        /// <param name="testSettings">Settings used for this run.</param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings)
            : this(sources, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, TimeSpan.MaxValue)
        {
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        /// <param name="testRunSettings">List of TestRunSettings for all providers as applicable for current run.</param>
        /// <param name="keepAlive">Whether the execution process should be kept alive after the run is finished or not.</param>
        /// <param name="runStatsChangeEventTimeout">Timeout that triggers sending results regardless of cache size.</param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout)
            : this(sources, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, null)
        {
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>        
        /// <param name="baseTestRunCriteria">The BaseTestRunCriteria</param>
        public TestRunCriteria(IEnumerable<string> sources, BaseTestRunCriteria baseTestRunCriteria)
            : base(baseTestRunCriteria)
        {
            ValidateArg.NotNullOrEmpty(sources, "sources");

            this.AdapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            this.AdapterSourceMap.Add(Constants.UnspecifiedAdapterPath, sources);
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
		/// <param name="testSettings">Settings used for this run.</param>
        /// <param name="keepAlive">Whether the execution process should be kept alive after the run is finished or not.</param>
        /// <param name="runStatsChangeEventTimeout">Timeout that triggers sending results regardless of cache size.</param>
        /// <param name="testHostLauncher">Test host launcher. If null then default will be used.</param>
        public TestRunCriteria(IEnumerable<string> sources, 
               long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, 
               TimeSpan runStatsChangeEventTimeout, ITestHostLauncher testHostLauncher)
            : base (frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, testHostLauncher)
        {
            ValidateArg.NotNullOrEmpty(sources, "sources");

            this.AdapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            this.AdapterSourceMap.Add(Constants.UnspecifiedAdapterPath, sources);
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
		/// <param name="testSettings">Settings used for this run.</param>
        /// <param name="keepAlive">Whether the execution process should be kept alive after the run is finished or not.</param>
        /// <param name="runStatsChangeEventTimeout">Timeout that triggers sending results regardless of cache size.</param>
        /// <param name="testHostLauncher">Test host launcher. If null then default will be used.</param>
        public TestRunCriteria(Dictionary<string, IEnumerable<string>> adapterSourceMap, long frequencyOfRunStatsChangeEvent, bool keepAlive, 
            string testSettings, TimeSpan runStatsChangeEventTimeout, ITestHostLauncher testHostLauncher)
            : base(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, testHostLauncher)
        {
            ValidateArg.NotNullOrEmpty(adapterSourceMap, "adapterSourceMap");

            this.AdapterSourceMap = adapterSourceMap;
        }

        #endregion

        #region Constructors that take list of test cases

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="tests">Tests which should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent)
            :this(tests, frequencyOfRunStatsChangeEvent, false)
        {
        }
        
        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="tests">Tests which should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        /// <param name="keepAlive">Whether or not to keep the test executor process alive after run completion</param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive)
            : this(tests, frequencyOfRunStatsChangeEvent, keepAlive, string.Empty)
        {
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="tests">Tests which should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        /// <param name="keepAlive">Whether or not to keep the test executor process alive after run completion</param>
        /// <param name="testSettings">Settings used for this run.</param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings)
            : this(tests, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, TimeSpan.MaxValue)
        {
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="tests">Tests which should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        /// <param name="keepAlive">Whether or not to keep the test executor process alive after run completion</param>
        /// <param name="testSettings">Settings used for this run.</param>
        /// <param name="runStatsChangeEventTimeout">Timeout that triggers sending results regardless of cache size.</param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout)
            : this(tests, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, null)
        {
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="tests">Tests which should be executed</param>        
        /// <param name="baseTestRunCriteria">The BaseTestRunCriteria</param>
        public TestRunCriteria(IEnumerable<TestCase> tests, BaseTestRunCriteria baseTestRunCriteria)
            : base(baseTestRunCriteria)
        {
            ValidateArg.NotNullOrEmpty(tests, "tests");
            this.Tests = tests;
        }

        /// <summary>
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">Sources which contains tests that should be executed</param>        
        /// <param name="frequencyOfRunStatsChangeEvent">Frequency of run stats event</param>
        /// <param name="keepAlive">Whether or not to keep the test executor process alive after run completion</param>
        /// <param name="testSettings">Settings used for this run.</param>
        /// <param name="runStatsChangeEventTimeout">Timeout that triggers sending results regardless of cache size.</param>
        /// <param name="testHostLauncher">Test host launcher. If null then default will be used.</param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout, ITestHostLauncher testHostLauncher)
            : base(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, testHostLauncher)
        {
            ValidateArg.NotNullOrEmpty(tests, "tests");

            this.Tests = tests;
        }

        #endregion
        

        /// <summary>
        /// Test Containers (e.g. DLL/EXE/artifacts to scan)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Sources
        {
            get
            {
                IEnumerable<string> sources = new List<string>();
                return this.AdapterSourceMap?.Values.Aggregate(sources, (current, enumerable) => current.Concat(enumerable));
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
        /// Tests that need to executed in this test run. 
        /// 
        /// This will be null if test run is created with specific test containers
        /// </summary>
        public IEnumerable<TestCase> Tests { get; private set; }

        private string testCaseFilter;

        /// <summary>
        /// Criteria for filtering test cases. This is only for with sources.
        /// </summary>
        public string TestCaseFilter
        {
            get
            {
                return this.testCaseFilter;
            }

            set
            {
                if (value != null && !this.HasSpecificSources)
                {
                    throw new InvalidOperationException(Resources.NoTestCaseFilterForSpecificTests);
                }

                this.testCaseFilter = value;
            }
        }

        /// <summary>
        /// Returns whether run criteria is based on specific tests 
        /// </summary>
        public bool HasSpecificTests
        {
            get { return Tests != null; }
        }

        /// <summary>
        /// Returns whether run criteria is based on specific sources 
        /// </summary>
        public bool HasSpecificSources
        {
            get { return Sources != null; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "TestRunCriteria:"));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "   KeepAlive={0},FrequencyOfRunStatsChangeEvent={1},RunStatsChangeEventTimeout={2},TestCaseFilter={3},TestExecutorLauncher={4}", 
                                            KeepAlive, FrequencyOfRunStatsChangeEvent, RunStatsChangeEventTimeout, TestCaseFilter, TestHostLauncher));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "   Settingsxml={0}", TestRunSettings));

            return sb.ToString();
        }
    }


    /// <summary>
    /// Defines the base testRun criterion
    /// </summary>
    public class BaseTestRunCriteria
    {
        public BaseTestRunCriteria(BaseTestRunCriteria runCriteria)
        {
            ValidateArg.NotNull(runCriteria, "runCriteria");

            this.FrequencyOfRunStatsChangeEvent = runCriteria.FrequencyOfRunStatsChangeEvent;
            this.KeepAlive = runCriteria.KeepAlive;
            this.TestRunSettings = runCriteria.TestRunSettings;
            this.RunStatsChangeEventTimeout = runCriteria.RunStatsChangeEventTimeout;
            this.TestHostLauncher = runCriteria.TestHostLauncher;
        }

        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent)
            : this(frequencyOfRunStatsChangeEvent, true)
        {
        }

        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive)
            : this(frequencyOfRunStatsChangeEvent, keepAlive, string.Empty)
        {
        }

        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings)
            : this(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, TimeSpan.MaxValue)
        {
        }

        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout)
            : this(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, null)
        {
        }


        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout, ITestHostLauncher testHostLauncher)
        {
            if (frequencyOfRunStatsChangeEvent <= 0) throw new ArgumentOutOfRangeException("frequencyOfRunStatsChangeEvent", Resources.NotificationFrequencyIsNotPositive);
            if (runStatsChangeEventTimeout <= TimeSpan.MinValue) throw new ArgumentOutOfRangeException("runStatsChangeEventTimeout", Resources.NotificationTimeoutIsZero);

            this.FrequencyOfRunStatsChangeEvent = frequencyOfRunStatsChangeEvent;
            this.KeepAlive = keepAlive;
            this.TestRunSettings = testSettings;
            this.RunStatsChangeEventTimeout = runStatsChangeEventTimeout;
            this.TestHostLauncher = testHostLauncher;
        }

        /// <summary>
        /// Whether or not to keep the test executor process alive after run completion.
        /// </summary>
        public bool KeepAlive { get; private set; }

        /// <summary>
        /// Settings used for this run.
        /// </summary>
        public string TestRunSettings { get; private set; }

        /// <summary>
        /// Custom launcher for test executor.
        /// </summary>
        public ITestHostLauncher TestHostLauncher { get; private set; }

        /// <summary>
        /// Defines the frequency of run stats test event. 
        /// </summary>
        /// <remarks>
        /// Run stats change event will be raised after completion of these number of tests. 
        /// Note that this event is raised asynchronously and the underlying execution process is not 
        /// paused during the listener invocation. So if the event handler, you try to query the 
        /// next set of results, you may get more than 'FrequencyOfRunStatsChangeEvent'.
        /// </remarks>        
        public long FrequencyOfRunStatsChangeEvent { get; private set; }

        /// <summary>
        /// Timeout that triggers sending results regardless of cache size.
        /// </summary>
        public TimeSpan RunStatsChangeEventTimeout { get; private set; }
    }

}
