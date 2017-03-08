// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The data collection test case event manager for sending events to in-proc and out-of-proc data collectors.
    /// </summary>
    internal class DataCollectionTestCaseEventManager : IDataCollectionTestCaseEventManager
    {
        internal static TestProperty FlushResultTestResultPoperty;
        private IDictionary<Guid, List<TestResult>> testResultDictionary;
        private HashSet<Guid> testCaseEndStatusMap;
        private ITestRunCache testRunCache;

        private object testCaseEndStatusSyncObject = new object();

        /// <inheritdoc />
        public event EventHandler<SessionStartEventArgs> SessionStart;

        /// <inheritdoc />
        public event EventHandler<SessionEndEventArgs> SessionEnd;

        /// <inheritdoc />
        public event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        /// <inheritdoc />
        public event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        /// <inheritdoc />
        public event EventHandler<TestResultEventArgs> TestResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventManager"/> class.
        /// </summary>
        /// <param name="testRunCache">
        /// The test run cache.
        /// </param>
        public DataCollectionTestCaseEventManager(ITestRunCache testRunCache)
        {
            this.testRunCache = testRunCache;
            this.testResultDictionary = new Dictionary<Guid, List<TestResult>>();
            this.testCaseEndStatusMap = new HashSet<Guid>();

            FlushResultTestResultPoperty = TestProperty.Register(id: "allowTestResultFlush", label: "allowTestResultFlush", category: string.Empty, description: string.Empty, valueType: typeof(bool), validateValueCallback: null, attributes: TestPropertyAttributes.None, owner: typeof(TestCase));
        }

        /// <inheritdoc />
        public void RaiseSessionStart(SessionStartEventArgs e)
        {
            this.testCaseEndStatusMap.Clear();
            this.testResultDictionary.Clear();

            this.SessionStart.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseSessionStart");
        }

        /// <inheritdoc />
        public void RaiseSessionEnd(SessionEndEventArgs e)
        {
            this.SessionEnd.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseSessionEnd");
        }

        /// <inheritdoc />
        public void RaiseTestCaseStart(TestCaseStartEventArgs e)
        {
            lock (this.testCaseEndStatusSyncObject)
            {
                this.testCaseEndStatusMap.Remove(e.TestCaseId);
            }

            this.TestCaseStart.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseTestCaseStart");
        }

        /// <inheritdoc />
        public void RaiseTestCaseEnd(TestCaseEndEventArgs e)
        {
            var isTestCaseEndAlreadySent = false;
            lock (this.testCaseEndStatusSyncObject)
            {
                isTestCaseEndAlreadySent = this.testCaseEndStatusMap.Contains(e.TestCaseId);
                if (!isTestCaseEndAlreadySent)
                {
                    this.testCaseEndStatusMap.Add(e.TestCaseId);
                }

                // Do not support multiple - testcasends for a single test case start
                // TestCaseEnd must always be preceded by testcasestart for a given test case id
                if (!isTestCaseEndAlreadySent)
                {
                    // If dictionary contains results for this test case, update them with in-proc data and flush them
                    List<TestResult> testResults;
                    if (this.testResultDictionary.TryGetValue(e.TestCaseId, out testResults))
                    {
                        foreach (var testResult in testResults)
                        {
                            var testResultEventArgs = new TestResultEventArgs(testResult);
                            this.TestResult.SafeInvoke(this, testResultEventArgs, "DataCollectionTestCaseEventManager.RaiseTestCaseEnd");

                            // TestResult updated with in-proc data, just flush
                            this.testRunCache.OnNewTestResult(testResult);
                        }

                        this.testResultDictionary.Remove(e.TestCaseId);
                    }
                    else
                    {
                        // Call all in-proc datacollectors - TestCaseEnd event
                        this.TestCaseEnd.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseTestCaseEnd");
                    }
                }
            }
        }

        /// <inheritdoc />
        public void RaiseTestResult(TestResultEventArgs e)
        {
            var allowTestResultFlush = true;
            var testCaseId = e.TestResult.TestCase.Id;

            lock (this.testCaseEndStatusSyncObject)
            {
                if (this.testCaseEndStatusMap.Contains(testCaseId))
                {
                    this.TestResult.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseTestCaseEnd");
                }
                else
                {
                    // No TestCaseEnd received yet
                    // We need to wait for testcaseend before flushing
                    allowTestResultFlush = false;

                    List<TestResult> testResults;
                    // Cache results so we can flush later with in proc data
                    if (this.testResultDictionary.TryGetValue(testCaseId, out testResults))
                    {
                        testResults.Add(e.TestResult);
                    }
                    else
                    {
                        this.testResultDictionary.Add(testCaseId, new List<TestResult>() { e.TestResult });
                    }
                }
            }

            this.SetAllowTestResultFlushInTestResult(e.TestResult, allowTestResultFlush);
        }

        /// <inheritdoc />
        public void FlushLastChunkResults()
        {
            // Can happen if we cached test results expecting a test case end event for them
            // If test case end events never come, we have to flush all of them 
            foreach (var results in this.testResultDictionary.Values)
            {
                foreach (var result in results)
                {
                    this.testRunCache.OnNewTestResult(result);
                }
            }
        }

        /// <summary>
        /// Set the data sent via datacollection sink in the testresult property for upstream applications to read.
        /// And removes the data from the dictionary.
        /// </summary>
        /// <param name="testResult">
        /// The test Result.
        /// </param>
        /// <param name="allowTestResultFlush">
        /// The allow Test Result Flush.
        /// </param>
        private void SetAllowTestResultFlushInTestResult(TestResult testResult, bool allowTestResultFlush)
        {
            testResult.SetPropertyValue(FlushResultTestResultPoperty, allowTestResultFlush);
        }
    }
}
