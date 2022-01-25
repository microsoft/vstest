// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using ObjectModel.Client;

    /// <summary>
    /// Maintains a cache of last 'n' test results and maintains stats for the complete run.
    /// </summary>
    internal class TestRunCache : ITestRunCache
    {
        #region Private Members

        /// <summary>
        /// Specifies whether the object is disposed or not.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Test run stats
        /// </summary>
        private readonly Dictionary<TestOutcome, long> runStats;

        /// <summary>
        /// Total tests which have currently executed
        /// </summary>
        private long totalExecutedTests;

        /// <summary>
        /// Callback used when cache is ready to report some test results/case.
        /// </summary>
        private readonly OnCacheHit onCacheHit;

        /// <summary>
        /// Max size of the test result buffer
        /// </summary>
        private readonly long cacheSize;

        /// <summary>
        /// Timeout that triggers sending results regardless of cache size.
        /// </summary>
        private TimeSpan cacheTimeout;

        /// <summary>
        /// Timer for cache
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Last time results were sent.
        /// </summary>
        private DateTime lastUpdate;

        /// <summary>
        /// The test case currently in progress.
        /// </summary>
        private ICollection<TestCase> inProgressTests;

        /// <summary>
        /// Test results buffer
        /// </summary>
        private ICollection<TestResult> testResults;

        /// <summary>
        /// Sync object
        /// </summary>
        private readonly object syncObject;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunCache"/> class.
        /// </summary>
        /// <param name="cacheSize"> The cache size. </param>
        /// <param name="cacheTimeout"> The cache timeout. </param>
        /// <param name="onCacheHit"> The on cache hit. </param>
        internal TestRunCache(long cacheSize, TimeSpan cacheTimeout, OnCacheHit onCacheHit)
        {
            Debug.Assert(cacheSize > 0, "Buffer size cannot be less than zero");
            Debug.Assert(onCacheHit != null, "Callback which listens for cache size limit cannot be null.");
            Debug.Assert(cacheTimeout > TimeSpan.MinValue, "The cache timeout must be greater than min value.");

            if (cacheTimeout.TotalMilliseconds > int.MaxValue)
            {
                cacheTimeout = TimeSpan.FromMilliseconds(int.MaxValue / 2);
            }

            this.cacheSize = cacheSize;
            this.onCacheHit = onCacheHit;
            lastUpdate = DateTime.Now;
            this.cacheTimeout = cacheTimeout;
            inProgressTests = new Collection<TestCase>();
            testResults = new Collection<TestResult>();
            runStats = new Dictionary<TestOutcome, long>();
            syncObject = new object();

            timer = new Timer(OnCacheTimeHit, this, cacheTimeout, cacheTimeout);
        }

        #endregion

        #region Delegates

        /// <summary>
        /// Called when the cache is ready to report on the current status.
        /// </summary>
        internal delegate void OnCacheHit(TestRunStatistics testRunStats, ICollection<TestResult> results, ICollection<TestCase> inProgressTests);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the test results present in the cache currently.
        /// </summary>
        public ICollection<TestResult> TestResults
        {
            get
            {
                lock (syncObject)
                {
                    return testResults;
                }
            }
        }

        /// <summary>
        /// Gets the set of in-progress test cases present in the cache currently.
        /// </summary>
        public ICollection<TestCase> InProgressTests
        {
            get
            {
                lock (syncObject)
                {
                    return inProgressTests;
                }
            }
        }

        /// <summary>
        /// Gets the total executed tests.
        /// </summary>
        public long TotalExecutedTests
        {
            get
            {
                lock (syncObject)
                {
                    return totalExecutedTests;
                }
            }
        }

        /// <summary>
        /// Gets the test run stats
        /// </summary>
        public TestRunStatistics TestRunStatistics
        {
            get
            {
                lock (syncObject)
                {
                    var stats = new TestRunStatistics(new Dictionary<TestOutcome, long>(runStats));
                    stats.ExecutedTests = TotalExecutedTests;

                    return stats;
                }
            }
        }

        public IDictionary<string, int> AdapterTelemetry { get; set; } = new Dictionary<string, int>();
        #endregion

        #region Public/internal methods

        /// <summary>
        /// Disposes the cache
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this valueType implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Notifies the cache that a test is starting.
        /// This notification comes from the adapter to the engine which then calls into the cache.
        /// </summary>
        /// <param name="testCase"> The test Case. </param>
        public void OnTestStarted(TestCase testCase)
        {
            lock (syncObject)
            {
                inProgressTests.Add(testCase);

                CheckForCacheHit();
            }
        }

        /// <summary>
        /// Notifies the cache of a new test result from the adapter.
        /// </summary>
        /// <param name="testResult"> The test result. </param>
        public void OnNewTestResult(TestResult testResult)
        {
            lock (syncObject)
            {
                totalExecutedTests++;
                testResults.Add(testResult);
                MSTestV1TelemetryHelper.AddTelemetry(testResult, AdapterTelemetry);

                if (runStats.TryGetValue(testResult.Outcome, out long count))
                {
                    count++;
                }
                else
                {
                    count = 1;
                }

                runStats[testResult.Outcome] = count;

                RemoveInProgress(testResult);

                CheckForCacheHit();
            }
        }

        /// <summary>
        /// Notifies the cache of a test completion.
        /// </summary>
        /// <param name="completedTest">
        /// The completed Test.
        /// </param>
        /// <returns> True if this test has been removed from the list of in progress tests. </returns>
        public bool OnTestCompletion(TestCase completedTest)
        {
            lock (syncObject)
            {
                if (completedTest == null)
                {
                    EqtTrace.Warning("TestRunCache: completedTest is null");
                    return false;
                }

                if (inProgressTests == null || inProgressTests.Count == 0)
                {
                    EqtTrace.Warning("TestRunCache: InProgressTests is null");
                    return false;
                }

                var removed = inProgressTests.Remove(completedTest);
                if (removed)
                {
                    return true;
                }

                // Try finding/removing a matching test corresponding to the completed test
                var inProgressTest = inProgressTests.FirstOrDefault(inProgress => inProgress.Id == completedTest.Id);
                if (inProgressTest != null)
                {
                    removed = inProgressTests.Remove(inProgressTest);
                }

                return removed;
            }
        }

        /// <summary>
        /// Returns the last chunk
        /// </summary>
        /// <returns> The set of test results remaining in the cache. </returns>
        public ICollection<TestResult> GetLastChunk()
        {
            lock (syncObject)
            {
                var lastChunk = testResults;

                testResults = new Collection<TestResult>();

                return lastChunk;
            }
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing"> Indicates if this needs to clean up managed resources. </param>
        /// <remarks>
        /// The dispose pattern is a best practice to differentiate between managed and native resources cleanup.
        /// Even though this particular class does not have any native resources - honoring the pattern to be consistent throughout the code base.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            // If you need thread safety, use a lock around these
            // operations, as well as in your methods that use the resource.
            if (disposing && !isDisposed)
            {
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }

                // Indicate that the instance has been disposed.
                isDisposed = true;
            }
        }

        #endregion

        #region Private methods.

        /// <summary>
        /// Checks if the cache timeout/size has been met.
        /// </summary>
        private void CheckForCacheHit()
        {
            lock (syncObject)
            {
                // Send results when the specified cache size has been reached or
                // after the specified cache timeout has been hit.
                var timeDelta = DateTime.Now - lastUpdate;

                var inProgressTestsCount = inProgressTests.Count;

                if ((testResults.Count + inProgressTestsCount) >= cacheSize || (timeDelta >= cacheTimeout && inProgressTestsCount > 0))
                {
                    SendResults();
                }
            }
        }

        private void CheckForCacheHitOnTimer()
        {
            lock (syncObject)
            {
                if (testResults.Count > 0 || inProgressTests.Count > 0)
                {
                    SendResults();
                }
            }
        }

        private void SendResults()
        {
            // Pass on the buffer to the listener and clear the old one
            onCacheHit(TestRunStatistics, testResults, inProgressTests);
            testResults = new Collection<TestResult>();
            inProgressTests = new Collection<TestCase>();
            lastUpdate = DateTime.Now;

            // Reset the timer
            timer.Change(cacheTimeout, cacheTimeout);

            EqtTrace.Verbose("TestRunCache: OnNewTestResult: Notified the onCacheHit callback.");
        }

        private void OnCacheTimeHit(object state)
        {
            lock (syncObject)
            {
                try
                {
                    CheckForCacheHitOnTimer();
                }
                catch (Exception ex)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("TestRunCache: OnCacheTimeHit: Exception occurred while checking for cache hit. {0}", ex);
                    }
                }
            }
        }

        private void RemoveInProgress(TestResult result)
        {
            var removed = OnTestCompletion(result.TestCase);
            if (!removed)
            {
                EqtTrace.Warning("TestRunCache: No test found corresponding to testResult '{0}' in inProgress list.", result.DisplayName);
            }
        }

        #endregion
    }
}
