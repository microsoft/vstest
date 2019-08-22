// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public sealed class TestRunDetails
    {
        /// <summary>
        /// Constructor Class for TestRunDetails.
        /// </summary>
        public TestRunDetails()
        {
        }

        /// <summary>
        /// Test run summary of all test results.
        /// </summary>
        [DataMember]
        internal TestRunSummary Summary { get; set; }

        /// <summary>
        /// List of informational run level messages.
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageInformational = new List<string>();

        /// <summary>
        /// List of error and warning messages.
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageErrorAndWarning = new List<string>();

        /// <summary>
        /// List of all the results
        /// </summary>
        [DataMember]
        internal List<TestResultCollection> ResultCollectionList = new List<TestResultCollection>();
    }
}
