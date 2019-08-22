// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel
{
    public class TestRunSummary
    {
        /// <summary>
        /// Indicates the pass percentage
        /// </summary>
        public int PassPercentage { get; set; }

        /// <summary>
        /// Total test run time.
        /// </summary>
        public string TotalRunTime { get; set; }

        /// <summary>
        /// Total tests of a test run.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Passed tests of test run.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Failed Tests of test run.
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Skipped Tests of test run.
        /// </summary>
        public int SkippedTests { get; set; }
    }
}