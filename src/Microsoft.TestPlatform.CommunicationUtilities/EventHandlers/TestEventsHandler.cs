// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The test case events handler.
    /// </summary>
    internal class TestEventsHandler : ITestEventsHandler
    {
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
        /// Initializes a new instance of the <see cref="TestEventsHandler"/> class.
        /// </summary>
        public TestEventsHandler()
        {
            this.testResultDictionary = new Dictionary<Guid, List<TestResult>>();
            this.testCaseEndStatusMap = new HashSet<Guid>();
        }

        /// <inheritdoc />
        public void Initialize(ITestRunCache testRunCache)
        {
            this.testRunCache = testRunCache;
            this.testCaseEndStatusMap.Clear();
            this.testResultDictionary.Clear();
        }

        /// <inheritdoc />
        public void SendTestSessionStart()
        {
            this.SessionStart.SafeInvoke(this, new SessionStartEventArgs(), "TestCaseEventsHandler.SendTestSessionStart");
        }

        /// <inheritdoc />
        public void SendTestSessionEnd()
        {
            this.SessionEnd.SafeInvoke(this, new SessionEndEventArgs(), "TestCaseEventsHandler.SendTestSessionEnd");
        }

        /// <inheritdoc />
        public void SendTestCaseStart(TestCase testCase)
        {
            var eventArgs = new TestCaseStartEventArgs(testCase);

            lock (this.testCaseEndStatusSyncObject)
            {
                this.testCaseEndStatusMap.Remove(eventArgs.TestCaseId);
            }

            this.TestCaseStart.SafeInvoke(this, eventArgs, "TestCaseEventsHandler.RaiseTestCaseStart");
        }

        /// <inheritdoc />
        public void SendTestCaseEnd(TestCase testCase, TestOutcome outcome)
        {
            var eventArgs = new TestCaseEndEventArgs(testCase, outcome);
            var isTestCaseEndAlreadySent = false;
            lock (this.testCaseEndStatusSyncObject)
            {
                isTestCaseEndAlreadySent = this.testCaseEndStatusMap.Contains(eventArgs.TestCaseId);
                if (!isTestCaseEndAlreadySent)
                {
                    this.testCaseEndStatusMap.Add(eventArgs.TestCaseId);
                }

                // Do not support multiple - testcasends for a single test case start
                // TestCaseEnd must always be preceded by testcasestart for a given test case id
                if (!isTestCaseEndAlreadySent)
                {
                    // If dictionary contains results for this test case, update them with in-proc data and flush them
                    List<TestResult> testResults;
                    if (this.testResultDictionary.TryGetValue(eventArgs.TestCaseId, out testResults))
                    {
                        foreach (var testResult in testResults)
                        {
                            var testResultEventArgs = new TestResultEventArgs(testResult);
                            this.TestResult.SafeInvoke(this, testResultEventArgs, "TestCaseEventsHandler.SendTestCaseEnd");

                            // TestResult updated with in-proc data, just flush
                            this.testRunCache.OnNewTestResult(testResult);
                        }

                        this.testResultDictionary.Remove(eventArgs.TestCaseId);
                    }
                    else
                    {
                        // Call all in-proc datacollectors - TestCaseEnd event
                        this.TestCaseEnd.SafeInvoke(this, eventArgs, "TestCaseEventsHandler.SendTestCaseEnd");
                    }
                }
            }
        }

        /// <inheritdoc />
        public bool SendTestResult(TestResult result)
        {
            var eventArgs = new TestResultEventArgs(result);
            var allowTestResultFlush = true;
            var testCaseId = eventArgs.TestResult.TestCase.Id;

            lock (this.testCaseEndStatusSyncObject)
            {
                if (this.testCaseEndStatusMap.Contains(testCaseId))
                {
                    this.TestResult.SafeInvoke(this, eventArgs, "TestCaseEventsHandler.SendTestResult");
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
                        testResults.Add(eventArgs.TestResult);
                    }
                    else
                    {
                        this.testResultDictionary.Add(testCaseId, new List<TestResult>() { eventArgs.TestResult });
                    }
                }
            }

            return allowTestResultFlush;
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
    }
}
