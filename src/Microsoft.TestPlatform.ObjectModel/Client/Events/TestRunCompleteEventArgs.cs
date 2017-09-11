// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Event arguments used when a test run has completed.
    /// </summary>
    [DataContract]
    public class TestRunCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stats">The final stats for the test run. This parameter is only set for communications between the test host and the clients (like VS)</param>
        /// <param name="isCanceled">Specifies whether the test run is canceled.</param>
        /// <param name="isAborted">Specifies whether the test run is aborted.</param>
        /// <param name="error">Specifies the error encountered during the execution of the test run.</param>
        /// <param name="attachmentSets">Attachment sets associated with the run.</param>
        /// <param name="elapsedTime">Time elapsed in just running tests</param>
        /// <param name="metrics">Metrics</param>
        public TestRunCompleteEventArgs(ITestRunStatistics stats, bool isCanceled, bool isAborted, Exception error, Collection<AttachmentSet> attachmentSets, TimeSpan elapsedTime, IDictionary<string, string> metrics = null)
        {
            this.TestRunStatistics = stats;
            this.IsCanceled = isCanceled;
            this.IsAborted = isAborted;
            this.Error = error;
            this.AttachmentSets = attachmentSets;
            this.ElapsedTimeInRunningTests = elapsedTime;
            this.metrics = metrics;
        }

        /// <summary>
        /// Gets the statistics on the state of the test run.
        /// </summary>
        [DataMember]
        public ITestRunStatistics TestRunStatistics { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the test run is canceled or not. 
        /// </summary>
        [DataMember]
        public bool IsCanceled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the test run is aborted. 
        /// </summary>
        [DataMember]
        public bool IsAborted { get; private set; }

        /// <summary>
        /// Gets the error encountered during the execution of the test run. Null if there is no error.
        /// </summary>
        [DataMember]
        public Exception Error { get; private set; }
        
        /// <summary>
        /// Gets the attachment sets associated with the test run. 
        /// </summary>
        [DataMember]
        public Collection<AttachmentSet> AttachmentSets { get; private set; }

        /// <summary>
        /// Gets the time elapsed in just running the tests.
        /// Value is set to TimeSpan.Zero incase of any error.
        /// </summary>
        [DataMember]
        public TimeSpan ElapsedTimeInRunningTests { get; private set; }

        /// <summary>
        /// Get or Sets the Metrics
        /// </summary>
        [DataMember]
        public IDictionary<string, string> metrics { get; set; }
    }    
}
