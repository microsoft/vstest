// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Execution
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// The cache for test execution information.
    /// </summary>
    public interface ITestRunCache : IDisposable
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