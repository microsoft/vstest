using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing
{
    internal class DataCollectorAttachmentsProcessorsFactory : IDataCollectorAttachmentsProcessorsFactory
    {
        private static Uri CoverageUri = new Uri("datacollector://microsoft/CodeCoverage/2.0");
        private const string CoverageFriendlyName = "Code Coverage";

        public IReadOnlyDictionary<string, IConfigurableDataCollectorAttachmentProcessor> Create(InvokedDataCollector[] invokedDataCollectors)
        {
            IDictionary<string, Tuple<string, IConfigurableDataCollectorAttachmentProcessor>> datacollectorsAttachmentsProcessors = new Dictionary<string, Tuple<string, IConfigurableDataCollectorAttachmentProcessor>>();
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

                    var dataCollectorExtensionManager = DataCollectorExtensionManager.Create(invokedDataCollector.FilePath, true, TestSessionMessageLogger.Instance);
                    var dataCollectorExtension = dataCollectorExtensionManager.TryGetTestExtension(invokedDataCollector.Uri);
                    if (dataCollectorExtension?.Metadata.HasAttachmentProcessor == true)
                    {
                        Type attachmentProcessorType = ((DataCollectorConfig)dataCollectorExtension.TestPluginInfo).AttachmentsProcessorType;
                        IConfigurableDataCollectorAttachmentProcessor dataCollectorAttachmentProcessorInstance = null;
                        try
                        {
                            dataCollectorAttachmentProcessorInstance = TestPluginManager.CreateTestExtension<IConfigurableDataCollectorAttachmentProcessor>(attachmentProcessorType);
                        }
                        catch (Exception ex)
                        {
                            EqtTrace.Error($"DataCollectorAttachmentsProcessorsFactory: Failed during the creation of data collector attachment processor '{attachmentProcessorType.AssemblyQualifiedName}'\n{ex}");
                        }

                        if (dataCollectorAttachmentProcessorInstance != null && !datacollectorsAttachmentsProcessors.ContainsKey(attachmentProcessorType.AssemblyQualifiedName))
                        {
                            datacollectorsAttachmentsProcessors.Add(attachmentProcessorType.AssemblyQualifiedName, new Tuple<string, IConfigurableDataCollectorAttachmentProcessor>(dataCollectorExtension.Metadata.FriendlyName, dataCollectorAttachmentProcessorInstance));

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

            if (addCodeCoverageAttachmentProcessors)
            {
                datacollectorsAttachmentsProcessors.Add(typeof(CodeCoverageDataAttachmentsHandler).AssemblyQualifiedName, new Tuple<string, IConfigurableDataCollectorAttachmentProcessor>(CoverageFriendlyName, new CodeCoverageDataAttachmentsHandler()));
            }

            var finalDatacollectorsAttachmentsProcessors = new Dictionary<string, IConfigurableDataCollectorAttachmentProcessor>();
            foreach (var attachementProcessor in datacollectorsAttachmentsProcessors)
            {
                EqtTrace.Info($"DataCollectorAttachmentsProcessorsFactory: valid data collector attachment processor found: '{attachementProcessor.Value.Item2.GetType().AssemblyQualifiedName}'");
                finalDatacollectorsAttachmentsProcessors.Add(attachementProcessor.Value.Item1, attachementProcessor.Value.Item2);
            }

            return new ReadOnlyDictionary<string, IConfigurableDataCollectorAttachmentProcessor>(finalDatacollectorsAttachmentsProcessors);
        }
    }
}
