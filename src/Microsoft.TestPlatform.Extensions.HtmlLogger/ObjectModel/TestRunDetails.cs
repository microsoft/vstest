// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    [KnownType(typeof(TestResult))]
    public sealed class TestRunDetails
    {
        /// <summary>
        /// Constructor Class for TestRunDetails.
        /// </summary>
        public TestRunDetails()
        {
        }

        /// <summary>
        /// It has the test run summary of all TestResults.
        /// </summary>
        [DataMember]
        internal TestRunSummary Summary { get; set; }

        /// <summary>
        /// List of Run level message that is Informational.
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageInformational = new List<string>();

        /// <summary>
        /// List of Run level message error and warnings
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageErrorAndWarning = new List<string>();

        /// <summary>
        /// List of all results in hierarchical model.
        /// </summary>
        [DataMember]
        internal List<TestResult> Results = new List<TestResult>();

        /// <summary>
        /// Gives the count of elements that Present in the results list.
        /// </summary>
        /// <returns></returns>
        internal int GetTestResultscount()
        {
            return Results.Count;
        }
    }
}
