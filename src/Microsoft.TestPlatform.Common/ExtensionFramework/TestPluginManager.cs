// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Manages test plugins information.
    /// </summary>
    internal class TestPluginManager
    {
        private static TestPluginManager instance;

        #region Public Static Methods

        /// <summary>
        /// Gets the singleton instance of TestPluginManager.
        /// </summary>
        public static TestPluginManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TestPluginManager();
                }

                return instance;
            }
        }

        /// <summary>
        /// Gets data type of test extension with given assembly qualified name.
        /// </summary>
        /// <param name="extensionTypeName">Assembly qualified name of the test extension</param>
        /// <returns>Data type of the test extension</returns>
        public static Type GetTestExtensionType(string extensionTypeName)
        {
            Type extensionType = null;
            try
            {
                extensionType = Type.GetType(extensionTypeName, true);
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "GetTestExtensionType: Failed to get type for test extension '{0}': {1}",
                    extensionTypeName,
                    ex);
                throw;
            }

            return extensionType;
        }

        /// <summary>
        /// Instantiates a given data type.
        /// </summary>
        /// <typeparam name="T">Return type of the test extension</typeparam>
        /// <param name="extensionType">Data type of the extension to be instantiated</param>
        /// <returns>Test extension instance</returns>
        public static T CreateTestExtension<T>(Type extensionType)
        {
            if (extensionType == null)
            {
                throw new ArgumentNullException("extensionType");
            }

            EqtTrace.Info("TestPluginManager.CreateTestExtension: Attempting to load test extension: " + extensionType);

            try
            {
                object rawPlugin = Activator.CreateInstance(extensionType);

                T testExtension = (T)rawPlugin;
                return testExtension;
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("TestPluginManager.CreateTestExtension: Could not create instance of type: " + extensionType.ToString() + "  Exception: " + ex);
                    }
                    throw;
                }
#if NETFRAMEWORK
                else if (ex is SystemException)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("TestPluginManager.CreateTestExtension: Could not create instance of type: " + extensionType.ToString() + "  Exception: " + ex);
                    }
                    throw;
                }
#endif
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("TestPluginManager.CreateTestExtension: Could not create instance of type: " + extensionType.ToString() + "  Exception: " + ex);
                }

                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves the test extension collections of given extension type.
        /// </summary>
        /// <typeparam name="IExtension">
        /// Type of the required extensions
        /// </typeparam>
        /// <typeparam name="IMetadata">
        /// Type of metadata of required extensions
        /// </typeparam>
        /// <typeparam name="TMetadata">
        /// Concrete type of metadata
        /// </typeparam>
        /// <param name="endsWithPattern">
        /// Pattern used to select files using String.EndsWith
        /// </param>
        /// <param name="unfiltered">
        /// Receives unfiltered list of test extensions
        /// </param>
        /// <param name="filtered">
        /// Receives test extensions filtered by Identifier data
        /// </param>
        public void GetSpecificTestExtensions<TPluginInfo, IExtension, IMetadata, TMetadata>(
            string endsWithPattern,
            out IEnumerable<LazyExtension<IExtension, Dictionary<string, object>>> unfiltered,
            out IEnumerable<LazyExtension<IExtension, IMetadata>> filtered) where TMetadata : IMetadata where TPluginInfo : TestPluginInformation
        {
            var extensions = TestPluginCache.Instance.DiscoverTestExtensions<TPluginInfo, IExtension>(endsWithPattern);
            this.GetExtensions<TPluginInfo, IExtension, IMetadata, TMetadata>(extensions, out unfiltered, out filtered);
        }

        /// <summary>
        /// Retrieves the test extension collections of given extension type for the provided extension assembly.
        /// </summary>
        /// <param name="extensionAssembly">
        /// The extension assembly. 
        /// </param>
        /// <typeparam name="TPluginInfo">
        /// </typeparam>
        /// <typeparam name="IExtension">
        /// Type of the required extensions
        /// </typeparam>
        /// <typeparam name="IMetadata">
        /// Type of metadata of required extensions
        /// </typeparam>
        /// <typeparam name="TMetadata">
        /// Concrete type of metadata
        /// </typeparam>
        /// <param name="unfiltered">
        /// Receives unfiltered list of test extensions
        /// </param>
        /// <param name="filtered">
        /// Receives test extensions filtered by Identifier data
        /// </param>
        public void GetTestExtensions<TPluginInfo, IExtension, IMetadata, TMetadata>(
            string extensionAssembly,
            out IEnumerable<LazyExtension<IExtension, Dictionary<string, object>>> unfiltered,
            out IEnumerable<LazyExtension<IExtension, IMetadata>> filtered) where TMetadata : IMetadata where TPluginInfo : TestPluginInformation
        {
            var extensions = TestPluginCache.Instance.GetTestExtensions<TPluginInfo, IExtension>(extensionAssembly);
            this.GetExtensions<TPluginInfo, IExtension, IMetadata, TMetadata>(extensions, out unfiltered, out filtered);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Prepares a List of TestPluginInformation&gt;
        /// </summary>
        /// <typeparam name="T"> Type of TestPluginIInformation. </typeparam>
        /// <param name="dictionary"> The dictionary containing plugin identifier data and its info. </param>
        /// <returns> Collection of test plugins information </returns>
        private IEnumerable<TestPluginInformation> GetValuesFromDictionary<T>(Dictionary<string, T> dictionary) where T : TestPluginInformation
        {
            var values = new List<TestPluginInformation>();

            foreach (var key in dictionary.Keys)
            {
                values.Add(dictionary[key]);
            }

            return values;
        }

        /// <summary>
        /// Gets unfiltered and filtered extensions from the provided test extension collection.
        /// </summary>
        /// <typeparam name="TPluginInfo">
        /// </typeparam>
        /// <typeparam name="IExtension">
        /// Type of the required extensions
        /// </typeparam>
        /// <typeparam name="IMetadata">
        /// Type of metadata of required extensions
        /// </typeparam>
        /// <typeparam name="TMetadata">
        /// Concrete type of metadata
        /// </typeparam>
        /// <param name="testPluginInfo">
        /// The test extension dictionary.
        /// </param>
        /// <param name="unfiltered">
        /// Receives unfiltered list of test extensions
        /// </param>
        /// <param name="filtered">
        /// Receives test extensions filtered by Identifier data
        /// </param>
        private void GetExtensions<TPluginInfo, IExtension, IMetadata, TMetadata>(
            Dictionary<string, TPluginInfo> testPluginInfo,
            out IEnumerable<LazyExtension<IExtension, Dictionary<string, object>>> unfiltered,
            out IEnumerable<LazyExtension<IExtension, IMetadata>> filtered) where TMetadata : IMetadata where TPluginInfo : TestPluginInformation
        {
            var unfilteredExtensions = new List<LazyExtension<IExtension, Dictionary<string, object>>>();
            var filteredExtensions = new List<LazyExtension<IExtension, IMetadata>>();

            var testPlugins = this.GetValuesFromDictionary(testPluginInfo);
            foreach (var plugin in testPlugins)
            {
                if (!string.IsNullOrEmpty(plugin.IdentifierData))
                {
                    var testExtension = new LazyExtension<IExtension, IMetadata>(plugin, typeof(TMetadata));
                    filteredExtensions.Add(testExtension);
                }

                unfilteredExtensions.Add(new LazyExtension<IExtension, Dictionary<string, object>>(plugin, new Dictionary<string, object>()));
            }

            unfiltered = unfilteredExtensions;
            filtered = filteredExtensions;
        }

        #endregion
    }
}
