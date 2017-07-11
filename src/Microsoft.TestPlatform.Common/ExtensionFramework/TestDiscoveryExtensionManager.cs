// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Responsible for managing the Test Discoverer extensions which are available.
    /// </summary>
    internal class TestDiscoveryExtensionManager
    {
        #region Fields
        public static Regex Regex = new Regex(TestPlatformConstants.TestAdapterRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static TestDiscoveryExtensionManager testDiscoveryExtensionManager;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <remarks>The factory should be used for getting instances of this type so the constructor is not public.</remarks>
        protected TestDiscoveryExtensionManager(
            IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> discoverers,
            IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> unfilteredDiscoverers)
        {
            ValidateArg.NotNull<IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>>>(discoverers, "discoverers");
            ValidateArg.NotNull<IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>>>(unfilteredDiscoverers, "unfilteredDiscoverers");

            this.Discoverers = discoverers;
            this.UnfilteredDiscoverers = unfilteredDiscoverers;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unfiltered list of test discoverers which are available.
        /// </summary>
        /// <remarks>
        /// Used in the /ListDiscoverers command line argument processor to generically list out extensions.
        /// </remarks>
        public IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> UnfilteredDiscoverers { get; private set; }


        /// <summary>
        /// Gets the discoverers which are available for discovering tests.
        /// </summary>
        public IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> Discoverers { get; private set; }

        #endregion

        #region Factory

        /// <summary>
        /// Gets an instance of the Test Discovery Extension Manager.
        /// </summary>
        /// <returns>
        /// Instance of the Test Discovery Extension Manager
        /// </returns>
        /// <remarks>
        /// This would provide a discovery extension manager where extensions in 
        /// all the extension assemblies are discovered. This is cached.
        /// </remarks>
        public static TestDiscoveryExtensionManager Create()
        {
            if (testDiscoveryExtensionManager == null)
            {
                IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> unfilteredTestExtensions;
                IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> testExtensions;

                TestPluginManager.Instance
                    .GetSpecificTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                        TestDiscoveryExtensionManager.Regex,
                        out unfilteredTestExtensions,
                        out testExtensions);

                testDiscoveryExtensionManager = new TestDiscoveryExtensionManager(
                    testExtensions,
                    unfilteredTestExtensions);
            }

            return testDiscoveryExtensionManager;
        }

        /// <summary>
        /// Gets an instance of the Test Discovery Extension Manager for the extension.
        /// </summary>
        /// <param name="extensionAssembly"> The extension assembly. </param>
        /// <returns> The <see cref="TestDiscoveryExtensionManager"/> instance. </returns>
        /// <remarks>
        /// This would provide a discovery extension manager where extensions in
        /// only the extension assembly provided are discovered. This is not cached
        /// </remarks>
        public static TestDiscoveryExtensionManager GetDiscoveryExtensionManager(string extensionAssembly)
        {
            IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> unfilteredTestExtensions;
            IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> testExtensions;

            TestPluginManager.Instance
                .GetTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                    extensionAssembly,
                    out unfilteredTestExtensions,
                    out testExtensions);

            return new TestDiscoveryExtensionManager(
                testExtensions,
                unfilteredTestExtensions);
        }

        /// <summary>
        /// Loads and Initializes all the extensions.
        /// </summary>
        /// <param name="throwOnError"> The throw On Error. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void LoadAndInitializeAllExtensions(bool throwOnError)
        {
            try
            {
                var allDiscoverers = Create();

                // Iterate throw the discoverers so that they are initialized
                foreach (var discoverer in allDiscoverers.Discoverers)
                {
                    // discoverer.value below is what initializes the extension types and hence is not under a EqtTrace.IsVerboseEnabled check.
                    EqtTrace.Verbose("TestDiscoveryManager: LoadExtensions: Created discoverer {0}", discoverer.Value);
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("TestDiscoveryManager: LoadExtensions: Exception occured while loading extensions {0}", ex);
                }

                if (throwOnError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Destroys the cache.
        /// </summary>
        internal static void Destroy()
        {
            testDiscoveryExtensionManager = null;
        }

        #endregion
    }

    /// <summary>
    /// Hold data about the Test discoverer.
    /// </summary>
    internal class TestDiscovererMetadata : ITestDiscovererCapabilities
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        /// <param name="fileExtensions"> The file Extensions. </param>
        /// <param name="defaultExecutorUri"> The default Executor Uri. </param>
        public TestDiscovererMetadata(IReadOnlyCollection<string> fileExtensions, string defaultExecutorUri)
        {
            if (fileExtensions != null && fileExtensions.Count > 0)
            {
                this.FileExtension = new List<string>(fileExtensions);
            }

            if (!string.IsNullOrWhiteSpace(defaultExecutorUri))
            {
                this.DefaultExecutorUri = new Uri(defaultExecutorUri);
            }
        }

        /// <summary>
        /// Gets file extensions supported by the discoverer.
        /// </summary>
        public IEnumerable<string> FileExtension
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the default executor Uri for this discoverer
        /// </summary>
        public Uri DefaultExecutorUri
        {
            get;
            private set;
        }
    }
}
