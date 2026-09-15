// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer;

/// <summary>
/// Vstest console request sender for sending requests to vstest.console.exe
/// </summary>
internal partial class VsTestConsoleRequestSender : ITranslationLayerRequestSender
{
    private readonly ICommunicationManager _communicationManager;
    private readonly IDataSerializer _dataSerializer;
    private readonly ITestPlatformEventSource _testPlatformEventSource;
    private readonly ManualResetEvent _handShakeComplete = new(false);

    private bool _handShakeSuccessful;
    private int _protocolVersion = ProtocolVersioning.HighestSupportedVersion;

    /// <summary>
    /// Used to cancel blocking tasks associated with the vstest.console process.
    /// </summary>
    private CancellationTokenSource? _processExitCancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsTestConsoleRequestSender"/> class.
    /// </summary>
    public VsTestConsoleRequestSender()
        : this(
            new SocketCommunicationManager(),
            JsonDataSerializer.Instance,
            TestPlatformEventSource.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VsTestConsoleRequestSender"/> class.
    /// </summary>
    ///
    /// <param name="communicationManager">The communication manager.</param>
    /// <param name="dataSerializer">The data serializer.</param>
    /// <param name="testPlatformEventSource">The test platform event source.</param>
    internal VsTestConsoleRequestSender(
        ICommunicationManager communicationManager,
        IDataSerializer dataSerializer,
        ITestPlatformEventSource testPlatformEventSource)
    {
        _communicationManager = communicationManager;
        _dataSerializer = dataSerializer;
        _testPlatformEventSource = testPlatformEventSource;
    }


    #region ITranslationLayerRequestSender

    /// <inheritdoc/>
    public int InitializeCommunication()
    {
        EqtTrace.Info("VsTestConsoleRequestSender.InitializeCommunication: Started.");

        _processExitCancellationTokenSource = new CancellationTokenSource();
        _handShakeSuccessful = false;
        _handShakeComplete.Reset();
        int port = -1;
        try
        {
            port = _communicationManager.HostServer(new IPEndPoint(IPAddress.Loopback, 0)).Port;
            _communicationManager.AcceptClientAsync();

            Task.Run(() =>
            {
                _communicationManager.WaitForClientConnection(Timeout.Infinite);
                _handShakeSuccessful = HandShakeWithVsTestConsole();
                _handShakeComplete.Set();
            });
        }
        catch (Exception ex)
        {
            EqtTrace.Error(
                "VsTestConsoleRequestSender.InitializeCommunication: Error initializing communication with VstestConsole: {0}",
                ex);
            _handShakeComplete.Set();
        }

        EqtTrace.Info("VsTestConsoleRequestSender.InitializeCommunication: Ended.");

        return port;
    }

    /// <inheritdoc/>
    public bool WaitForRequestHandlerConnection(int clientConnectionTimeout)
    {
        var waitSuccess = _handShakeComplete.WaitOne(clientConnectionTimeout);
        return waitSuccess && _handShakeSuccessful;
    }

    /// <inheritdoc/>
    public async Task<int> InitializeCommunicationAsync(int clientConnectionTimeout)
    {
        EqtTrace.Info($"VsTestConsoleRequestSender.InitializeCommunicationAsync: Started with client connection timeout {clientConnectionTimeout} milliseconds.");

        _processExitCancellationTokenSource = new CancellationTokenSource();
        _handShakeSuccessful = false;
        _handShakeComplete.Reset();
        int port = -1;
        try
        {
            port = _communicationManager.HostServer(new IPEndPoint(IPAddress.Loopback, 0)).Port;
            var timeoutSource = new CancellationTokenSource(clientConnectionTimeout);
            await Task.Run(() =>
                _communicationManager.AcceptClientAsync(), timeoutSource.Token).ConfigureAwait(false);

            _handShakeSuccessful = await HandShakeWithVsTestConsoleAsync().ConfigureAwait(false);
            _handShakeComplete.Set();
        }
        catch (Exception ex)
        {
            EqtTrace.Error(
                "VsTestConsoleRequestSender.InitializeCommunicationAsync: Error initializing communication with VstestConsole: {0}",
                ex);
            _handShakeComplete.Set();
        }

        EqtTrace.Info("VsTestConsoleRequestSender.InitializeCommunicationAsync: Ended.");

        return _handShakeSuccessful ? port : -1;
    }

    /// <inheritdoc/>
    public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
    {
        EqtTrace.Info($"VsTestConsoleRequestSender.InitializeExtensions: Initializing extensions with additional extensions path {string.Join(",", pathToAdditionalExtensions.ToList())}.");

        _communicationManager.SendMessage(
            MessageType.ExtensionsInitialize,
            pathToAdditionalExtensions,
            _protocolVersion);
    }

    /// <inheritdoc/>
    public void OnProcessExited()
    {
        _processExitCancellationTokenSource?.Cancel();
    }

    /// <inheritdoc/>
    public void Close()
    {
        Dispose();
    }

    /// <inheritdoc/>
    public void EndSession()
    {
        _communicationManager.SendMessage(MessageType.SessionEnd);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _communicationManager?.StopServer();
    }

    #endregion

    private bool HandShakeWithVsTestConsole()
    {
        var message = _communicationManager.ReceiveMessage();

        if (message?.MessageType != MessageType.SessionConnected)
        {
            EqtTrace.Error(
                "VsTestConsoleRequestSender.HandShakeWithVsTestConsole: SessionConnected Message Expected but different message received: Received MessageType: {0}",
                message?.MessageType);
            return false;
        }

        _communicationManager.SendMessage(
            MessageType.VersionCheck,
            _protocolVersion);

        message = _communicationManager.ReceiveMessage();
        var success = false;

        if (message?.MessageType == MessageType.VersionCheck)
        {
            _protocolVersion = _dataSerializer
                .DeserializePayload<int>(message);
            success = true;
        }
        else if (message?.MessageType == MessageType.ProtocolError)
        {
            // TODO : Payload for ProtocolError needs to finalized.
            EqtTrace.Error(
                "VsTestConsoleRequestSender.HandShakeWithVsTestConsole: Version Check failed. ProtolError was received from the runner");
        }
        else
        {
            EqtTrace.Error(
                "VsTestConsoleRequestSender.HandShakeWithVsTestConsole: VersionCheck Message Expected but different message received: Received MessageType: {0}",
                message?.MessageType);
        }

        return success;
    }

    private async Task<bool> HandShakeWithVsTestConsoleAsync()
    {
        TPDebug.Assert(_processExitCancellationTokenSource is not null, "_processExitCancellationTokenSource is null");
        var message = await _communicationManager.ReceiveMessageAsync(
            _processExitCancellationTokenSource.Token).ConfigureAwait(false);

        if (message?.MessageType != MessageType.SessionConnected)
        {
            EqtTrace.Error(
                "VsTestConsoleRequestSender.HandShakeWithVsTestConsoleAsync: SessionConnected Message Expected but different message received: Received MessageType: {0}",
                message?.MessageType);
            return false;
        }

        _communicationManager.SendMessage(
            MessageType.VersionCheck,
            _protocolVersion);

        message = await _communicationManager.ReceiveMessageAsync(
            _processExitCancellationTokenSource.Token).ConfigureAwait(false);

        var success = false;
        if (message?.MessageType == MessageType.VersionCheck)
        {
            _protocolVersion = _dataSerializer.DeserializePayload<int>(message);
            success = true;
        }
        else if (message?.MessageType == MessageType.ProtocolError)
        {
            // TODO : Payload for ProtocolError needs to finalized.
            EqtTrace.Error(
                "VsTestConsoleRequestSender.HandShakeWithVsTestConsoleAsync: Version Check failed. ProtolError was received from the runner");
        }
        else
        {
            EqtTrace.Error(
                "VsTestConsoleRequestSender.HandShakeWithVsTestConsoleAsync: VersionCheck Message Expected but different message received: Received MessageType: {0}",
                message?.MessageType);
        }

        return success;
    }
}
