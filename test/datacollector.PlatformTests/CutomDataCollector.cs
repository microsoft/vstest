// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.PlatformTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    [DataCollectorFriendlyName("CustomDataCollector")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        private DataCollectionSink dataCollectionSink;
        private DataCollectionEnvironmentContext context;
        private DataCollectionLogger logger;

        public override void Initialize(
            System.Xml.XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            events.SessionStart += new EventHandler<SessionStartEventArgs>(this.SessionStarted_Handler);
            events.SessionEnd += new EventHandler<SessionEndEventArgs>(this.SessionEnded_Handler);
            this.dataCollectionSink = dataSink;
            this.context = environmentContext;
            this.logger = logger;
        }

        private void SessionStarted_Handler(object sender, SessionStartEventArgs args)
        {
            var filename = Path.Combine(FileUtility.GetAppDomainBaseDir(), "filename.txt");
            File.WriteAllText(filename, string.Empty);
            this.dataCollectionSink.SendFileAsync(context.SessionDataCollectionContext, filename, true);
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "SessionEnded");
        }

        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            //logger.LogError(this.context.SessionDataCollectionContext, new Exception("my exception"));
            //logger.LogWarning(this.context.SessionDataCollectionContext, "my arning");
            //logger.LogException(context.SessionDataCollectionContext, new Exception("abc"), DataCollectorMessageLevel.Error);

            this.logger.LogWarning(this.context.SessionDataCollectionContext, "SessionEnded");
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
        }
    }
}
