// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System.Collections.Generic;

    using ObjectModel;
    using ObjectModel.Client;

    /// <summary>
    /// The cache for test execution information.
    /// </summary>
    internal interface ITestRunCache
    {
        #region Properties

        ICollection<TestResult> TestResults { get; }

        ICollection<TestCase> InProgressTests { get; }
        
        long TotalExecutedTests { get; }

        TestRunStatistics TestRunStatistics { get; }

        #endregion

        #region Methods

        void OnTestStarted(TestCase testCase);

        void OnNewTestResult(TestResult testResult);

        bool OnTestCompletion(TestCase completedTest);

        ICollection<TestResult> GetLastChunk();

        #endregion
    }
}