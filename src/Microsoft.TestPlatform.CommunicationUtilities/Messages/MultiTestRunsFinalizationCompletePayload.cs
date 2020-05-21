// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Multi test runs finalization complete payload.
    /// </summary>
    public class MultiTestRunsFinalizationCompletePayload
    {
        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>
        public ICollection<AttachmentSet> Attachments { get; set; }
    }
}
