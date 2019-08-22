// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel
{
    using System.Collections.Generic;

    public class TestResultCollection
    {
        /// <summary>
        /// Source of the test dll.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Hash id of source.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// List of test results.
        /// </summary>
        public List<TestResult> ResultList { get; set; }

        /// <summary>
        /// list of failed test results.
        /// </summary>
        public List<TestResult> FailedResultList { get; set; }
    }
}
