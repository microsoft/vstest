// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;

internal sealed class DataCollectorAttachmentProcessAssemblyLoadContext : IDataCollectorAttachmentProcessor, IDisposable
{
    private readonly PluginLoadContext _context;
    private readonly IDataCollectorAttachmentProcessor? _actualCollectorAttachmentProcessor;

    public DataCollectorAttachmentProcessAssemblyLoadContext(InvokedDataCollector invokedDataCollector!!, IMessageLogger? logger)
    {
        _context = new PluginLoadContext(invokedDataCollector.Uri.ToString(), invokedDataCollector.FilePath);

        DataCollectorAttachmentsProcessorsFactory.TryLoadExtension(
            invokedDataCollector.FilePath,
            invokedDataCollector.Uri,
            DataCollectorExtensionManager.Create(invokedDataCollector.FilePath, true, TestSessionMessageLogger.Instance, new TestPluginCache(_context)),
            msg => EqtTrace.Info(msg),
            errorMsg =>
            {
                EqtTrace.Error(errorMsg);
                logger?.SendMessage(TestMessageLevel.Error, errorMsg);
            },
            out var friendlyName,
            out var assemblyQualifiedName,
            out _actualCollectorAttachmentProcessor);


        AssemblyQualifiedName = assemblyQualifiedName;
        FriendlyName = friendlyName;
    }

    public string? AssemblyQualifiedName { get; }
    public string? FriendlyName { get; }

    public bool SupportsIncrementalProcessing
        => _actualCollectorAttachmentProcessor == null
            ? throw new InvalidOperationException($"There is no loaded collector attachment processor, '{nameof(SupportsIncrementalProcessing)}' should not have been called.")
            : _actualCollectorAttachmentProcessor.SupportsIncrementalProcessing;

    public bool AttachmentProcessorLoaded => _actualCollectorAttachmentProcessor != null;

    public IEnumerable<Uri> GetExtensionUris()
        => _actualCollectorAttachmentProcessor == null
            ? throw new InvalidOperationException($"There is no loaded collector attachment processor, '{nameof(GetExtensionUris)}' should not have been called.")
            : _actualCollectorAttachmentProcessor.GetExtensionUris();

    public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments,
        IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        => _actualCollectorAttachmentProcessor == null
            ? throw new InvalidOperationException($"There is no loaded collector attachment processor, '{nameof(ProcessAttachmentSetsAsync)}' should not have been called.")
            : _actualCollectorAttachmentProcessor.ProcessAttachmentSetsAsync(configurationElement, attachments, progressReporter, logger, cancellationToken);

    public void Dispose()
        => _context.Dispose();
}

#endif
