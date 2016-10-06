using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
#if NET46
    internal class AdapterManager : MarshalByRefObject
    {
        [NonSerialized]
        private readonly IDictionary<string, Assembly> resolvedAssemblies;

        [NonSerialized]
        private ITestExecutor adapterExecutor;

        public AdapterManager(string executorTypeFullName, string assemblyQualifiedName)
        {
            this.resolvedAssemblies = new Dictionary<string, Assembly>();
            InitializeAdapter(executorTypeFullName, assemblyQualifiedName);
        }

        /// <summary>
        /// Initializes the Adapter that can run the test
        /// </summary>
        /// <param name="typeName">Type of the "ITestExecutor" impl in the Adapter</param>
        /// <param name="assemblyQualifiedName">AssemblyQualifiedName of the adapter assembly</param>
        private void InitializeAdapter(string executorTypeFullName, string assemblyQualifiedName)
        {
            this.adapterExecutor = null;
            try
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyQualifiedName));
                var adapterExecutorType = assembly.GetTypes().AsParallel().Where((type) => string.Equals(executorTypeFullName, type.FullName)).FirstOrDefault();

                if (adapterExecutorType != null)
                {
                    this.adapterExecutor = (ITestExecutor) Activator.CreateInstance(adapterExecutorType);
                }
            }
            catch(Exception ex)
            {
                EqtTrace.Error("InitializeAdapter: Failed to load assembly '{0}' and type '{1}': '{2}'", 
                    assemblyQualifiedName, executorTypeFullName, ex);
            }
        }


        /// <summary>
        /// Initializes the Adapter that can run the test
        /// </summary>
        /// <param name="typeName">Type of the "ITestExecutor" impl in the Adapter</param>
        /// <param name="assemblyQualifiedName">AssemblyQualifiedName of the adapter assembly</param>
        public void InvokeTestRun(List<string> sources, string runSettings, FrameworkHandleInAppDomain frameworkHandle, bool isBeingDebugged)
        {
            if (this.adapterExecutor != null)
            {
                try
                {
                    var runContext = new RunContext();
                    runContext.RunSettings = RunSettingsUtilities.CreateAndInitializeRunSettings(runSettings);
                    runContext.KeepAlive = false;
                    runContext.InIsolation = true;
                    runContext.IsDataCollectionEnabled = false;
                    runContext.IsBeingDebugged = isBeingDebugged;

                    var runConfig = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettings);
                    runContext.TestRunDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfig);
                    runContext.SolutionDirectory = RunSettingsUtilities.GetSolutionDirectory(runConfig);

                    frameworkHandle.inProcDataCollectionExtensionManager?.TriggerTestSessionStart();

                    this.adapterExecutor.RunTests(sources, runContext, frameworkHandle);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("RunTests: Failed to run tests: {0}", ex);
                }
                finally
                {
                    frameworkHandle.inProcDataCollectionExtensionManager?.TriggerTestSessionEnd();
                }
            }
        }

        /// <summary>
        /// Initializes the Adapter that can run the test
        /// </summary>
        /// <param name="typeName">Type of the "ITestExecutor" impl in the Adapter</param>
        /// <param name="assemblyQualifiedName">AssemblyQualifiedName of the adapter assembly</param>
        public void InvokeTestRun(string serializedTestCases, string runSettings, FrameworkHandleInAppDomain frameworkHandle, bool isBeingDebugged)
        {
            if (this.adapterExecutor != null)
            {
                try
                {
                    var testCases = JsonDataSerializer.Instance.Deserialize<List<TestCase>>(serializedTestCases);

                    var runContext = new RunContext();
                    runContext.RunSettings = RunSettingsUtilities.CreateAndInitializeRunSettings(runSettings);
                    runContext.KeepAlive = false;
                    runContext.InIsolation = true;
                    runContext.IsDataCollectionEnabled = false;
                    runContext.IsBeingDebugged = isBeingDebugged;

                    var runConfig = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettings);
                    runContext.TestRunDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfig);
                    runContext.SolutionDirectory = RunSettingsUtilities.GetSolutionDirectory(runConfig);

                    frameworkHandle.inProcDataCollectionExtensionManager?.TriggerTestSessionStart();

                    this.adapterExecutor.RunTests(testCases, runContext, frameworkHandle);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("RunTests: Failed to run tests: {0}", ex);
                }
                finally
                {
                    frameworkHandle.inProcDataCollectionExtensionManager?.TriggerTestSessionEnd();
                }
            }
        }
    }
#endif
}
