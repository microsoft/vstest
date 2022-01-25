// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Sends test case events to communication layer.
    /// </summary>
    internal class ProxyOutOfProcDataCollectionManager
    {
        private readonly IDataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private readonly ITestEventsPublisher testEventsPublisher;
        private readonly Dictionary<Guid, Collection<AttachmentSet>> attachmentsCache;

        /// <summary>
        /// Sync object for ensuring that only run is active at a time
        /// </summary>
        private readonly object syncObject = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOutOfProcDataCollectionManager"/> class.
        /// </summary>
        /// <param name="dataCollectionTestCaseEventSender">
        /// The data collection test case event sender.
        /// </param>
        /// <param name="dataCollectionTestCaseEventManager">
        /// The data collection test case event manager.
        /// </param>
        public ProxyOutOfProcDataCollectionManager(IDataCollectionTestCaseEventSender dataCollectionTestCaseEventSender, ITestEventsPublisher testEventsPublisher)
        {
            attachmentsCache = new Dictionary<Guid, Collection<AttachmentSet>>();
            this.testEventsPublisher = testEventsPublisher;
            this.dataCollectionTestCaseEventSender = dataCollectionTestCaseEventSender;

            this.testEventsPublisher.TestCaseStart += TriggerTestCaseStart;
            this.testEventsPublisher.TestCaseEnd += TriggerTestCaseEnd;
            this.testEventsPublisher.TestResult += TriggerSendTestResult;
            this.testEventsPublisher.SessionEnd += TriggerTestSessionEnd;
            attachmentsCache = new Dictionary<Guid, Collection<AttachmentSet>>();
        }

        private void TriggerTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            dataCollectionTestCaseEventSender.SendTestCaseStart(e);
        }

        private void TriggerTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            var attachments = dataCollectionTestCaseEventSender.SendTestCaseEnd(e);

            if (attachments != null)
            {
                lock (syncObject)
                {
                    if (!attachmentsCache.TryGetValue(e.TestCaseId, out Collection<AttachmentSet> attachmentSets))
                    {
                        attachmentSets = new Collection<AttachmentSet>();
                        attachmentsCache.Add(e.TestCaseId, attachmentSets);
                    }

                    foreach (var attachment in attachments)
                    {
                        attachmentSets.Add(attachment);
                    }
                }
            }
        }

        private void TriggerSendTestResult(object sender, TestResultEventArgs e)
        {
            lock (syncObject)
            {
                if (attachmentsCache.TryGetValue(e.TestCaseId, out Collection<AttachmentSet> attachmentSets))
                {
                    foreach (var attachment in attachmentSets)
                    {
                        e.TestResult.Attachments.Add(attachment);
                    }
                }

                attachmentsCache.Remove(e.TestCaseId);
            }
        }

        private void TriggerTestSessionEnd(object sender, SessionEndEventArgs e)
        {
            dataCollectionTestCaseEventSender.SendTestSessionEnd(e);
        }
    }
}
