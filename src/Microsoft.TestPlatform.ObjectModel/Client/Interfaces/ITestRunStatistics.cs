// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Stats on the test run state
    /// </summary>
    public interface ITestRunStatistics
    {
        /// <summary>
        /// The number of tests that have the specified value of TestOutcome
        /// </summary>
        /// <param name="testOutcome"></param>
        /// <returns></returns>
        long this[TestOutcome testOutcome] { get; }

        /// <summary>
        /// TestOutcome - Test count map
        /// </summary>
        IDictionary<TestOutcome, long> Stats { get; }

        /// <summary>
        /// Number of tests that have been run.
        /// </summary>
        long ExecutedTests { get; }
    }

}
