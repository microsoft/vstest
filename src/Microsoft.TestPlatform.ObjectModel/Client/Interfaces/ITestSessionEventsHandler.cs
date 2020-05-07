// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface contract for handling test session complete events
    /// </summary>
    public interface ITestSessionEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Dispatch SessionComplete event to listeners.
        /// </summary>
        /// <param name="attachments">Attachments reprocessed.</param>
        void HandleTestSessionComplete(IEnumerable<AttachmentSet> attachments);
    }
}