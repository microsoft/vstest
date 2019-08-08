// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    [DataContract]
    [KnownType(typeof(TestResult))]
    public sealed class TestResults
    {
        /// <summary>
        /// Constructor Class for testResults
        /// </summary>
        public TestResults()
        {
        }

        /// <summary>
        /// It has the test run summary of all test results
        /// </summary>
        [DataMember]
        internal TestRunSummary Summary { get; set; }

        /// <summary>
        /// List of Run Level Message that is Informational 
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageInformational = new List<string>();

        /// <summary>
        /// List of Run Level MessageError and warnings
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageErrorAndWarning = new List<string>();

        /// <summary>
        /// List of all results in Hierachal model
        /// </summary>
        [DataMember]
        internal List<TestResult> Results = new List<TestResult>();

        /// <summary>
        /// Gives the count of elements that Present in the Results List
        /// </summary>
        /// <returns></returns>
        internal int GetTestResultscount()
        {
            return Results.Count;
        }
    }
}
