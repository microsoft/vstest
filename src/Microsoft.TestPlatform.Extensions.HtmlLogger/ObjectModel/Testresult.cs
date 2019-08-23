﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel
{

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Test results stores the relevant information to show on html file
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Fully qualified name of the Test Result.
        /// </summary>
        public string FullyQualifiedName { get; set; }

        /// <summary>
        /// Unique identifier for test result
        /// </summary>
        public Guid TestResultId { get; set; }

        /// <summary>
        /// Display Name for the particular Test Result
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The error stack trace of the Test Result.
        /// </summary>
        public string ErrorStackTrace { get; set; }

        /// <summary>
        /// Error message of the Test Result.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Enum that determines the outcome of the test case
        /// </summary>
        public TestOutcome ResultOutcome { get; set; }

        /// <summary>
        /// Total timespan of the TestResult
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// The list of TestResults that are children to the current Test Result.
        /// </summary>
        public List<TestResult> InnerTestResults { get; set; }
    }
}
