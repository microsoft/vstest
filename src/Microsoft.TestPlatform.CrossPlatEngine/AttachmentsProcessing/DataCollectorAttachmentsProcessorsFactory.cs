// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
#if NETFRAMEWORK || NETCOREAPP3_0_OR_GREATER
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
#endif

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;

internal class DataCollectorAttachmentsProcessorsFactory : IDataCollectorAttachmentsProcessorsFactory
{
    private const string CoverageFriendlyName = "Code Coverage";
    private static readonly ConcurrentDictionary<string, DataCollectorExtensionManager> DataCollectorExtensionManagerCache = new();

    public DataCollectorAttachmentProcessor[] Create(InvokedDataCollector[]? invokedDataCollectors, IMessageLogger? logger)
    {
        Dictionary<string, Tuple<string, IDataCollectorAttachmentProcessor>> datacollectorsAttachmentsProcessors = new();

        if (invokedDataCollectors?.Length > 0)
        {
            // We order files by filename descending so in case of the same collector from the same nuget but with different versions, we'll run the newer version.
            // i.e. C:\Users\xxx\.nuget\packages\coverlet.collector
            // /3.0.2
            // /3.0.3
            // /3.1.0
            foreach (var invokedDataCollector in invokedDataCollectors.OrderByDescending(d => d.FilePath))
            {
                // We'll merge using only one AQN in case of more "same processors" in different assembly.
                if (!invokedDataCollector.HasAttachmentProcessor)
                {
                    continue;
                }

                EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: Analyzing data collector attachment processor Uri: {invokedDataCollector.Uri} AssemblyQualifiedName: {invokedDataCollector.AssemblyQualifiedName} FilePath: {invokedDataCollector.FilePath} AttachmentProcessorLoaded: {invokedDataCollector.HasAttachmentProcessor}");

#if NETFRAMEWORK || NETCOREAPP3_0_OR_GREATER
                // If we're in design mode we need to load the extension inside a different AppDomain to avoid to lock extension file containers.
                if (RunSettingsHelper.Instance.IsDesignMode)
                {
                    try
                    {
#if NETFRAMEWORK
                        var wrapper = new DataCollectorAttachmentProcessorAppDomain(invokedDataCollector, logger);
#else
                        var wrapper = new DataCollectorAttachmentProcessAssemblyLoadContext(invokedDataCollector, logger);
#endif

                        AddAttachmentProcessor(invokedDataCollector, wrapper.AttachmentProcessorLoaded, wrapper.AssemblyQualifiedName!, wrapper.FriendlyName!, wrapper);
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"DataCollectorAttachmentsProcessorsFactory: Failed during the creation of data collector attachment processor '{invokedDataCollector.AssemblyQualifiedName}'\n{ex}";
                        EqtTrace.Error(errorMsg);
                        logger?.SendMessage(TestMessageLevel.Error, errorMsg);
                    }
                }
                else
                {
#endif
                    // We cache extension locally by file path
                    var attachmentProcessorLoaded = TryLoadExtension(
                        invokedDataCollector,
                        filePath => DataCollectorExtensionManagerCache.GetOrAdd(filePath, DataCollectorExtensionManager.Create(filePath, true, TestSessionMessageLogger.Instance)),
                        msg => EqtTrace.Info(msg),
                        errorMsg =>
                        {
                            EqtTrace.Error(errorMsg);
                            logger?.SendMessage(TestMessageLevel.Error, errorMsg);
                        },
                        out var friendlyName,
                        out var assemblyQualifiedName,
                        out var attachmentProcessor);

                    if (attachmentProcessorLoaded)
                    {
                        AddAttachmentProcessor(invokedDataCollector, attachmentProcessorLoaded: true, assemblyQualifiedName!, friendlyName, attachmentProcessor!);
                    }
#if NETFRAMEWORK || NETCOREAPP3_0_OR_GREATER
                }
#endif
            }
        }

        // We provide the implementation of CodeCoverageDataAttachmentsHandler through nuget package, but in case of absent registration or if for some reason
        // the attachment processor from package fails we fallback to the default implementation.
        if (typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName is string attachmentHandlerAssemblyQualifiedName
            && !datacollectorsAttachmentsProcessors.ContainsKey(attachmentHandlerAssemblyQualifiedName))
        {
            datacollectorsAttachmentsProcessors.Add(attachmentHandlerAssemblyQualifiedName, new Tuple<string, IDataCollectorAttachmentProcessor>(CoverageFriendlyName, new CodeCoverageDataAttachmentsHandler()));
            EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: Collector attachment processor '{typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName}' for the data collector with friendly name '{CoverageFriendlyName}' added to the 'run list'");
        }

        var finalDatacollectorsAttachmentsProcessors = new List<DataCollectorAttachmentProcessor>();
        foreach (var attachementProcessor in datacollectorsAttachmentsProcessors)
        {
            EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: Valid data collector attachment processor found: '{attachementProcessor.Key}'");
            finalDatacollectorsAttachmentsProcessors.Add(new DataCollectorAttachmentProcessor(attachementProcessor.Value.Item1, attachementProcessor.Value.Item2));
        }

        return finalDatacollectorsAttachmentsProcessors.ToArray();

        // Local functions
        void AddAttachmentProcessor(InvokedDataCollector dataCollector, bool attachmentProcessorLoaded, string assemblyQualifiedName, string friendlyName,
            IDataCollectorAttachmentProcessor processor)
        {
            if (!attachmentProcessorLoaded)
            {
                EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory.AddAttachmentProcessor: DataCollectorExtension not found for uri '{dataCollector.Uri}'.");
                return;
            }

            if (!datacollectorsAttachmentsProcessors.ContainsKey(assemblyQualifiedName))
            {
                datacollectorsAttachmentsProcessors.Add(assemblyQualifiedName, new Tuple<string, IDataCollectorAttachmentProcessor>(friendlyName, processor));
                EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory.AddAttachmentProcessor: Collector attachment processor '{assemblyQualifiedName}' from file '{dataCollector.FilePath}' added to the 'run list'.");
                return;
            }

            if (processor is IDisposable disposable)
            {
                // If we already registered same IDataCollectorAttachmentProcessor and we are using AppDomain or AssemblyLoadContext,
                // let's unload the no longer used resource.
                EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory.AddAttachmentProcessor: Unloading unused context for '{friendlyName}'/");
                disposable.Dispose();
            }
        }
    }

