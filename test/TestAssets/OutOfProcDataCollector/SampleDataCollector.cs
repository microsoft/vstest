// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OutOfProcDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    [DataCollectorFriendlyName("SampleDataCollector")]
    [DataCollectorTypeUri("my://sample/datacollector")]
    public class SampleDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
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
            events.SessionStart += this.SessionStarted_Handler;
            events.SessionEnd += this.SessionEnded_Handler;
            events.TestCaseStart += this.Events_TestCaseStart;
            events.TestCaseEnd += this.Events_TestCaseEnd;
            this.dataCollectionSink = dataSink;
            this.context = environmentContext;
            this.logger = logger;
        }

        private void Events_TestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "TestCaseEnded " + e.TestCaseName);
        }

        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "TestCaseStarted " + e.TestCaseName);
        }

        private void SessionStarted_Handler(object sender, SessionStartEventArgs args)
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);
            this.dataCollectionSink.SendFileAsync(this.context.SessionDataCollectionContext, filename, true);
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "SessionStarted");
        }

        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            this.logger.LogError(this.context.SessionDataCollectionContext, new Exception("my exception"));
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "my warning");
            this.logger.LogException(this.context.SessionDataCollectionContext, new Exception("abc"), DataCollectorMessageLevel.Error);

            this.logger.LogWarning(this.context.SessionDataCollectionContext, "SessionEnded");
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
        }
    }
}