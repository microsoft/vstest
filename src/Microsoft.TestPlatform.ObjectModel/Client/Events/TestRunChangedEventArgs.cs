// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    
    /// <summary>
    /// The test run changed event args that provides the test results available.
    /// </summary>
    public class TestRunChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunChangedEventArgs"/> class.
        /// </summary>
        /// <param name="stats"> The stats. </param>
        /// <param name="newTestResults"> The new test results. </param>
        /// <param name="activeTests"> The active tests. </param>
        public TestRunChangedEventArgs(ITestRunStatistics stats, IEnumerable<TestResult> newTestResults, IEnumerable<TestCase> activeTests)
        {
            this.TestRunStatistics = stats;
            this.NewTestResults = newTestResults;
            this.ActiveTests = activeTests;
        }

        /// <summary>
        /// Gets the new test results.
        /// </summary>
        public IEnumerable<TestResult> NewTestResults { get; private set; }

        /// <summary>
        /// Gets the test run statistics.
        /// </summary>
        public ITestRunStatistics TestRunStatistics { get; private set; }

        /// <summary>
        /// Gets the active tests.
        /// </summary>
        public IEnumerable<TestCase> ActiveTests { get; private set; }
    }
}
