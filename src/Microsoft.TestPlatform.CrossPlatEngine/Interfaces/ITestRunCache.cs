// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using System;

    /// <summary>
    /// The cache for test execution information.
    /// </summary>
    internal interface ITestRunCache : IDisposable
    {
        #region Properties

        ICollection<TestResult> TestResults { get; }

        ICollection<TestCase> InProgressTests { get; }
        long TotalExecutedTests { get; }

        TestRunStatistics TestRunStatistics { get; }

        IDictionary<string, int> AdapterTelemetry { get; }

        #endregion

        #region Methods

        void OnTestStarted(TestCase testCase);

        void OnNewTestResult(TestResult testResult);

        bool OnTestCompletion(TestCase completedTest);

        ICollection<TestResult> GetLastChunk();

        #endregion
    }
}