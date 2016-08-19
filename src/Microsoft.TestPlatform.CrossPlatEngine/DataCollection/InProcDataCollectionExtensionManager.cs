// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
#if !NET46
    using System.Runtime.Loader;
#endif

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using ObjectModel.DataCollector.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;

    /// <summary>
    /// The in process data collection extension manager.
    /// </summary>
    public class InProcDataCollectionExtensionManager
    {
        private IDictionary<string, object> inProcDataCollectors;
        private IDataCollectionSink inProcDataCollectionSink;
        private IDictionary<string, Tuple<string, string>> inProcDataSinkDict;

        public InProcDataCollectionExtensionManager()
        {
            this.inProcDataCollectors = new Dictionary<string, Object>();
            this.inProcDataCollectionSink = new InProcDataCollectionSink();
            this.inProcDataSinkDict = new Dictionary<string, Tuple<string, string>>();
        }

        public InProcDataCollectionExtensionManager(string runsettings)
        {
            this.inProcDataCollectors = new Dictionary<string, Object>();
            InProcDataCollectionUtilities.ReadInProcDataCollectionRunSettings(runsettings);
            this.inProcDataCollectionSink = new InProcDataCollectionSink();
            this.inProcDataSinkDict = new Dictionary<string, Tuple<string, string>>();
            this.InitializeInProcDataCollectors();
        }

        public bool IsInProcDataCollectionEnabled
        {
            get
            {
                return InProcDataCollectionUtilities.InProcDataCollectionEnabled;
            }
        }

        public Collection<DataCollectorSettings> InProcDataCollectorSettingsCollection { get; private set; }

        /// <summary>
        /// Loads all the inproc data collector dlls
        /// </summary>       
        public void InitializeInProcDataCollectors()
        {
            try
            {
                if (this.IsInProcDataCollectionEnabled)
                {
                    this.InProcDataCollectorSettingsCollection = InProcDataCollectionUtilities.GetInProcDataCollectorSettings();

                    var interfaceType = typeof(InProcDataCollection);
                    foreach (var inProcDc in this.InProcDataCollectorSettingsCollection)
                    {
                        var assembly = this.LoadInProcDataCollectorExtension(inProcDc.CodeBase);
                        var type =
                            assembly?.GetTypes()
                                .FirstOrDefault(x => (x.AssemblyQualifiedName.Equals(inProcDc.AssemblyQualifiedName) && interfaceType.GetTypeInfo().IsAssignableFrom(x)));
                        if (type != null && !this.inProcDataCollectors.ContainsKey(type.AssemblyQualifiedName))
                        {
                            var obj = CreateObjectFromType(type);
                            InvokeInitializeOnInProcDataCollector(obj, this.inProcDataCollectionSink);
                            this.inProcDataCollectors[type.AssemblyQualifiedName] = obj;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("Error occured while Initializing the datacollectors : {0}", ex);
            }
        }

        /// <summary>
        /// The trigger session start.
        /// </summary>
        public virtual void TriggerTestSessionStart()
        {
            TestSessionStartArgs testSessionStartArgs = new TestSessionStartArgs();
            this.TriggerInProcDataCollectionMethods(Constants.TestSessionStartMethodName, testSessionStartArgs);
        }

        /// <summary>
        /// The trigger session end.
        /// </summary>
        public virtual void TriggerTestSessionEnd()
        {
            var testSessionEndArgs = new TestSessionEndArgs();
            this.TriggerInProcDataCollectionMethods(Constants.TestSessionEndMethodName, testSessionEndArgs);
        }

        /// <summary>
        /// The trigger test case start.
        /// </summary>
        public virtual void TriggerTestCaseStart(TestCase testCase)
        {
            var testCaseStartArgs = new TestCaseStartArgs(testCase);
            this.TriggerInProcDataCollectionMethods(Constants.TestCaseStartMethodName, testCaseStartArgs);

        }

        /// <summary>
        /// The trigger test case end.
        /// </summary>
        public virtual void TriggerTestCaseEnd(TestCase testCase, TestOutcome outcome)
        {
            var dataCollectionContext = new DataCollectionContext(testCase);
            var testCaseEndArgs = new TestCaseEndArgs(dataCollectionContext, outcome);
            this.TriggerInProcDataCollectionMethods(Constants.TestCaseEndMethodName, testCaseEndArgs);
        }

        /// <summary>
        /// Triggers the send test result method
        /// </summary>
        /// <param name="testResult"></param>
        public virtual void TriggerUpdateTestResult(TestResult testResult)
        {
            this.SetInProcDataCollectionDataInTestResult(testResult);
        }

        /// <summary>
        /// Loads the assembly into the default context based on the codebase path
        /// </summary>
        /// <param name="codeBase"></param>
        /// <returns></returns>
        private Assembly LoadInProcDataCollectorExtension(string codeBase)
        {
            Assembly assembly = null;
            try
            {
#if NET46
                assembly = Assembly.LoadFrom(codeBase);                
#else
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(codeBase);
#endif

            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "Error occured while loading the InProcDataCollector : {0} , Exception Details : {1}", codeBase, ex);
            }

            return assembly;
        }

        private static object CreateObjectFromType(Type type)
        {
            object obj = null;

            var typeInfo = type.GetTypeInfo();
            var constructorInfo = typeInfo?.GetConstructor(Type.EmptyTypes);
            obj = constructorInfo?.Invoke(new object[] { });

            return obj;
        }

        private static void InvokeInitializeOnInProcDataCollector(object obj, IDataCollectionSink inProcDataCollectionSink)
        {
            var initializeMethodInfo = GetMethodInfoFromType(obj.GetType(), "Initialize", new Type[] { typeof(IDataCollectionSink) });
            initializeMethodInfo.Invoke(obj, new object[] { inProcDataCollectionSink });
        }

        private static MethodInfo GetMethodInfoFromType(Type type, string funcName, Type[] argumentTypes)
        {
            MethodInfo methodInfo = null;

            var typeInfo = type.GetTypeInfo();
            methodInfo = typeInfo?.GetMethod(funcName, argumentTypes);
            return methodInfo;
        }

        private void TriggerInProcDataCollectionMethods(string methodName, InProcDataCollectionArgs methodArg)
        {
            try
            {
                foreach (var entry in this.inProcDataCollectors)
                {
                    var methodInfo = GetMethodInfoFromType(entry.Value.GetType(), methodName, new[] { methodArg.GetType() });

                    if (methodName.Equals(Constants.TestSessionStartMethodName))
                    {
                        var testSessionStartArgs = (TestSessionStartArgs)methodArg;
                        var config =
                        this.InProcDataCollectorSettingsCollection.FirstOrDefault(
                            x => x.AssemblyQualifiedName.Equals(entry.Key))?
                            .Configuration.OuterXml;
                        testSessionStartArgs.Configuration = config;
                        methodInfo?.Invoke(entry.Value, new object[] { testSessionStartArgs });
                    }
                    else
                    {
                        methodInfo?.Invoke(entry.Value, new object[] { methodArg });
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("Error occured while Triggering the {0} method : {1}", methodName, ex);
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
