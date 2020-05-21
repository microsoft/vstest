// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    public class MultiTestRunsDataCollectorAttachmentsHandler
    {
        private readonly IDataCollectorAttachments dataCollectorAttachmentsHandler;

        public MultiTestRunsDataCollectorAttachmentsHandler(IDataCollectorAttachments dataCollectorAttachmentsHandler)
        {            
            this.dataCollectorAttachmentsHandler = dataCollectorAttachmentsHandler;
        }

        public void HandleAttachements(ICollection<AttachmentSet> attachments)
        {
            Uri attachementUri = dataCollectorAttachmentsHandler.GetExtensionUri();
            if (attachementUri != null)
            {
                var coverageAttachments = attachments.Where(dataCollectionAttachment => attachementUri.Equals(dataCollectionAttachment.Uri)).ToArray();

                foreach (var coverageAttachment in coverageAttachments)
                {
                    attachments.Remove(coverageAttachment);
                }

                ICollection<AttachmentSet> mergedAttachments = dataCollectorAttachmentsHandler.HandleDataCollectionAttachmentSets(new Collection<AttachmentSet>(coverageAttachments));
                foreach (var attachment in mergedAttachments)
                {
                    attachments.Add(attachment);
                }
            }
        }
    }
}
