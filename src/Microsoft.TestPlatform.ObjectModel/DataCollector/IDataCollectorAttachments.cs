// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Interface for data collectors add-ins that choose to handle attachment(s) generated
    /// </summary>
    public interface IDataCollectorAttachments
    {
        /// <summary>
        /// Gets the attachment set after Test Run Session
        /// </summary>
        /// <returns>Gets the attachment set after Test Run Session</returns>
        ICollection<AttachmentSet> HandleDataCollectionAttachmentSets(ICollection<AttachmentSet> dataCollectionAttachments);

        /// <summary>
        /// Gets the attachment set after Test Run Session
        /// </summary>
        /// <param name="dataCollectionAttachments">Attachments to be processed</param>
        /// <param name="progressReporter">Progress reporter. Accepts integers from 0 to 100</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Gets the attachment set after Test Run Session</returns>
        ICollection<AttachmentSet> HandleDataCollectionAttachmentSets(ICollection<AttachmentSet> dataCollectionAttachments, IProgress<int> progressReporter, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the attachment Uri, which is handled by current Collector
        /// </summary>
        Uri GetExtensionUri();
    }
}
