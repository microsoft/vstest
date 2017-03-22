﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// Sends test case events to communication layer.
    /// </summary>
    internal class ProxyOutOfProcDataCollectionManager
    {
        private IDataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private ITestEventsHandler testCaseEventsHandler;
        private Dictionary<Guid, Collection<AttachmentSet>> attachmentsCache;

        /// <summary>
        /// Sync object for ensuring that only run is active at a time
        /// </summary>
        private Object syncObject = new Object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOutOfProcDataCollectionManager"/> class.
        /// </summary>
        /// <param name="dataCollectionTestCaseEventSender">
        /// The data collection test case event sender.
        /// </param>
        /// <param name="dataCollectionTestCaseEventManager">
        /// The data collection test case event manager.
        /// </param>
        public ProxyOutOfProcDataCollectionManager(IDataCollectionTestCaseEventSender dataCollectionTestCaseEventSender, ITestEventsHandler testCaseEventsHandler)
        {
            this.attachmentsCache = new Dictionary<Guid, Collection<AttachmentSet>>();
            this.testCaseEventsHandler = testCaseEventsHandler;
            this.dataCollectionTestCaseEventSender = dataCollectionTestCaseEventSender;

            this.testCaseEventsHandler.TestCaseStart += this.TriggerTestCaseStart;
            this.testCaseEventsHandler.TestCaseEnd += this.TriggerTestCaseEnd;
            this.testCaseEventsHandler.TestResult += TriggerSendTestResult;
            this.testCaseEventsHandler.SessionEnd += this.TriggerTestSessionEnd;
            this.attachmentsCache = new Dictionary<Guid, Collection<AttachmentSet>>();
        }

        private void TriggerTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            this.dataCollectionTestCaseEventSender.SendTestCaseStart(e);
        }

        private void TriggerTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            var attachments = this.dataCollectionTestCaseEventSender.SendTestCaseEnd(e);

            if (attachments != null)
            {
                lock (syncObject)
                {
                    Collection<AttachmentSet> attachmentSets;
                    if (!attachmentsCache.TryGetValue(e.TestCaseId, out attachmentSets))
                    {
                        attachmentSets = new Collection<AttachmentSet>();
                        this.attachmentsCache.Add(e.TestCaseId, attachmentSets);
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
                Collection<AttachmentSet> attachmentSets;
                if (this.attachmentsCache.TryGetValue(e.TestCaseId, out attachmentSets))
                {
                    foreach (var attachment in attachmentSets)
                    {
                        e.TestResult.Attachments.Add(attachment);
                    }
                }

                this.attachmentsCache.Remove(e.TestCaseId);
            }
        }

        private void TriggerTestSessionEnd(object sender, SessionEndEventArgs e)
        {
            this.dataCollectionTestCaseEventSender.SendTestSessionEnd(e);
        }
    }
}
