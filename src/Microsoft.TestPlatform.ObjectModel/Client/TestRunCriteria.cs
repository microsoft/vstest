// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;

    /// <summary>
    /// Defines the testRun criterion
    /// </summary>
    public class TestRunCriteria : BaseTestRunCriteria, ITestRunConfiguration
    {
        private string testCaseFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent)
            : this(sources, frequencyOfRunStatsChangeEvent, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether the execution process should be kept alive after the run is finished or not.
        /// </param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent, bool keepAlive)
            : this(sources, frequencyOfRunStatsChangeEvent, keepAlive, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether the execution process should be kept alive after the run is finished or not.
        /// </param>
        /// <param name="testSettings">
        /// Settings used for this run.
        /// </param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings)
            : this(sources, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, TimeSpan.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether the execution process should be kept alive after the run is finished or not.
        /// </param>
        /// <param name="testSettings">
        /// The test Settings.
        /// </param>
        /// <param name="runStatsChangeEventTimeout">
        /// Timeout that triggers sending results regardless of cache size.
        /// </param>
        public TestRunCriteria(IEnumerable<string> sources, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout)
            : this(sources, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// Create the TestRunCriteria for a test run
        /// </summary>
        /// <param name="sources">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="baseTestRunCriteria">
        /// The BaseTestRunCriteria
        /// </param>
        public TestRunCriteria(IEnumerable<string> sources, BaseTestRunCriteria baseTestRunCriteria)
            : base(baseTestRunCriteria)
        {
            var testSources = sources as IList<string> ?? sources.ToArray();
            ValidateArg.NotNullOrEmpty(testSources, "sources");

            this.AdapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            this.AdapterSourceMap.Add(Constants.UnspecifiedAdapterPath, testSources);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="sources">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether the execution process should be kept alive after the run is finished or not.
        /// </param>
        /// <param name="testSettings">
        /// Settings used for this run.
        /// </param>
        /// <param name="runStatsChangeEventTimeout">
        /// Timeout that triggers sending results regardless of cache size.
        /// </param>
        /// <param name="testHostLauncher">
        /// Test host launcher. If null then default will be used.
        /// </param>
        public TestRunCriteria(
            IEnumerable<string> sources,
            long frequencyOfRunStatsChangeEvent,
            bool keepAlive,
            string testSettings,
            TimeSpan runStatsChangeEventTimeout,
            ITestHostLauncher testHostLauncher)
            : base(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, testHostLauncher)
        {
            var testSources = sources as IList<string> ?? sources.ToList();
            ValidateArg.NotNullOrEmpty(testSources, "sources");

            this.AdapterSourceMap = new Dictionary<string, IEnumerable<string>>();
            this.AdapterSourceMap.Add(Constants.UnspecifiedAdapterPath, testSources);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="adapterSourceMap">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether the execution process should be kept alive after the run is finished or not.
        /// </param>
        /// <param name="testSettings">
        /// Settings used for this run.
        /// </param>
        /// <param name="runStatsChangeEventTimeout">
        /// Timeout that triggers sending results regardless of cache size.
        /// </param>
        /// <param name="testHostLauncher">
        /// Test host launcher. If null then default will be used.
        /// </param>
        public TestRunCriteria(
            Dictionary<string, IEnumerable<string>> adapterSourceMap,
            long frequencyOfRunStatsChangeEvent,
            bool keepAlive,
            string testSettings,
            TimeSpan runStatsChangeEventTimeout,
            ITestHostLauncher testHostLauncher)
            : base(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, testHostLauncher)
        {
            ValidateArg.NotNullOrEmpty(adapterSourceMap, "adapterSourceMap");

            this.AdapterSourceMap = adapterSourceMap;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="tests">
        /// Tests which should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent)
            : this(tests, frequencyOfRunStatsChangeEvent, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="tests">
        /// Tests which should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether or not to keep the test executor process alive after run completion
        /// </param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive)
            : this(tests, frequencyOfRunStatsChangeEvent, keepAlive, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="tests">
        /// Tests which should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether or not to keep the test executor process alive after run completion
        /// </param>
        /// <param name="testSettings">
        /// Settings used for this run.
        /// </param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings)
            : this(tests, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, TimeSpan.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="tests">
        /// Tests which should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether or not to keep the test executor process alive after run completion
        /// </param>
        /// <param name="testSettings">
        /// Settings used for this run.
        /// </param>
        /// <param name="runStatsChangeEventTimeout">
        /// Timeout that triggers sending results regardless of cache size.
        /// </param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout)
            : this(tests, frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="tests">
        /// Tests which should be executed
        /// </param>
        /// <param name="baseTestRunCriteria">
        /// The BaseTestRunCriteria
        /// </param>
        public TestRunCriteria(IEnumerable<TestCase> tests, BaseTestRunCriteria baseTestRunCriteria)
            : base(baseTestRunCriteria)
        {
            var testCases = tests as IList<TestCase> ?? tests.ToList();
            ValidateArg.NotNullOrEmpty(testCases, "tests");

            this.Tests = testCases;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCriteria"/> class.
        /// </summary>
        /// <param name="tests">
        /// Sources which contains tests that should be executed
        /// </param>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of run stats event
        /// </param>
        /// <param name="keepAlive">
        /// Whether or not to keep the test executor process alive after run completion
        /// </param>
        /// <param name="testSettings">
        /// Settings used for this run.
        /// </param>
        /// <param name="runStatsChangeEventTimeout">
        /// Timeout that triggers sending results regardless of cache size.
        /// </param>
        /// <param name="testHostLauncher">
        /// Test host launcher. If null then default will be used.
        /// </param>
        public TestRunCriteria(IEnumerable<TestCase> tests, long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout, ITestHostLauncher testHostLauncher)
            : base(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, testHostLauncher)
        {
            var testCases = tests as IList<TestCase> ?? tests.ToList();
            ValidateArg.NotNullOrEmpty(testCases, "tests");

            this.Tests = testCases;
        }

        /// <summary>
        /// Gets the test Containers (e.g. DLL/EXE/artifacts to scan).
        /// </summary>
        [IgnoreDataMember]
        public IEnumerable<string> Sources
        {
            get
            {
                IEnumerable<string> sources = new List<string>();
                return this.AdapterSourceMap?.Values.Aggregate(sources, (current, enumerable) => current.Concat(enumerable));
            }
        }

        /// <summary>
        /// Gets the test adapter and source map which would look like below:
        /// <code>
        /// { C:\temp\testAdapter1.dll : [ source1.dll, source2.dll ], C:\temp\testadapter2.dll : [ source3.dll, source2.dll ]
        /// </code>
        /// </summary>
        [DataMember]
        public Dictionary<string, IEnumerable<string>> AdapterSourceMap { get; private set; }

        /// <summary>
        /// Gets the tests that need to executed in this test run.
        /// This will be null if test run is created with specific test containers
        /// </summary>
        [DataMember]
        public IEnumerable<TestCase> Tests { get; private set; }

        /// <summary>
        /// Gets or sets the criteria for filtering test cases.
        /// </summary>
        /// <remarks>This is only for with sources.</remarks>
        [DataMember]
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
        /// Gets a value indicating whether run criteria is based on specific tests.
        /// </summary>
        public bool HasSpecificTests
        {
            get { return this.Tests != null; }
        }

        /// <summary>
        /// Gets a value indicating whether run criteria is based on specific sources.
        /// </summary>
        [DataMember]
        public bool HasSpecificSources
        {
            get { return this.Sources != null; }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "TestRunCriteria:"));
            sb.AppendLine(string.Format(
                    CultureInfo.CurrentCulture,
                    "   KeepAlive={0},FrequencyOfRunStatsChangeEvent={1},RunStatsChangeEventTimeout={2},TestCaseFilter={3},TestExecutorLauncher={4}",
                    this.KeepAlive,
                    this.FrequencyOfRunStatsChangeEvent,
                    this.RunStatsChangeEventTimeout,
                    this.TestCaseFilter,
                    this.TestHostLauncher));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "   Settingsxml={0}", this.TestRunSettings));

            return sb.ToString();
        }

        protected bool Equals(TestRunCriteria other)
        {
            return base.Equals(other)
                && string.Equals(this.testCaseFilter, other.testCaseFilter);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj))
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((TestRunCriteria)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.testCaseFilter != null ? this.testCaseFilter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.AdapterSourceMap != null ? this.AdapterSourceMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.Tests != null ? this.Tests.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Defines the base testRun criterion
    /// </summary>
    public class BaseTestRunCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTestRunCriteria"/> class.
        /// </summary>
        /// <param name="runCriteria">
        /// Run criteria to clone.
        /// </param>
        public BaseTestRunCriteria(BaseTestRunCriteria runCriteria)
        {
            ValidateArg.NotNull(runCriteria, "runCriteria");

            this.FrequencyOfRunStatsChangeEvent = runCriteria.FrequencyOfRunStatsChangeEvent;
            this.KeepAlive = runCriteria.KeepAlive;
            this.TestRunSettings = runCriteria.TestRunSettings;
            this.RunStatsChangeEventTimeout = runCriteria.RunStatsChangeEventTimeout;
            this.TestHostLauncher = runCriteria.TestHostLauncher;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTestRunCriteria"/> class.
        /// </summary>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of <c>TestRunChangedEvent</c>.
        /// </param>
        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent)
            : this(frequencyOfRunStatsChangeEvent, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTestRunCriteria"/> class.
        /// </summary>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of <c>TestRunChangedEvent</c>.
        /// </param>
        /// <param name="keepAlive">
        /// Specify if the test host process should be stay alive after run.
        /// </param>
        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive)
            : this(frequencyOfRunStatsChangeEvent, keepAlive, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTestRunCriteria"/> class.
        /// </summary>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of <c>TestRunChangedEvent</c>.
        /// </param>
        /// <param name="keepAlive">
        /// Specify if the test host process should be stay alive after run.
        /// </param>
        /// <param name="testSettings">
        /// Test run settings.
        /// </param>
        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings)
            : this(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, TimeSpan.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTestRunCriteria"/> class.
        /// </summary>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of <c>TestRunChangedEvent</c>.
        /// </param>
        /// <param name="keepAlive">
        /// Specify if the test host process should be stay alive after run.
        /// </param>
        /// <param name="testSettings">
        /// Test run settings.
        /// </param>
        /// <param name="runStatsChangeEventTimeout">
        /// Timeout for a <c>TestRunChangedEvent</c>.
        /// </param>
        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout)
            : this(frequencyOfRunStatsChangeEvent, keepAlive, testSettings, runStatsChangeEventTimeout, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTestRunCriteria"/> class.
        /// </summary>
        /// <param name="frequencyOfRunStatsChangeEvent">
        /// Frequency of <c>TestRunChangedEvent</c>.
        /// </param>
        /// <param name="keepAlive">
        /// Specify if the test host process should be stay alive after run.
        /// </param>
        /// <param name="testSettings">
        /// Test run settings.
        /// </param>
        /// <param name="runStatsChangeEventTimeout">
        /// Timeout for a <c>TestRunChangedEvent</c>.
        /// </param>
        /// <param name="testHostLauncher">
        /// Test host launcher.
        /// </param>
        public BaseTestRunCriteria(long frequencyOfRunStatsChangeEvent, bool keepAlive, string testSettings, TimeSpan runStatsChangeEventTimeout, ITestHostLauncher testHostLauncher)
        {
            if (frequencyOfRunStatsChangeEvent <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frequencyOfRunStatsChangeEvent), Resources.NotificationFrequencyIsNotPositive);
            }

            if (runStatsChangeEventTimeout <= TimeSpan.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(runStatsChangeEventTimeout), Resources.NotificationTimeoutIsZero);
            }

            this.FrequencyOfRunStatsChangeEvent = frequencyOfRunStatsChangeEvent;
            this.KeepAlive = keepAlive;
            this.TestRunSettings = testSettings;
            this.RunStatsChangeEventTimeout = runStatsChangeEventTimeout;
            this.TestHostLauncher = testHostLauncher;
        }

        /// <summary>
        /// Gets a value indicating whether the test executor process should remain alive after run completion.
        /// </summary>
        [DataMember]
        public bool KeepAlive { get; private set; }

        /// <summary>
        /// Gets the settings used for this run.
        /// </summary>
        [DataMember]
        public string TestRunSettings { get; private set; }

        /// <summary>
        /// Gets the custom launcher for test executor.
        /// </summary>
        [DataMember]
        public ITestHostLauncher TestHostLauncher { get; private set; }

        /// <summary>
        /// Gets the frequency of run stats test event.
        /// </summary>
        /// <remarks>
        /// Run stats change event will be raised after completion of these number of tests.
        /// Note that this event is raised asynchronously and the underlying execution process is not
        /// paused during the listener invocation. So if the event handler, you try to query the
        /// next set of results, you may get more than 'FrequencyOfRunStatsChangeEvent'.
        /// </remarks>
        [DataMember]
        public long FrequencyOfRunStatsChangeEvent { get; private set; }

        /// <summary>
        /// Gets the timeout that triggers sending results regardless of cache size.
        /// </summary>
        [DataMember]
        public TimeSpan RunStatsChangeEventTimeout { get; private set; }

        protected bool Equals(BaseTestRunCriteria other)
        {
            return this.KeepAlive == other.KeepAlive
                && string.Equals(this.TestRunSettings, other.TestRunSettings)
                && this.FrequencyOfRunStatsChangeEvent == other.FrequencyOfRunStatsChangeEvent
                && this.RunStatsChangeEventTimeout.Equals(other.RunStatsChangeEventTimeout);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj))
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((BaseTestRunCriteria)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.KeepAlive.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.TestRunSettings != null ? this.TestRunSettings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.FrequencyOfRunStatsChangeEvent.GetHashCode();
                hashCode = (hashCode * 397) ^ this.RunStatsChangeEventTimeout.GetHashCode();
                return hashCode;
            }
        }
    }
}
