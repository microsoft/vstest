﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;

/// <summary>
/// This class is the "container" for the real IDataCollectorAttachmentProcessor implementation.
/// It tries to load the extension and it receives calls from the DataCollectorAttachmentProcessorAppDomain that
/// acts as a proxy for the main AppDomain(the runner one).
/// </summary>
internal class DataCollectorAttachmentProcessorRemoteWrapper : MarshalByRefObject
{
    private readonly AnonymousPipeServerStream _pipeServerStream = new(PipeDirection.Out, HandleInheritability.None);
    private readonly object _pipeClientLock = new();
    private readonly string _pipeShutdownMessagePrefix;

    private IDataCollectorAttachmentProcessor? _dataCollectorAttachmentProcessorInstance;

    private CancellationTokenSource? _processAttachmentCts;

    public string? AssemblyQualifiedName { get; private set; }

    public string? FriendlyName { get; private set; }

    public bool AttachmentProcessorLoaded => _dataCollectorAttachmentProcessorInstance != null;

    public DataCollectorAttachmentProcessorRemoteWrapper(string pipeShutdownMessagePrefix!!)
    {
        _pipeShutdownMessagePrefix = pipeShutdownMessagePrefix;
    }

    public string GetClientHandleAsString() => _pipeServerStream.GetClientHandleAsString();

    public bool SupportsIncrementalProcessing => _dataCollectorAttachmentProcessorInstance?.SupportsIncrementalProcessing == true;

    public Uri[]? GetExtensionUris() => _dataCollectorAttachmentProcessorInstance?.GetExtensionUris()?.ToArray();

    public string ProcessAttachment(
        string configurationElement,
        string attachments)
    {
        var doc = new XmlDocument();
        doc.LoadXml(configurationElement);
        AttachmentSet[] attachmentSets = JsonDataSerializer.Instance.Deserialize<AttachmentSet[]>(attachments);
        SynchronousProgress progress = new(Report);
        _processAttachmentCts = new CancellationTokenSource();

        ICollection<AttachmentSet> attachmentsResult =
            Task.Run(async () => await _dataCollectorAttachmentProcessorInstance!.ProcessAttachmentSetsAsync(
            doc.DocumentElement,
            attachmentSets,
            progress,
            new MessageLogger(this, nameof(ProcessAttachment)),
            _processAttachmentCts.Token))
            // We cannot marshal Task so we need to block the thread until the end of the processing
            .ConfigureAwait(false).GetAwaiter().GetResult();

        return JsonDataSerializer.Instance.Serialize(attachmentsResult.ToArray());
    }

    public void CancelProcessAttachment() => _processAttachmentCts?.Cancel();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LoadExtension(string dataCollectorFilePath, Uri dataCollectorUri)
    {
        DataCollectorAttachmentsProcessorsFactory.TryLoadExtension(
            dataCollectorFilePath,
            dataCollectorUri,
            DataCollectorExtensionManager.Create(dataCollectorFilePath, true, new MessageLogger(this, nameof(LoadExtension))),
            TraceInfo,
            errorMsg =>
            {
                TraceError(errorMsg);
                SendMessage(nameof(LoadExtension), TestMessageLevel.Error, errorMsg);
            },
            out var friendlyName,
            out var assemblyQualifiedName,
            out _dataCollectorAttachmentProcessorInstance);

        AssemblyQualifiedName = assemblyQualifiedName;
        FriendlyName = friendlyName;
    }

    private void TraceError(string message) => Trace(AppDomainPipeMessagePrefix.EqtTraceError, message);

    private void TraceInfo(string message) => Trace(AppDomainPipeMessagePrefix.EqtTraceInfo, message);

    private void Trace(string traceType, string message)
    {
        lock (_pipeClientLock)
        {
            WriteToPipe($"{traceType}|{message}");
        }
    }

    private void Report(int value)
    {
        lock (_pipeClientLock)
        {
            WriteToPipe($"{AppDomainPipeMessagePrefix.Report}|{value}");
        }
    }

    private void SendMessage(string origin, TestMessageLevel messageLevel, string message)
    {
        lock (_pipeClientLock)
        {
            WriteToPipe($"{origin}.TestMessageLevel.{messageLevel}|{message}");
        }
    }

    private void WriteToPipe(string message)
    {
        using StreamWriter sw = new(_pipeServerStream, Encoding.Default, 1024, true);
        sw.AutoFlush = true;
        // We want to keep the protocol very simple and text message oriented.
        // On the read side we do ReadLine() to simplify the parsing and
        // for this reason we remove the \n to null terminator and we'll aggregate on client side.
        sw.WriteLine(message.Replace(Environment.NewLine, "\0").Replace("\n", "\0"));
        _pipeServerStream.Flush();
        _pipeServerStream.WaitForPipeDrain();
    }

    private class MessageLogger : IMessageLogger
    {
        private readonly string _name;
        private readonly DataCollectorAttachmentProcessorRemoteWrapper _wrapper;

        public MessageLogger(DataCollectorAttachmentProcessorRemoteWrapper wrapper!!, string name!!)
        {
            _wrapper = wrapper;
            _name = name;
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
            => _wrapper.SendMessage(_name, testMessageLevel, message);
    }

    private class SynchronousProgress : IProgress<int>
    {
        private readonly Action<int> _report;

        public SynchronousProgress(Action<int> report!!) => _report = report;

        public void Report(int value) => _report(value);
    }

    public void Dispose()
    {
        _processAttachmentCts?.Dispose();
        // Send shutdown message to gracefully close the client.
        WriteToPipe($"{_pipeShutdownMessagePrefix}_{AppDomain.CurrentDomain.FriendlyName}");
        _pipeServerStream.Dispose();
        (_dataCollectorAttachmentProcessorInstance as IDisposable)?.Dispose();
    }
}

#endif
