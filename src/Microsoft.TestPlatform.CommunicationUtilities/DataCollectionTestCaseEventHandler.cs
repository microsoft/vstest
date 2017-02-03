// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

    /// <summary>
    /// The test case data collection request handler.
    /// </summary>
    internal class DataCollectionTestCaseEventHandler : IDataCollectionTestCaseEventHandler, IDisposable
    {
        private ICommunicationManager communicationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventHandler"/> class.
        /// </summary>
        internal DataCollectionTestCaseEventHandler() : this(new SocketCommunicationManager())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestCaseEventHandler"/> class.
        /// </summary>
        /// <param name="communicationManager">
        /// The communication manager.
        /// </param>
        internal DataCollectionTestCaseEventHandler(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
        }

        /// <inheritDoc />
        public int InitializeCommunication()
        {
            var port = this.communicationManager.HostServer();
            this.communicationManager.AcceptClientAsync();
            return port;
        }

        /// <inheritDoc />
        public bool WaitForRequestHandlerConnection(int connectionTimeout)
        {
            return this.communicationManager.WaitForClientConnection(connectionTimeout);
        }

        /// <inheritDoc />
        public void Close()
        {
            this.communicationManager?.StopServer();
        }

        /// <inheritDoc />
        public void ProcessRequests()
        {
            // todo : implement this while doing integration with test execution process.
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.communicationManager?.StopServer();
        }
    }
}
