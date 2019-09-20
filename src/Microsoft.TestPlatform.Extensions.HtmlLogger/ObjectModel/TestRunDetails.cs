// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// It stores the all relevant information of the test run.
    /// </summary>
    [DataContract]
    public sealed class TestRunDetails
    {
        /// <summary>
        /// Test run summary of all test results.
        /// </summary>
        [DataMember] public TestRunSummary Summary { get; set; }

        /// <summary>
        /// List of informational run level messages.
        /// </summary>
        [DataMember] public List<string> RunLevelMessageInformational;

        /// <summary>
        /// List of error and warning messages.
        /// </summary>
        [DataMember] public List<string> RunLevelMessageErrorAndWarning;

        /// <summary>
        /// List of all the results
        /// </summary>
        [DataMember] public List<TestResultCollection> ResultCollectionList = new List<TestResultCollection>();
    }
}
