// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.TestRunnerConnectionInfo;

    /// <inheritdoc/>
    public class SocketTransport : ITransport
    {
        private ConnectionInfo connectionInfo;

        private ICommunicationManager communicationManager;

        public SocketTransport(ICommunicationManager communicationManager, ConnectionInfo connectionInfo)
        {
            this.communicationManager = communicationManager;
            this.connectionInfo = connectionInfo;
        }

        /// <inheritdoc/>
        public int InitializeTransportLayer()
        {
            switch (this.connectionInfo.Role)
            {
                case ConnectionRole.Host:
                    {
                        var port = this.communicationManager.HostServer(this.connectionInfo.Endpoint);
                        this.communicationManager.AcceptClientAsync();
                        return port;
                    }

                case ConnectionRole.Client:
                    {
                        this.communicationManager.SetupClientAsync(this.connectionInfo.Endpoint);
                        break;
                    }

                default:
                    throw new NotImplementedException("Unsupported Connection Role");
            }

            return 1;
        }

        /// <inheritdoc/>
        public bool WaitForConnectionToEstablish(int connectionTimeout)
        {
            return this.connectionInfo.Role == ConnectionRole.Client ? this.communicationManager.WaitForServerConnection(connectionTimeout) : this.communicationManager.WaitForClientConnection(connectionTimeout);
        }
    }
}
