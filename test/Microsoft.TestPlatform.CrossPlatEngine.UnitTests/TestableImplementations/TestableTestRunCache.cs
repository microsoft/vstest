// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    public class TestableTestRunCache : ITestRunCache
    {
        public TestableTestRunCache()
        {
            this.TestStartedList = new List<TestCase>();
            this.TestCompletedList = new List<TestCase>();
            this.TestResultList = new List<TestResult>();
        }
        
        // use the below three to fill in data to the testable cache.
        public List<TestCase> TestStartedList { get; private set; }

        public List<TestCase> TestCompletedList { get; private set; }

        public List<TestResult> TestResultList { get; private set; }

        public ICollection<TestCase> InProgressTests { get; set; }


        // Use the TestResultList instead to fill in data. This is just to avoid confusion.
        public ICollection<TestResult> TestResults { get; set; }

        public TestRunStatistics TestRunStatistics { get; set; }

        public long TotalExecutedTests { get; set; }

        public ICollection<TestResult> GetLastChunk()
        {
            return this.TestResultList;
        }

        public void OnNewTestResult(TestResult testResult)
        {
            this.TestResultList.Add(testResult);
        }

        public bool OnTestCompletion(TestCase testCase)
        {
            this.TestCompletedList.Add(testCase);

            return false;
        }

        public void OnTestStarted(TestCase testCase)
        {
            this.TestStartedList.Add(testCase);
        }

        public void Dispose()
        {
        }
    }
}
