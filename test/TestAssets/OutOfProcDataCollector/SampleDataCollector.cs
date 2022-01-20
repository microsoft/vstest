﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        int i = 0;
        private DataCollectionSink dataCollectionSink;
        private DataCollectionEnvironmentContext context;
        private DataCollectionLogger logger;
        private readonly string tempDirectoryPath = Environment.GetEnvironmentVariable("TEST_ASSET_SAMPLE_COLLECTOR_PATH") ?? Path.GetTempPath();

        public override void Initialize(
            System.Xml.XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {

            events.TestHostLaunched += this.TestHostLaunched_Handler;
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
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "TestCaseStarted " + e.TestElement.FullyQualifiedName);
            var filename = Path.Combine(this.tempDirectoryPath, "testcasefilename" + i++ + ".txt");
            File.WriteAllText(filename, string.Empty);
            this.dataCollectionSink.SendFileAsync(e.Context, filename, true);
        }

        private void SessionStarted_Handler(object sender, SessionStartEventArgs args)
        {
            var filename = Path.Combine(this.tempDirectoryPath, "filename.txt");
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

        private void TestHostLaunched_Handler(object sender, TestHostLaunchedEventArgs e)
        {
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "TestHostLaunched " + e.TestHostProcessId);
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
        }

        protected override void Dispose(bool disposing)
        {
            this.logger.LogWarning(this.context.SessionDataCollectionContext, "Dispose called.");
        }
    }
}