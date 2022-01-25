﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

using System;
using System.Net;

using Interfaces;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <inheritdoc/>
public sealed class SocketTransport : ITransport
{
    /// <summary>
    /// Specifies whether the resolver is disposed or not
    /// </summary>
    private bool _disposed;

    private readonly TestHostConnectionInfo _connectionInfo;

    private readonly ICommunicationManager _communicationManager;

    public SocketTransport(ICommunicationManager communicationManager, TestHostConnectionInfo connectionInfo)
    {
        _communicationManager = communicationManager;
        _connectionInfo = connectionInfo;
    }

    /// <inheritdoc/>
    public IPEndPoint Initialize()
    {
        var endpoint = GetIpEndPoint(_connectionInfo.Endpoint);
        switch (_connectionInfo.Role)
        {
            case ConnectionRole.Host:
                {
                    // In case users passes endpoint Port as 0 HostServer will allocate endpoint at appropriate port,
                    // So reassign endpoint to point to correct endpoint.
                    endpoint = _communicationManager.HostServer(endpoint);
                    _communicationManager.AcceptClientAsync();
                    return endpoint;
                }

            case ConnectionRole.Client:
                {
                    _communicationManager.SetupClientAsync(GetIpEndPoint(_connectionInfo.Endpoint));
                    return endpoint;
                }

            default:
                throw new NotImplementedException("Unsupported Connection Role");
        }
    }

    /// <inheritdoc/>
    public bool WaitForConnection(int connectionTimeout)
    {
        return _connectionInfo.Role == ConnectionRole.Client ? _communicationManager.WaitForServerConnection(connectionTimeout) : _communicationManager.WaitForClientConnection(connectionTimeout);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_connectionInfo.Role == ConnectionRole.Client)
                {
                    _communicationManager?.StopClient();
                }
                else
                {
                    _communicationManager?.StopServer();
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Converts a given string endpoint address to valid Ipv4, Ipv6 IPEndpoint
    /// </summary>
    /// <param name="endpointAddress">Input endpoint address</param>
    /// <returns>IPEndpoint from give string</returns>
    private IPEndPoint GetIpEndPoint(string endpointAddress)
    {
        return Uri.TryCreate(string.Concat("tcp://", endpointAddress), UriKind.Absolute, out Uri uri)
            ? new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port)
            : null;
    }
}