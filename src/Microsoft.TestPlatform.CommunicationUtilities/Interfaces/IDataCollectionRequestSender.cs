// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces
{
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;

    /// <summary>
    /// Defines contract to send test platform requests to test host
    /// </summary>
    internal interface IDataCollectionRequestSender
    {
        /// <summary>
        /// Initializes the communication for sending requests
        /// </summary>
        /// <returns>Port Number of the communication channel</returns>
        int InitializeCommunication();

        /// <summary>
        /// Waits for Request Handler to be connected 
        /// </summary>
        /// <param name="connectionTimeout">Time to wait for connection</param>
        /// <returns>True, if Handler is connected</returns>
        bool WaitForRequestHandlerConnection(int connectionTimeout);

        /// <summary>
        /// Close the Sender 
        /// </summary>
        void Close();

        /// <summary>
        /// Sends the BeforeTestRunStart event and waits for result
        /// </summary> 
        /// <param name="settingXml"></param>
        /// <returns>BeforeTestRunStartResult containing environment variables</returns>
        BeforeTestRunStartResult SendBeforeTestRunStartAndGetResult(string settingXml);

        /// <summary>
        /// Sends the AfterTestRunStart event and waits for result
        /// </summary>
        /// <returns>DataCollector attachments</returns>
        Collection<AttachmentSet> SendAfterTestRunStartAndGetResult();
    }
}