// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Sends test case events to communication layer.
    /// </summary>
    internal class ProxyOutOfProcDataCollectionManager
    {
        private IDataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private IDataCollectionTestCaseEventManager dataCollectionTestCaseEventManager;

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
            this.dataCollectionTestCaseEventManager = dataCollectionTestCaseEventManager;
            this.dataCollectionTestCaseEventSender = dataCollectionTestCaseEventSender;

            this.dataCollectionTestCaseEventManager.TestCaseStart += this.TriggerTestCaseStart;
            this.dataCollectionTestCaseEventManager.TestResult += this.TriggerSendTestResult;
            this.dataCollectionTestCaseEventManager.SessionEnd += this.TriggerTestSessionEnd;
        }

        private void TriggerSendTestResult(object sender, TestResultEventArgs e)
        {
            this.dataCollectionTestCaseEventSender.SendTestCaseComplete(e);
        }

        private void TriggerTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            this.dataCollectionTestCaseEventSender.SendTestCaseStart(e);
        }

        private void TriggerTestSessionEnd(object sender, SessionEndEventArgs e)
        {
            this.dataCollectionTestCaseEventSender.SendTestSessionEnd(e);
        }
    }
}
