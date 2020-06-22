// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class MultiTestRunFinalizationCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="isCanceled">Specifies whether the finalization is canceled.</param>
        /// <param name="isAborted">Specifies whether the finalization is aborted.</param>
        /// <param name="error">Specifies the error encountered during the execution of the finalization.</param>
        public MultiTestRunFinalizationCompleteEventArgs(bool isCanceled, bool isAborted, Exception error)
        {
            this.IsCanceled = isCanceled;
            this.IsAborted = isAborted;
            this.Error = error;
        }

        /// <summary>
        /// Gets a value indicating whether the finalization is aborted or not.
        /// </summary>
        [DataMember]
        public bool IsAborted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the finalization is canceled or not.
        /// </summary>
        [DataMember]
        public bool IsCanceled { get; private set; }

        /// <summary>
        /// Gets the error encountered during the finalization of the test runs. Null if there is no error.
        /// </summary>
        [DataMember]
        public Exception Error { get; private set; }

        /// <summary>
        /// Get or Sets the Metrics
        /// </summary>
        [DataMember]
        public IDictionary<string, object> Metrics { get; set; }
    }
}
