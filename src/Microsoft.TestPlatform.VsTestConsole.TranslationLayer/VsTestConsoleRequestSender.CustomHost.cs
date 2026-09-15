// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

using TranslationLayerResources = Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Resources.Resources;

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer;

/// <summary>
/// Custom test host launch/debugger attach and telemetry event handling shared by the
/// discovery and execution paths of <see cref="VsTestConsoleRequestSender"/>.
/// </summary>
internal partial class VsTestConsoleRequestSender
{
    private Message TryReceiveMessage()
    {
        return TryReceiveMessageAsync().GetAwaiter().GetResult();
    }

    private async Task<Message> TryReceiveMessageAsync()
    {
        // TODO: Rework logic of this class to avoid relying on throwing/catching exceptions:
        // - NRE on null _processExitCancellationTokenSource
        // - TransationLayerException on null message
        Message? message = await _communicationManager.ReceiveMessageAsync(_processExitCancellationTokenSource!.Token)
            .ConfigureAwait(false);

        return message ?? throw new TransationLayerException(TranslationLayerResources.FailedToReceiveMessage);
    }

    private void HandleCustomHostLaunch(ITestHostLauncher? customHostLauncher, Message message)
    {
        var ackPayload = new CustomHostLaunchAckPayload()
        {
            HostProcessId = -1,
            ErrorMessage = null
        };

        try
        {
            var testProcessStartInfo = _dataSerializer.DeserializePayload<TestProcessStartInfo>(message);

            ackPayload.HostProcessId = customHostLauncher?.LaunchTestHost(testProcessStartInfo!) ?? -1;
        }
        catch (Exception ex)
        {
            EqtTrace.Error("Error while launching custom host: {0}", ex);

            // Vstest.console will send the abort message properly while cleaning up all the
            // flow, so do not abort here.
            // Let the ack go through and let vstest.console handle the error.
            ackPayload.ErrorMessage = ex.Message;
        }
        finally
        {
            // Always unblock the vstest.console thread which is indefinitely waiting on this
            // ACK.
            _communicationManager.SendMessage(
                MessageType.CustomTestHostLaunchCallback,
                ackPayload,
                _protocolVersion);
        }
    }

    private void AttachDebuggerToProcess(ITestHostLauncher? customHostLauncher, Message message)
    {
        var ackPayload = new EditorAttachDebuggerAckPayload()
        {
            Attached = false,
            ErrorMessage = null
        };

        try
        {
            // Handle EditorAttachDebugger2.
            if (message.MessageType == MessageType.EditorAttachDebugger2)
            {
                var attachDebuggerPayload = _dataSerializer.DeserializePayload<EditorAttachDebuggerPayload>(message);
                TPDebug.Assert(attachDebuggerPayload is not null, "attachDebuggerPayload is null");
                switch (customHostLauncher)
                {
                    case ITestHostLauncher3 launcher3:
                        var attachDebuggerInfo = new AttachDebuggerInfo
                        {
                            ProcessId = attachDebuggerPayload.ProcessID,
                            TargetFramework = attachDebuggerPayload.TargetFramework,
                            Sources = attachDebuggerPayload.Sources,
                        };
                        ackPayload.Attached = launcher3.AttachDebuggerToProcess(attachDebuggerInfo, CancellationToken.None);
                        break;
                    case ITestHostLauncher2 launcher2:
                        ackPayload.Attached = launcher2.AttachDebuggerToProcess(attachDebuggerPayload.ProcessID);
                        break;
                    default:
                        // TODO: Maybe we should do something, but the rest of the story is broken, so it's better to not block users.
                        break;
                }
            }

            // Handle EditorAttachDebugger.
            if (message.MessageType == MessageType.EditorAttachDebugger)
            {
                var pid = _dataSerializer.DeserializePayload<int>(message);

                switch (customHostLauncher)
                {
                    case ITestHostLauncher3 launcher3:
                        ackPayload.Attached = launcher3.AttachDebuggerToProcess(new AttachDebuggerInfo { ProcessId = pid }, CancellationToken.None);
                        break;
                    case ITestHostLauncher2 launcher2:
                        ackPayload.Attached = launcher2.AttachDebuggerToProcess(pid);
                        break;
                    default:
                        // TODO: Maybe we should do something, but the rest of the story is broken, so it's better to not block users.
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            EqtTrace.Error("VsTestConsoleRequestSender.AttachDebuggerToProcess: Error while attaching debugger to process: {0}", ex);

            // vstest.console will send the abort message properly while cleaning up all the
            // flow, so do not abort here.
            // Let the ack go through and let vstest.console handle the error.
            ackPayload.ErrorMessage = ex.Message;
        }
        finally
        {
            // Always unblock the vstest.console thread which is indefintitely waiting on this
            // ACK.
            _communicationManager.SendMessage(
                MessageType.EditorAttachDebuggerCallback,
                ackPayload,
                _protocolVersion);
        }
    }

    private void HandleTelemetryEvent(ITelemetryEventsHandler telemetryEventsHandler, Message message)
    {
        try
        {
            TelemetryEvent? telemetryEvent = _dataSerializer.DeserializePayload<TelemetryEvent>(message);
            if (telemetryEvent is not null)
            {
                telemetryEventsHandler.HandleTelemetryEvent(telemetryEvent);
            }
        }
        catch (Exception ex)
        {
            EqtTrace.Error("VsTestConsoleRequestSender.HandleTelemetryEvent: Error while handling telemetry event: {0}", ex);
        }
    }
}
