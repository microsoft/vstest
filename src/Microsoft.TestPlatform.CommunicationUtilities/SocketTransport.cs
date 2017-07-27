// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Net;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <inheritdoc/>
    public sealed class SocketTransport : ITransport
    {
        /// <summary>
        /// Specifies whether the resolver is disposed or not
        /// </summary>
        private bool disposed;

        private TestHostConnectionInfo connectionInfo;

        private ICommunicationManager communicationManager;

        public SocketTransport(ICommunicationManager communicationManager, TestHostConnectionInfo connectionInfo)
        {
            this.communicationManager = communicationManager;
            this.connectionInfo = connectionInfo;
        }

        /// <inheritdoc/>
        public IPEndPoint Initialize()
        {
            var endpoint = this.GetIPEndPoint(this.connectionInfo.Endpoint);
            switch (this.connectionInfo.Role)
            {
                case ConnectionRole.Host:
                    {
                        // In case users passes endpoint Port as 0 HostServer will allocate endpoint at appropriate port,
                        // So reassign endpoint to point to correct endpoint.
                        endpoint = this.communicationManager.HostServer(endpoint);
                        this.communicationManager.AcceptClientAsync();
                        return endpoint;
                    }

                case ConnectionRole.Client:
                    {
                        this.communicationManager.SetupClientAsync(this.GetIPEndPoint(this.connectionInfo.Endpoint));
                        return endpoint;
                    }

                default:
                    throw new NotImplementedException("Unsupported Connection Role");
            }
        }

        /// <inheritdoc/>
        public bool WaitForConnection(int connectionTimeout)
        {
            return this.connectionInfo.Role == ConnectionRole.Client ? this.communicationManager.WaitForServerConnection(connectionTimeout) : this.communicationManager.WaitForClientConnection(connectionTimeout);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.connectionInfo.Role == ConnectionRole.Client)
                    {
                        this.communicationManager?.StopClient();
                    }
                    else
                    {
                        this.communicationManager?.StopServer();
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Converts a given string endpoint address to valid Ipv4, Ipv6 IPEndpoint
        /// </summary>
        /// <param name="endpointAddress">Input endpoint address</param>
        /// <returns>IPEndpoint from give string</returns>
        private IPEndPoint GetIPEndPoint(string endpointAddress)
        {
            if (Uri.TryCreate(string.Concat("tcp://", endpointAddress), UriKind.Absolute, out Uri uri))
            {
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
            }

            return null;
        }
    }
}
