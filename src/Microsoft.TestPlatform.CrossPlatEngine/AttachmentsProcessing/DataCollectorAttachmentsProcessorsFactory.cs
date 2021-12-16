using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing
{
    internal class DataCollectorAttachmentsProcessorsFactory : IDataCollectorAttachmentsProcessorsFactory
    {
        private static Uri CoverageUri = new Uri("datacollector://microsoft/CodeCoverage/2.0");
        private const string CoverageFriendlyName = "Code Coverage";
        private static ConcurrentDictionary<string, DataCollectorExtensionManager> dataCollectorExtensionManagerCache = new ConcurrentDictionary<string, DataCollectorExtensionManager>();

        public IReadOnlyDictionary<string, IDataCollectorAttachmentProcessor> Create(InvokedDataCollector[] invokedDataCollectors)
        {
            IDictionary<string, Tuple<string, IDataCollectorAttachmentProcessor>> datacollectorsAttachmentsProcessors = new Dictionary<string, Tuple<string, IDataCollectorAttachmentProcessor>>();
            bool addCodeCoverageAttachmentProcessors = true;

            if (invokedDataCollectors?.Length > 0)
            {
                // We order files by filename descending so in case of the same collector from the same nuget but with different versions, we'll run the newer version.
                // i.e. C:\Users\xxx\.nuget\packages\coverlet.collector
                // /3.0.2
                // /3.0.3
                // /3.1.0
                foreach (var invokedDataCollector in invokedDataCollectors.OrderByDescending(d => d.FilePath))
                {
                    EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: Analyzing data collector attachment processor Uri: {invokedDataCollector.Uri} AssemblyQualifiedName: {invokedDataCollector.AssemblyQualifiedName} FilePath: {invokedDataCollector.FilePath} HasAttachmentProcessor: {invokedDataCollector.HasAttachmentProcessor}");

                    // We'll merge using only one AQN in case of more "same processors" in different assembly.
                    if (!invokedDataCollector.HasAttachmentProcessor || datacollectorsAttachmentsProcessors.ContainsKey(invokedDataCollector.AssemblyQualifiedName))
                    {
                        continue;
                    }

                    // We cache extension locally by file path
                    var dataCollectorExtensionManager = dataCollectorExtensionManagerCache.GetOrAdd(invokedDataCollector.FilePath, DataCollectorExtensionManager.Create(invokedDataCollector.FilePath, true, TestSessionMessageLogger.Instance));
                    var dataCollectorExtension = dataCollectorExtensionManager.TryGetTestExtension(invokedDataCollector.Uri);
                    if (dataCollectorExtension?.Metadata.HasAttachmentProcessor == true)
                    {
                        Type attachmentProcessorType = ((DataCollectorConfig)dataCollectorExtension.TestPluginInfo).AttachmentsProcessorType;
                        IDataCollectorAttachmentProcessor dataCollectorAttachmentProcessorInstance = null;
                        try
                        {
                            dataCollectorAttachmentProcessorInstance = TestPluginManager.CreateTestExtension<IDataCollectorAttachmentProcessor>(attachmentProcessorType);
                        }
                        catch (Exception ex)
                        {
                            EqtTrace.Error($"DataCollectorAttachmentsProcessorsFactory: Failed during the creation of data collector attachment processor '{attachmentProcessorType.AssemblyQualifiedName}'\n{ex}");
                        }

                        if (dataCollectorAttachmentProcessorInstance != null && !datacollectorsAttachmentsProcessors.ContainsKey(attachmentProcessorType.AssemblyQualifiedName))
                        {
                            datacollectorsAttachmentsProcessors.Add(attachmentProcessorType.AssemblyQualifiedName, new Tuple<string, IDataCollectorAttachmentProcessor>(dataCollectorExtension.Metadata.FriendlyName, dataCollectorAttachmentProcessorInstance));

                            // If we found inside an extension the CodeCoverage attachment processor we use it(the most up to date) and we won't add the default one inside TP.
                            if (invokedDataCollector.Uri.AbsoluteUri == CoverageUri.AbsoluteUri)
                            {
                                EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: Attachment data processor for data collector with friendly name '{CoverageFriendlyName}' found '{attachmentProcessorType.AssemblyQualifiedName}' inside '{invokedDataCollector.FilePath}'");
                                addCodeCoverageAttachmentProcessors = false;
                            }
                        }
                    }
                    else
                    {
                        EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: DataCollectorExtension not found for uri '{invokedDataCollector.Uri}'");
                    }
                }
            }

            // TODO: we can add it always as last processor...in case of error of newer one at least this one will run
            if (addCodeCoverageAttachmentProcessors)
            {
                datacollectorsAttachmentsProcessors.Add(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, new Tuple<string, IDataCollectorAttachmentProcessor>(CoverageFriendlyName, new CodeCoverageDataAttachmentsHandler()));
            }

            var finalDatacollectorsAttachmentsProcessors = new Dictionary<string, IDataCollectorAttachmentProcessor>();
            foreach (var attachementProcessor in datacollectorsAttachmentsProcessors)
            {
                EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: valid data collector attachment processor found: '{attachementProcessor.Value.Item2.GetType().AssemblyQualifiedName}'");
                finalDatacollectorsAttachmentsProcessors.Add(attachementProcessor.Value.Item1, attachementProcessor.Value.Item2);
            }

            return new ReadOnlyDictionary<string, IDataCollectorAttachmentProcessor>(finalDatacollectorsAttachmentsProcessors);
        }
    }
}
