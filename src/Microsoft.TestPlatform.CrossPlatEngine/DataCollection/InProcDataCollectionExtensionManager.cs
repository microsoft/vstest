// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;

    /// <summary>
    /// The in process data collection extension manager.
    /// </summary>
    internal class InProcDataCollectionExtensionManager
    {
        protected IDictionary<string, IInProcDataCollector> inProcDataCollectors;
        private IDataCollectionSink inProcDataCollectionSink;

        private IDictionary<Guid, List<TestResult>> testResultDictionary;
        private HashSet<Guid> testCaseEndStatusMap;
        private ITestRunCache testRunCache;

        internal static TestProperty FlushResultTestResultPoperty;

        /// <summary>
        /// Loaded in-proc datacollectors collection
        /// </summary>
        private IEnumerable<DataCollectorSettings> inProcDataCollectorSettingsCollection;

        private object testCaseEndStatusSyncObject = new object();

        public InProcDataCollectionExtensionManager(string runSettings, ITestRunCache testRunCache, IDataCollectionTestCaseEventManager dataCollectionTestCaseEventManager)
        {
            this.testRunCache = testRunCache;

            this.inProcDataCollectors = new Dictionary<string, IInProcDataCollector>();
            this.inProcDataCollectionSink = new InProcDataCollectionSink();
            this.testResultDictionary = new Dictionary<Guid, List<TestResult>>();
            this.testCaseEndStatusMap = new HashSet<Guid>();

            // Initialize InProcDataCollectors
            this.InitializeInProcDataCollectors(runSettings);

            FlushResultTestResultPoperty = TestProperty.Register(id: "allowTestResultFlush", label: "allowTestResultFlush", category: string.Empty, description: string.Empty, valueType: typeof(bool), validateValueCallback: null, attributes: TestPropertyAttributes.None, owner: typeof(TestCase));

            dataCollectionTestCaseEventManager.TestCaseEnd += TriggerTestCaseEnd;
            dataCollectionTestCaseEventManager.TestCaseStart += TriggerTestCaseStart;
            dataCollectionTestCaseEventManager.TestResult += TriggerUpdateTestResult;
            dataCollectionTestCaseEventManager.SessionStart += TriggerTestSessionStart;
            dataCollectionTestCaseEventManager.SessionEnd += TriggerTestSessionEnd;
        }



        public bool IsInProcDataCollectionEnabled { get; private set; }

        /// <summary>
        /// Loads all the inproc data collector dlls
        /// </summary>       
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

                // Initialize if we have atleast one
                if (inProcDataCollectionSettingsPresentInRunSettings)
                {
                    this.inProcDataCollectorSettingsCollection = inProcDataCollectionRunSettings.DataCollectorSettingsList;

                    var interfaceTypeInfo = typeof(InProcDataCollection).GetTypeInfo();
                    foreach (var inProcDc in this.inProcDataCollectorSettingsCollection)
                    {
                        var inProcDataCollector = this.CreateDataCollector(inProcDc, interfaceTypeInfo);
                        this.inProcDataCollectors[inProcDataCollector.AssemblyQualifiedName] = inProcDataCollector;
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("InProcDataCollectionExtensionManager: Error occured while Initializing the datacollectors : {0}", ex);
            }
            finally
            {
                this.IsInProcDataCollectionEnabled = this.inProcDataCollectors.Any();
            }
        }

        protected virtual IInProcDataCollector CreateDataCollector(DataCollectorSettings dataCollectorSettings, TypeInfo interfaceTypeInfo)
        {
            var inProcDataCollector = new InProcDataCollector(dataCollectorSettings.CodeBase, dataCollectorSettings.AssemblyQualifiedName,
                interfaceTypeInfo, dataCollectorSettings.Configuration.OuterXml);

            inProcDataCollector.LoadDataCollector(inProcDataCollectionSink);

            return inProcDataCollector;
        }

        /// </summary>
        public virtual void TriggerTestSessionStart(object sender, SessionStartEventArgs e)
        {
            this.testCaseEndStatusMap.Clear();
            this.testResultDictionary.Clear();

            TestSessionStartArgs testSessionStartArgs = new TestSessionStartArgs();
            this.TriggerInProcDataCollectionMethods(Constants.TestSessionStartMethodName, testSessionStartArgs);
        }

        /// <summary>
        /// The trigger session end.
        /// </summary>
        public virtual void TriggerTestSessionEnd(object sender, SessionEndEventArgs e)
        {
            var testSessionEndArgs = new TestSessionEndArgs();
            this.TriggerInProcDataCollectionMethods(Constants.TestSessionEndMethodName, testSessionEndArgs);
        }

        /// <summary>
        /// The trigger test case start.
        /// </summary>
        public virtual void TriggerTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            lock (testCaseEndStatusSyncObject)
            {
                this.testCaseEndStatusMap.Remove(e.TestCaseId);
            }

            var testCaseStartArgs = new TestCaseStartArgs(e.TestElement);
            this.TriggerInProcDataCollectionMethods(Constants.TestCaseStartMethodName, testCaseStartArgs);
        }

        /// <summary>
        /// The trigger test case end.
        /// </summary>
        public virtual void TriggerTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            bool isTestCaseEndAlreadySent = false;
            lock (testCaseEndStatusSyncObject)
            {
                isTestCaseEndAlreadySent = this.testCaseEndStatusMap.Contains(e.TestCaseId);
                if (!isTestCaseEndAlreadySent)
                {
                    this.testCaseEndStatusMap.Add(e.TestCaseId);
                }

                // Do not support multiple - testcasends for a single test case start
                // TestCaseEnd must always be preceded by testcasestart for a given test case id
                if (!isTestCaseEndAlreadySent)
                {
                    var dataCollectionContext = new DataCollectionContext(e.TestElement);
                    var testCaseEndArgs = new TestCaseEndArgs(dataCollectionContext, e.TestOutcome);


                    // Call all in-proc datacollectors - TestCaseEnd event
                    this.TriggerInProcDataCollectionMethods(Constants.TestCaseEndMethodName, testCaseEndArgs);

                    // If dictionary contains results for this test case, update them with in-proc data and flush them
                    if (testResultDictionary.ContainsKey(e.TestCaseId))
                    {
                        foreach (var testResult in testResultDictionary[e.TestCaseId])
                        {
                            this.SetInProcDataCollectionDataInTestResult(testResult);

                            // TestResult updated with in-proc data, just flush
                            this.testRunCache.OnNewTestResult(testResult);
                        }

                        this.testResultDictionary.Remove(e.TestCaseId);
                    }
                }
            }
        }

        /// <summary>
        /// Triggers the send test result method
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns>True, if result can be flushed. False, otherwise</returns>
        public virtual void TriggerUpdateTestResult(object sender, TestResultEventArgs e)
        {
            bool allowTestResultFlush = true;
            var testCaseId = e.TestResult.TestCase.Id;

            lock (testCaseEndStatusSyncObject)
            {
                if (this.testCaseEndStatusMap.Contains(testCaseId))
                {
                    // Just set the cached in-proc data if already exists
                    this.SetInProcDataCollectionDataInTestResult(e.TestResult);
                }
                else
                {
                    // No TestCaseEnd received yet
                    // We need to wait for testcaseend before flushing
                    allowTestResultFlush = false;

                    // Cache results so we can flush later with in proc data
                    if (testResultDictionary.ContainsKey(testCaseId))
                    {
                        testResultDictionary[testCaseId].Add(e.TestResult);
                    }
                    else
                    {
                        testResultDictionary.Add(testCaseId, new List<TestResult>() { e.TestResult });
                    }
                }
            }

            this.SetAllowTestResultFlushInTestResult(e.TestResult, allowTestResultFlush);
        }

        /// <summary>
        /// Flush any test results that are cached in dictionary
        /// </summary>
        public void FlushLastChunkResults()
        {
            // Can happen if we cached test results expecting a test case end event for them
            // If test case end events never come, we have to flush all of them 
            foreach (var results in this.testResultDictionary.Values)
            {
                foreach (var result in results)
                {
                    this.testRunCache.OnNewTestResult(result);
                }
            }
        }

        private void TriggerInProcDataCollectionMethods(string methodName, InProcDataCollectionArgs methodArg)
        {
            try
            {
                foreach (var inProcDc in this.inProcDataCollectors.Values)
                {
                    inProcDc.TriggerInProcDataCollectionMethod(methodName, methodArg);
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("InProcDataCollectionExtensionManager: Error occured while Triggering the {0} method : {1}", methodName, ex);
            }
        }

        /// <summary>
        /// Set the data sent via datacollection sink in the testresult property for upstream applications to read.
        /// And removes the data from the dictionary.
        /// </summary>
        /// <param name="testResultArg"></param>
        private void SetInProcDataCollectionDataInTestResult(TestResult testResult)
        {
            //Loops through each datacollector reads the data collection data and sets as TestResult property.
            foreach (var entry in this.inProcDataCollectors)
            {
                var dataCollectionData =
                    ((InProcDataCollectionSink)this.inProcDataCollectionSink).GetDataCollectionDataSetForTestCase(
                        testResult.TestCase.Id);

                foreach (var keyValuePair in dataCollectionData)
                {
                    var testProperty = TestProperty.Register(id: keyValuePair.Key, label: keyValuePair.Key, category: string.Empty, description: string.Empty, valueType: typeof(string), validateValueCallback: null, attributes: TestPropertyAttributes.None, owner: typeof(TestCase));
                    testResult.SetPropertyValue(testProperty, keyValuePair.Value);
                }
            }
        }

        /// <summary>
        /// Set the data sent via datacollection sink in the testresult property for upstream applications to read.
        /// And removes the data from the dictionary.
        /// </summary>
        /// <param name="testResultArg"></param>
        private void SetAllowTestResultFlushInTestResult(TestResult testResult, bool allowTestResultFlush)
        {
            var testProperty = TestProperty.Register(id: nameof(allowTestResultFlush), label: nameof(allowTestResultFlush), category: string.Empty, description: string.Empty, valueType: typeof(bool), validateValueCallback: null, attributes: TestPropertyAttributes.None, owner: typeof(TestCase));
            testResult.SetPropertyValue(testProperty, allowTestResultFlush);
        }
    }

    public static class Constants
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
    }
}
