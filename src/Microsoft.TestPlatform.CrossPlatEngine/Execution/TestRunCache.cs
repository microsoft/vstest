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
        private Dictionary<TestOutcome, long> runStats;

        /// <summary>
        /// Total tests which have currently executed
        /// </summary>
        private long totalExecutedTests;

        /// <summary>
        /// Callback used when cache is ready to report some test results/case.
        /// </summary>
        private OnCacheHit onCacheHit;

        /// <summary>
        /// Max size of the test result buffer
        /// </summary>
        private long cacheSize;

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
        private object syncObject;

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
            this.lastUpdate = DateTime.Now;
            this.cacheTimeout = cacheTimeout;
            this.inProgressTests = new Collection<TestCase>();
            this.testResults = new Collection<TestResult>();
            this.runStats = new Dictionary<TestOutcome, long>();
            this.syncObject = new object();

            this.timer = new Timer(this.OnCacheTimeHit, this, cacheTimeout, cacheTimeout);
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
                lock (this.syncObject)
                {
                    return this.testResults;
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
                lock (this.syncObject)
                {
                    return this.inProgressTests;
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
                lock (this.syncObject)
                {
                    return this.totalExecutedTests;
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
                lock (this.syncObject)
                {
                    var stats = new TestRunStatistics(new Dictionary<TestOutcome, long>(this.runStats));
                    stats.ExecutedTests = this.TotalExecutedTests;

                    return stats;
                }
            }
        }

        #endregion

        #region Public/internal methods

        /// <summary>
        /// Disposes the cache
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

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
            lock (this.syncObject)
            {
                this.inProgressTests.Add(testCase);

                this.CheckForCacheHit();
            }
        }

        /// <summary>
        /// Notifies the cache of a new test result from the adapter.
        /// </summary>
        /// <param name="testResult"> The test result. </param>
        public void OnNewTestResult(TestResult testResult)
        {
            lock (this.syncObject)
            {
                this.totalExecutedTests++;
                this.testResults.Add(testResult);

                long count;
                if (this.runStats.TryGetValue(testResult.Outcome, out count))
                {
                    count++;
                }
                else
                {
                    count = 1;
                }

                this.runStats[testResult.Outcome] = count;

                this.RemoveInProgress(testResult);

                this.CheckForCacheHit();
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
            lock (this.syncObject)
            {
                if (completedTest == null)
                {
                    EqtTrace.Warning("TestRunCache: completedTest is null");
                    return false;
                }

                if (this.inProgressTests == null || this.inProgressTests.Count == 0)
                {
                    EqtTrace.Warning("TestRunCache: InProgressTests is null");
                    return false;
                }

                var removed = this.inProgressTests.Remove(completedTest);
                if (removed)
                {
                    return true;
                }

                // Try finding/removing a matching test corresponding to the completed test
                var inProgressTest = this.inProgressTests.Where(inProgress => inProgress.Id == completedTest.Id).FirstOrDefault();
                if (inProgressTest != null)
                {
                    removed = this.inProgressTests.Remove(inProgressTest);
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
            lock (this.syncObject)
            {
                var lastChunk = this.testResults;

                this.testResults = new Collection<TestResult>();

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
            if (disposing && !this.isDisposed)
            {
                if (this.timer != null)
                {
                    this.timer.Dispose();
                    this.timer = null;
                }

                // Indicate that the instance has been disposed.
                this.isDisposed = true;
            }
        }

        #endregion

        #region Private methods.

        /// <summary>
        /// Checks if the cache timeout/size has been met.
        /// </summary>
        private void CheckForCacheHit()
        {
            lock (this.syncObject)
            {
                // Send results when the specified cache size has been reached or
                // after the specified cache timeout has been hit.
                var timeDelta = DateTime.Now - this.lastUpdate;

                var inProgressTestsCount = this.inProgressTests.Count;

                if ((this.testResults.Count + inProgressTestsCount) >= this.cacheSize || (timeDelta >= this.cacheTimeout && inProgressTestsCount > 0))
                {
                    this.SendResults();
                }
            }
        }

        private void CheckForCacheHitOnTimer()
        {
            lock (this.syncObject)
            {
                if (this.testResults.Count > 0 || this.inProgressTests.Count > 0)
                {
                    this.SendResults();
                }
            }
        }

        private void SendResults()
        {
            // Pass on the buffer to the listener and clear the old one
            this.onCacheHit(this.TestRunStatistics, this.testResults, this.inProgressTests);
            this.testResults = new Collection<TestResult>();
            this.inProgressTests = new Collection<TestCase>();
            this.lastUpdate = DateTime.Now;

            // Reset the timer
            this.timer.Change(this.cacheTimeout, this.cacheTimeout);

            EqtTrace.Verbose("TestRunCache: OnNewTestResult: Notified the onCacheHit callback.");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void OnCacheTimeHit(object state)
        {
            lock (this.syncObject)
            {
                try
                {
                    this.CheckForCacheHitOnTimer();
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
            var removed = this.OnTestCompletion(result.TestCase);
            if (!removed)
            {
                EqtTrace.Warning("TestRunCache: No test found corresponding to testResult '{0}' in inProgress list.", result.DisplayName);
            }
        }

        #endregion
    }
}
