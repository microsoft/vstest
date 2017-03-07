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
        private IDataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private IDataCollectionTestCaseEventManager dataCollectionTestCaseEventManager;
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
        public ProxyOutOfProcDataCollectionManager(IDataCollectionTestCaseEventSender dataCollectionTestCaseEventSender, IDataCollectionTestCaseEventManager dataCollectionTestCaseEventManager)
        {
            this.attachmentsCache = new Dictionary<Guid, Collection<AttachmentSet>>();
            this.dataCollectionTestCaseEventManager = dataCollectionTestCaseEventManager;
            this.dataCollectionTestCaseEventSender = dataCollectionTestCaseEventSender;

            this.dataCollectionTestCaseEventManager.TestCaseStart += this.TriggerTestCaseStart;
            this.dataCollectionTestCaseEventManager.SessionEnd += this.TriggerTestSessionEnd;
            this.dataCollectionTestCaseEventManager.TestCaseEnd += this.TriggerTestCaseEnd;
            this.dataCollectionTestCaseEventManager.TestResult += this.TriggerTestResult;
        }

        private void TriggerTestResult(object sender, TestResultEventArgs e)
        {
            lock (syncObject)
            {
                Collection<AttachmentSet> dcEntries;
                if (this.attachmentsCache.TryGetValue(e.TestResult.TestCase.Id, out dcEntries))
                {
                    foreach (var entry in dcEntries)
                    {
                        e.TestResult.Attachments.Add(entry);
                    }

                    // Remove the key
                    this.attachmentsCache.Remove(e.TestResult.TestCase.Id);
                }
            }
        }

        private void TriggerTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            this.dataCollectionTestCaseEventSender.SendTestCaseStart(e);
        }

        private void TriggerTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            var attachments = this.dataCollectionTestCaseEventSender.SendTestCaseEnd(e);

            if (attachments != null && attachments.Count > 0)
            {
                lock (this.syncObject)
                {
                    Collection<AttachmentSet> existingEntries = null;
                    if (this.attachmentsCache.TryGetValue(e.TestCaseId, out existingEntries))
                    {
                        foreach (AttachmentSet newEntry in attachments)
                        {
                            existingEntries.Add(newEntry);
                        }
                    }
                    else
                    {
                        this.attachmentsCache.Add(e.TestCaseId, attachments);
                    }
                }
            }
        }

        private void TriggerTestSessionEnd(object sender, SessionEndEventArgs e)
        {
            this.dataCollectionTestCaseEventSender.SendTestSessionEnd(e);
        }
    }
}
