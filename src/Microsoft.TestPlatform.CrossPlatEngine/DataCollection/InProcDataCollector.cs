// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <summary>
    /// Class representing an InProcDataCollector loaded by InProcDataCollectionExtensionManager
    /// </summary>
    internal class InProcDataCollector : IInProcDataCollector
    {
        /// <summary>
        /// DataCollector Class Type
        /// </summary>
        private Type dataCollectorType;

        /// <summary>
        /// Instance of the 
        /// </summary>
        private object dataCollectorObject;

        /// <summary>
        /// Config XML from the runsettings for current datacollector
        /// </summary>
        private string configXml;

        /// <summary>
        /// AssemblyLoadContext for current platform
        /// </summary>
        private IAssemblyLoadContext assemblyLoadContext;

        public InProcDataCollector(
            string codeBase,
            string assemblyQualifiedName,
            TypeInfo interfaceTypeInfo,
            string configXml)
            : this(codeBase, assemblyQualifiedName, interfaceTypeInfo, configXml, new PlatformAssemblyLoadContext())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InProcDataCollector"/> class.
        /// </summary>
        /// <param name="codeBase">
        /// </param>
        /// <param name="assemblyQualifiedName">
        /// </param>
        /// <param name="interfaceTypeInfo">
        /// </param>
        /// <param name="configXml">
        /// </param>
        /// <param name="assemblyLoadContext">
        /// </param>
        internal InProcDataCollector(string codeBase, string assemblyQualifiedName, TypeInfo interfaceTypeInfo, string configXml, IAssemblyLoadContext assemblyLoadContext)
        {
            this.configXml = configXml;
            this.assemblyLoadContext = assemblyLoadContext;

            var assembly = this.LoadInProcDataCollectorExtension(codeBase);
            this.dataCollectorType =
                assembly?.GetTypes()
                    .FirstOrDefault(x => x.AssemblyQualifiedName.Equals(assemblyQualifiedName) && interfaceTypeInfo.IsAssignableFrom(x.GetTypeInfo()));

            this.AssemblyQualifiedName = this.dataCollectorType?.AssemblyQualifiedName;
        }

        /// <summary>
        /// AssemblyQualifiedName of the datacollector type
        /// </summary>
        public string AssemblyQualifiedName { get; private set; }

        /// <summary>
        /// Loads the DataCollector type 
        /// </summary>
        /// <param name="inProcDataCollectionSink">Sink object to send data</param>
        public void LoadDataCollector(IDataCollectionSink inProcDataCollectionSink)
        {
            this.dataCollectorObject = CreateObjectFromType(dataCollectorType);
            InitializeDataCollector(dataCollectorObject, inProcDataCollectionSink);
        }

        /// <summary>
        /// Triggers InProcDataCollection Methods
        /// </summary>
        /// <param name="methodName">Name of the method to trigger</param>
        /// <param name="methodArg">Arguments for the method</param>
        public void TriggerInProcDataCollectionMethod(string methodName, InProcDataCollectionArgs methodArg)
        {
            var methodInfo = GetMethodInfoFromType(this.dataCollectorObject.GetType(), methodName, new[] { methodArg.GetType() });

            if (methodName.Equals(Constants.TestSessionStartMethodName))
            {
                var testSessionStartArgs = (TestSessionStartArgs)methodArg;
                testSessionStartArgs.Configuration = configXml;
                methodInfo?.Invoke(this.dataCollectorObject, new object[] { testSessionStartArgs });
            }
            else
            {
                methodInfo?.Invoke(this.dataCollectorObject, new object[] { methodArg });
            }
        }

        #region Private Methods

        private void InitializeDataCollector(object obj, IDataCollectionSink inProcDataCollectionSink)
        {
            var initializeMethodInfo = GetMethodInfoFromType(obj.GetType(), "Initialize", new Type[] { typeof(IDataCollectionSink) });
            initializeMethodInfo.Invoke(obj, new object[] { inProcDataCollectionSink });
        }

        private static MethodInfo GetMethodInfoFromType(Type type, string funcName, Type[] argumentTypes)
        {
            return type.GetMethod(funcName, argumentTypes);
        }

        private static object CreateObjectFromType(Type type)
        {
            object obj = null;

            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            obj = constructorInfo?.Invoke(new object[] { });

            return obj;
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
                assembly = this.assemblyLoadContext.LoadAssemblyFromPath(codeBase);
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "InProcDataCollectionExtensionManager: Error occured while loading the InProcDataCollector : {0} , Exception Details : {1}", codeBase, ex);
            }

            return assembly;
        }

        #endregion
    }
}
