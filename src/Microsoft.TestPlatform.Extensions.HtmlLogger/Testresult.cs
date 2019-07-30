// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    public class TestResult
    {
        /// <summary>
        /// Fully Qualified name of test result
        /// </summary>
        public string FullyQualifiedName;

        /// <summary>
        /// DisplayName for the Particular TestResult.It is unique for each Testresult
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// the error stact trace of the testResult
        /// </summary>
        public string ErrorStackTrace;

        /// <summary>
        /// error message of the testresult
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// it is enum whether the testresult is passed failed or skipped  
        /// </summary>
        public TestOutcome resultOutcome;

        /// <summary>
        /// total timespan of the testresult
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// the list of testresults that are chioldren to the current TestResults
        /// </summary>
        public List<TestResult> innerTestResults;

        /// <summary>
        /// get the count of inner testresults count
        /// </summary>
        /// <returns></returns>
        internal int GetInnerTestResultscount()
        {
            return this.innerTestResults.Count;
        }

        /// <summary>
        /// givest the current testresult
        /// </summary>
        /// <returns></returns>
        internal TestResult GetTestResult()
        {
            return this;
        }
    }
}
