// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;

internal sealed class DataCollectorAttachmentProcessAssemblyLoadContext : IDataCollectorAttachmentProcessor, IDisposable
{
    private readonly PluginLoadContext _context;
    private readonly InvokedDataCollector _invokedDataCollector;
    private readonly IDataCollectorAttachmentProcessor _actualCollectorAttachmentProcessor;

    public DataCollectorAttachmentProcessAssemblyLoadContext(InvokedDataCollector invokedDataCollector!!, IMessageLogger dataCollectorAttachmentsProcessorsLogger!!)
    {
        _invokedDataCollector = invokedDataCollector;
        var collectorPath = invokedDataCollector.Uri.AbsolutePath;
        _context = new PluginLoadContext(collectorPath);


        var assembly = _context.LoadFromAssemblyPath(collectorPath);
        (_actualCollectorAttachmentProcessor, var type) = CreateFirstAssignableType(assembly);
        AssemblyQualifiedName = type.AssemblyQualifiedName;
        FriendlyName = type.Assembly.FullName;
    }

    public string? AssemblyQualifiedName { get; }
    public string? FriendlyName { get; }

    public bool SupportsIncrementalProcessing
        => _actualCollectorAttachmentProcessor.SupportsIncrementalProcessing;

    public bool LoadSucceded { get; internal set; }
    public bool HasAttachmentProcessor { get; internal set; }

    public IEnumerable<Uri> GetExtensionUris()
        => _actualCollectorAttachmentProcessor.GetExtensionUris();

    public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        => _actualCollectorAttachmentProcessor.ProcessAttachmentSetsAsync(configurationElement, attachments, progressReporter, logger, cancellationToken);

    public void Dispose()
        => _context.Unload();

    // REVIEW: Shall we warn if there are multiple available?
    private static (IDataCollectorAttachmentProcessor, Type) CreateFirstAssignableType(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(IDataCollectorAttachmentProcessor).IsAssignableFrom(type))
            {
                // REVIEW: Ok to throw?
                return Activator.CreateInstance(type) is not IDataCollectorAttachmentProcessor instance
                    ? throw new InvalidOperationException($"Cannot create instance of '{nameof(IDataCollectorAttachmentProcessor)}' for type '{type}'.")
                    : (instance, type);
            }
        }

        // REVIEW: Ok to throw?
        throw new InvalidOperationException($"Could not find any type compatible with '{nameof(IDataCollectorAttachmentProcessor)}' in '{assembly.FullName}'.");
    }
}

#endif
