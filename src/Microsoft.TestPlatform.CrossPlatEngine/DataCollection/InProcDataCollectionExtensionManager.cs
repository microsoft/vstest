// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The in process data collection extension manager.
    /// </summary>
    internal class InProcDataCollectionExtensionManager
    {
        internal IDictionary<string, IInProcDataCollector> InProcDataCollectors;

        private IDataCollectionSink inProcDataCollectionSink;

        private const string DataCollectorEndsWithPattern = @"Collector.dll";

        private string defaultCodeBase;

        private List<string> codeBasePaths;

        private IFileHelper fileHelper;

        /// <summary>
        /// Loaded in-proc datacollectors collection
        /// </summary>
        private IEnumerable<DataCollectorSettings> inProcDataCollectorSettingsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="InProcDataCollectionExtensionManager"/> class.
        /// </summary>
        /// <param name="runSettings">
        /// The run settings.
        /// </param>
        /// <param name="testEventsPublisher">
        /// The data collection test case event manager.
        /// </param>
        /// <param name="defaultCodeBase">
        /// The default code base to be used by in-proc data collector
        /// </param>
        public InProcDataCollectionExtensionManager(string runSettings, ITestEventsPublisher testEventsPublisher, string defaultCodeBase, TestPluginCache testPluginCache)
            : this(runSettings, testEventsPublisher, defaultCodeBase, testPluginCache, new FileHelper())
        {}

        protected InProcDataCollectionExtensionManager(string runSettings, ITestEventsPublisher testEventsPublisher, string defaultCodeBase, TestPluginCache testPluginCache, IFileHelper fileHelper)
        {
            this.InProcDataCollectors = new Dictionary<string, IInProcDataCollector>();
            this.inProcDataCollectionSink = new InProcDataCollectionSink();
            this.defaultCodeBase = defaultCodeBase;
            this.fileHelper = fileHelper;
            this.codeBasePaths = new List<string> { this.defaultCodeBase };

            // Get Datacollector code base paths from test plugin cache
            var extensionPaths = testPluginCache.GetExtensionPaths(DataCollectorEndsWithPattern);
            foreach (var extensionPath in extensionPaths)
            {
                this.codeBasePaths.Add(Path.GetDirectoryName(extensionPath));
            }

            // Initialize InProcDataCollectors
            this.InitializeInProcDataCollectors(runSettings);

            if (this.IsInProcDataCollectionEnabled)
            {
                testEventsPublisher.TestCaseEnd += this.TriggerTestCaseEnd;
                testEventsPublisher.TestCaseStart += this.TriggerTestCaseStart;
                testEventsPublisher.TestResult += this.TriggerUpdateTestResult;
                testEventsPublisher.SessionStart += this.TriggerTestSessionStart;
                testEventsPublisher.SessionEnd += this.TriggerTestSessionEnd;
            }
        }

        /// <summary>
        /// Gets a value indicating whether is in-proc data collection enabled.
        /// </summary>
        public bool IsInProcDataCollectionEnabled { get; private set; }

        /// <summary>
        /// Creates data collector instance based on datacollector settings provided.
        /// </summary>
        /// <param name="dataCollectorSettings">
        /// Settings to be used for creating DataCollector.
        /// </param>
        /// <param name="interfaceTypeInfo">
        /// TypeInfo of datacollector.
        /// </param>
        /// <returns>
        /// The <see cref="IInProcDataCollector"/>.
        /// </returns>
        protected virtual IInProcDataCollector CreateDataCollector(string assemblyQualifiedName, string codebase, XmlElement configuration, TypeInfo interfaceTypeInfo)
        {
            var inProcDataCollector = new InProcDataCollector(
                codebase,
                assemblyQualifiedName,
                interfaceTypeInfo,
                configuration?.OuterXml);

            inProcDataCollector.LoadDataCollector(this.inProcDataCollectionSink);

            return inProcDataCollector;
        }

        /// <summary>
        /// The trigger test session start.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void TriggerTestSessionStart(object sender, SessionStartEventArgs e)
        {
            TestSessionStartArgs testSessionStartArgs = new TestSessionStartArgs(this.GetSessionStartProperties(e));
            this.TriggerInProcDataCollectionMethods(Constants.TestSessionStartMethodName, testSessionStartArgs);
        }

        /// <summary>
        /// The trigger session end.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void TriggerTestSessionEnd(object sender, SessionEndEventArgs e)
        {
            var testSessionEndArgs = new TestSessionEndArgs();
            this.TriggerInProcDataCollectionMethods(Constants.TestSessionEndMethodName, testSessionEndArgs);
        }

        /// <summary>
        /// The trigger test case start.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void TriggerTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            var testCaseStartArgs = new TestCaseStartArgs(e.TestElement);
            this.TriggerInProcDataCollectionMethods(Constants.TestCaseStartMethodName, testCaseStartArgs);
        }

        /// <summary>
        /// The trigger test case end.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void TriggerTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            var dataCollectionContext = new DataCollectionContext(e.TestElement);
            var testCaseEndArgs = new TestCaseEndArgs(dataCollectionContext, e.TestOutcome);
            this.TriggerInProcDataCollectionMethods(Constants.TestCaseEndMethodName, testCaseEndArgs);
        }

        /// <summary>
        /// Triggers the send test result method
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void TriggerUpdateTestResult(object sender, TestResultEventArgs e)
        {
            // Just set the cached in-proc data if already exists
            this.SetInProcDataCollectionDataInTestResult(e.TestResult);
        }

        /// <summary>
        /// Loads all the in-proc data collector dlls
        /// </summary>
        /// <param name="runSettings">
        /// The run Settings.
        /// </param>
        private void InitializeInProcDataCollectors(string runSettings)
        {
            try
            {
                // Check if runsettings contains in-proc datacollector element
                var inProcDataCollectionRunSettings = XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(runSettings);
                var inProcDataCollectionSettingsPresentInRunSettings = inProcDataCollectionRunSettings?.IsCollectionEnabled ?? false;

                // Verify if it has any valid in-proc datacollectors or just a dummy element
                inProcDataCollectionSettingsPresentInRunSettings = inProcDataCollectionSettingsPresentInRunSettings &&
                    inProcDataCollectionRunSettings.DataCollectorSettingsList.Any();

                // Initialize if we have at least one
                if (inProcDataCollectionSettingsPresentInRunSettings)
                {
                    this.inProcDataCollectorSettingsCollection = inProcDataCollectionRunSettings.DataCollectorSettingsList;

                    var interfaceTypeInfo = typeof(InProcDataCollection).GetTypeInfo();
                    foreach (var inProcDc in this.inProcDataCollectorSettingsCollection)
                    {
                        var codeBase = this.GetCodebase(inProcDc.CodeBase);
                        var assemblyQualifiedName = inProcDc.AssemblyQualifiedName;
                        var configuration = inProcDc.Configuration;
                        var inProcDataCollector = this.CreateDataCollector(assemblyQualifiedName, codeBase, configuration, interfaceTypeInfo);
                        this.InProcDataCollectors[inProcDataCollector.AssemblyQualifiedName] = inProcDataCollector;
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("InProcDataCollectionExtensionManager: Error occurred while Initializing the datacollectors : {0}", ex);
            }
            finally
            {
                this.IsInProcDataCollectionEnabled = this.InProcDataCollectors.Any();
            }
        }

        /// <summary>
        /// Gets code base for in-proc datacollector
        /// Uses all codebasePaths to check where the datacollector exists
        /// </summary>
        /// <param name="codeBase">The code base.</param>
        /// <returns> Code base </returns>
        private string GetCodebase(string codeBase)
        {
            if (!Path.IsPathRooted(codeBase))
            {
                foreach (var extensionPath in this.codeBasePaths)
                {
                    var assemblyPath = Path.Combine(extensionPath, codeBase);
                    if (this.fileHelper.Exists(assemblyPath))
                    {
                        return assemblyPath;
                    }
                }
            }

            return codeBase;
        }

        private IDictionary<string, object> GetSessionStartProperties(SessionStartEventArgs sessionStartEventArgs)
        {
            var properties = new Dictionary<string, object>
            {
                { Constants.TestSourcesPropertyName, sessionStartEventArgs.GetPropertyValue<IEnumerable<string>>(Constants.TestSourcesPropertyName) }
            };
            return properties;
        }

        private void TriggerInProcDataCollectionMethods(string methodName, InProcDataCollectionArgs methodArg)
        {
            try
            {
                foreach (var inProcDc in this.InProcDataCollectors.Values)
                {
                    inProcDc.TriggerInProcDataCollectionMethod(methodName, methodArg);
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("InProcDataCollectionExtensionManager: Error occurred while Triggering the {0} method : {1}", methodName, ex);
            }
        }

        /// <summary>
        /// Set the data sent via datacollection sink in the testresult property for upstream applications to read.
        /// And removes the data from the dictionary.
        /// </summary>
        /// <param name="testResult">
        /// The test Result.
        /// </param>
        private void SetInProcDataCollectionDataInTestResult(TestResult testResult)
        {
            // Loops through each datacollector reads the data collection data and sets as TestResult property.
            foreach (var entry in this.InProcDataCollectors)
            {
                var dataCollectionData = ((InProcDataCollectionSink)this.inProcDataCollectionSink).GetDataCollectionDataSetForTestCase(testResult.TestCase.Id);

                foreach (var keyValuePair in dataCollectionData)
                {
                    var testProperty = TestProperty.Register(id: keyValuePair.Key, label: keyValuePair.Key, category: string.Empty, description: string.Empty, valueType: typeof(string), validateValueCallback: null, attributes: TestPropertyAttributes.None, owner: typeof(TestCase));
                    testResult.SetPropertyValue(testProperty, keyValuePair.Value);
                }
            }
        }
    }

    internal static class Constants
    {
        /// <summary>
        /// The test session start method name.
        /// </summary>
        public const string TestSessionStartMethodName = "TestSessionStart";

        /// <summary>
        /// The test session end method name.
        /// </summary>
        public const string TestSessionEndMethodName = "TestSessionEnd";

        /// <summary>
        /// The test case start method name.
        /// </summary>
        public const string TestCaseStartMethodName = "TestCaseStart";

        /// <summary>
        /// The test case end method name.
        /// </summary>
        public const string TestCaseEndMethodName = "TestCaseEnd";

        /// <summary>
        /// Test sources property name
        /// </summary>
        public const string TestSourcesPropertyName = "TestSources";

        /// <summary>
        /// Coverlet in-proc data collector code base
        /// </summary>
        public const string CoverletDataCollectorCodebase = "coverlet.collector.dll";

        /// <summary>
        /// Coverlet in-proc data collector type name
        /// </summary>
        public const string CoverletDataCollectorTypeName = "Coverlet.Collector.DataCollection.CoverletInProcDataCollector";
    }
}
