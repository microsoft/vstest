// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Stores the list of failed results and list of all results corresponding to the source.
    /// </summary>
    [DataContract]
    public class TestResultCollection
    {
        private readonly string source;

        public TestResultCollection(string source) => this.source = source ?? throw new ArgumentNullException(nameof(source));

        /// <summary>
        /// Source of the test dll.
        /// </summary>
        [DataMember] public string Source
        {
            get => this.source;
            private set { }
        }

        /// <summary>
        /// Hash id of source.
        /// </summary>
        [DataMember] public int Id
        {
            get => this.source.GetHashCode();
            private set { }
        }

        /// <summary>
        /// List of test results.
        /// </summary>
        [DataMember] public List<TestResult> ResultList { get; set; }

        /// <summary>
        /// List of failed test results.
        /// </summary>
        [DataMember] public List<TestResult> FailedResultList { get; set; }
    }
}
