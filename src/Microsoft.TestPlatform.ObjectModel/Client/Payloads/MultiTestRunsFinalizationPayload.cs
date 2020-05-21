// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class used to define the MultiTestRunsFinalizationPayload sent by the Vstest.console translation layers into design mode
    /// </summary>
    public class MultiTestRunsFinalizationPayload
    {
        /// <summary>
        /// Settings used for the discovery request.
        /// </summary>
        [DataMember]
        public ICollection<AttachmentSet> Attachments { get; set; }
    }
}