    internal static bool TryLoadExtension(InvokedDataCollector dataCollector, Func<string, DataCollectorExtensionManager> createExtensionManager,
        Action<string> traceInfo, Action<string> onError, out string friendlyName, out string? assemblyQualifiedName,
        [NotNullWhen(returnValue: true)] out IDataCollectorAttachmentProcessor? attachmentProcessor)
    {
        var dataCollectorExtensionManager = createExtensionManager(dataCollector.FilePath);
        var dataCollectorExtension = dataCollectorExtensionManager.TryGetTestExtension(dataCollector.Uri);

        friendlyName = dataCollectorExtension.Metadata.FriendlyName;
        assemblyQualifiedName = null;
        attachmentProcessor = null;

        if (dataCollectorExtension?.Metadata.HasAttachmentProcessor != true)
        {
            traceInfo($"DataCollectorAttachmentsProcessorsFactory.TryLoadExtension: DataCollectorExtension not found for uri '{dataCollector.Uri}'");
            return false;
        }

        Type attachmentProcessorType = ((DataCollectorConfig)dataCollectorExtension!.TestPluginInfo).AttachmentsProcessorType;
        assemblyQualifiedName = attachmentProcessorType.AssemblyQualifiedName;
        try
        {
            attachmentProcessor = TestPluginManager.CreateTestExtension<IDataCollectorAttachmentProcessor>(attachmentProcessorType);
            traceInfo($"DataCollectorAttachmentsProcessorsFactory.TryLoadExtension: Creation of collector attachment processor '{assemblyQualifiedName}' from file '{dataCollector.FilePath}' succeded.");
            return true;
        }
        catch (Exception ex)
        {
            onError($"DataCollectorAttachmentsProcessorsFactory.TryLoadExtension: Failed during the creation of data collector attachment processor '{assemblyQualifiedName}'\n{ex}");
        }

        return false;
    }
}
