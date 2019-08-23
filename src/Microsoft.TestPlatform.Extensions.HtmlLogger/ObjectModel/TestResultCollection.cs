// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel
{
    using System.Collections.Generic;

    /// <summary>
    /// Stores the list of failed results and list of all results corresponding to the source.
    /// </summary>
    public class TestResultCollection
    {
        private readonly string source;

        public TestResultCollection(string source) => this.source = source ?? throw new ArgumentNullException(nameof(source));

        /// <summary>
        /// Source of the test dll.
        /// </summary>
        public string Source => this.source;

        /// <summary>
        /// Hash id of source.
        /// </summary>
        public int Id => this.source.GetHashCode();

        /// <summary>
        /// List of test results.
        /// </summary>
        public List<TestResult> ResultList { get; set; }

        /// <summary>
        /// List of failed test results.
        /// </summary>
        public List<TestResult> FailedResultList { get; set; }
    }
}
