// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.Generic;

    public class TestResult
    {
        /// <summary>
        /// Fully Qualified name of TestResult.
        /// </summary>
        public string FullyQualifiedName;

        /// <summary>
        /// DisplayName for the Particular TestResult.It is unique for each TestResult.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// The error stack trace of the TestResult.
        /// </summary>
        public string ErrorStackTrace;

        /// <summary>
        /// Error message of the TestResult.
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// It is enum whether the TestResult is passed failed or skipped.
        /// </summary>
        public TestOutcome resultOutcome;

        /// <summary>
        /// Total timespan of the TestResult
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// The list of TestResults that are children to the current TestResult.
        /// </summary>
        public List<TestResult> innerTestResults;

        /// <summary>
        /// Get the count of inner TestResults count.
        /// </summary>
        /// <returns></returns>
        internal int GetInnerTestResultscount()
        {
            return this.innerTestResults.Count;
        }

        /// <summary>
        /// Gives the current TestResult.
        /// </summary>
        /// <returns></returns>
        internal TestResult GetTestResult()
        {
            return this;
        }
    }
}
