﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    public class DataCollectorAttachmentsHandler
    {
        private readonly IDataCollectorAttachments[] dataCollectorAttachmentsHandlers;

        public DataCollectorAttachmentsHandler(params IDataCollectorAttachments[] dataCollectorAttachmentsHandlers)
        {            
            this.dataCollectorAttachmentsHandlers = dataCollectorAttachmentsHandlers;
        }

        public void HandleAttachements(ICollection<AttachmentSet> attachments, CancellationToken cancellationToken)
        {
            foreach(var dataCollectorAttachmentsHandler in dataCollectorAttachmentsHandlers)
            {
                Uri attachementUri = dataCollectorAttachmentsHandler.GetExtensionUri();
                if (attachementUri != null)
                {
                    var attachmentsToBeProcessed = attachments.Where(dataCollectionAttachment => attachementUri.Equals(dataCollectionAttachment.Uri)).ToArray();
                    if(attachmentsToBeProcessed.Any())
                    {
                        foreach (var attachment in attachmentsToBeProcessed)
                        {
                            attachments.Remove(attachment);
                        }

                        ICollection<AttachmentSet> processedAttachements = dataCollectorAttachmentsHandler.HandleDataCollectionAttachmentSets(new Collection<AttachmentSet>(attachmentsToBeProcessed), cancellationToken);
                        foreach (var attachment in processedAttachements)
                        {
                            attachments.Add(attachment);
                        }
                    }
                }
            }
        }
    }
}
