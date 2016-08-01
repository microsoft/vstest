// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    
    /// <summary>
    /// Manages the the Test Executor extensions.
    /// </summary>
    internal class TestExecutorExtensionManager : TestExtensionManager<ITestExecutor, ITestExecutorCapabilities>
    {
        #region Fields

        private static TestExecutorExtensionManager testExecutorExtensionManager;
        private static object synclock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="unfilteredTestExtensions"> The unfiltered Test Extensions. </param>
        /// <param name="testExtensions"> The test Extensions. </param>
        /// <param name="logger"> The logger. </param>
        /// <remarks>
        /// This constructor is not public because instances should be retrieved using the
        /// factory method.  The constructor is protected for testing purposes.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected TestExecutorExtensionManager(
            IEnumerable<LazyExtension<ITestExecutor, Dictionary<string, object>>> unfilteredTestExtensions,
            IEnumerable<LazyExtension<ITestExecutor, ITestExecutorCapabilities>> testExtensions,
            IMessageLogger logger)
            : base(unfilteredTestExtensions, testExtensions, logger)
        {
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates the TestExecutorExtensionManager.
        /// </summary>
        /// <returns>Instance of the TestExecutorExtensionManager</returns>
        internal static TestExecutorExtensionManager Create()
        {
            if (testExecutorExtensionManager == null)
            {
                lock (synclock)
                {
                    if (testExecutorExtensionManager == null)
                    {
                        IEnumerable<LazyExtension<ITestExecutor, Dictionary<string, object>>> unfilteredTestExtensions;
                        IEnumerable<LazyExtension<ITestExecutor, ITestExecutorCapabilities>> testExtensions;

                        TestPluginManager.Instance
                            .GetTestExtensions<ITestExecutor, ITestExecutorCapabilities, TestExecutorMetadata>(
                                out unfilteredTestExtensions,
                                out testExtensions);
                        
                        testExecutorExtensionManager = new TestExecutorExtensionManager(
                                                            unfilteredTestExtensions, testExtensions, TestSessionMessageLogger.Instance);
                    }
                }
            }

            return testExecutorExtensionManager;
        }

        /// <summary>
        /// Gets an instance of the Test Execution Extension Manager for the extension.
        /// </summary>
        /// <param name="extensionAssembly"> The extension assembly. </param>
        /// <returns> The <see cref="TestExecutorExtensionManager"/>. </returns>
        /// <remarks>
        /// This would provide an execution extension manager where extensions in
        /// only the extension assembly provided are discovered. This is not cached.
        /// </remarks>
        internal static TestExecutorExtensionManager GetExecutionExtensionManager(string extensionAssembly)
        {
            IEnumerable<LazyExtension<ITestExecutor, Dictionary<string, object>>> unfilteredTestExtensions;
            IEnumerable<LazyExtension<ITestExecutor, ITestExecutorCapabilities>> testExtensions;

            TestPluginManager.Instance
                .GetTestExtensions<ITestExecutor, ITestExecutorCapabilities, TestExecutorMetadata>(
                    extensionAssembly,
                    out unfilteredTestExtensions,
                    out testExtensions);

            // Todo aajohn . This can be optimized - The base class's populate map would be called repeatedly for the same extension assembly.
            // Have a single instance of TestExecutorExtensionManager that keeps populating the map iteratively.
            return new TestExecutorExtensionManager(
                unfilteredTestExtensions,
                testExtensions,
                TestSessionMessageLogger.Instance);
        }

        /// <summary>
        /// Destroy the TestExecutorExtensionManager.
        /// </summary>
        internal static void Destroy()
        {
            lock (synclock)
            {
                testExecutorExtensionManager = null;
            }
        }

        /// <summary>
        /// Load all the executors and fail on error
        /// </summary>
        /// <param name="shouldThrowOnError"> Indicates whether this method should throw on error. </param>
        internal static void LoadAndInitializeAllExtensions(bool shouldThrowOnError)
        {
            var executorExtensionManager = Create();

            try
            {
                foreach (var executor in executorExtensionManager.TestExtensions)
                {
                    // Note: - The below Verbose call should not be under IsVerboseEnabled check as we want to 
                    // call executor.Value even if logging is not enabled. 
                    EqtTrace.Verbose("TestExecutorExtensionManager: Loading executor {0}", executor.Value);
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error(
                        "TestExecutorExtensionManager: LoadAndInitialize: Exception occured while loading extensions {0}",
                        ex);
                }

                if (shouldThrowOnError)
                {
                    throw;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Holds data about the Test executor.
    /// </summary>
    internal class TestExecutorMetadata : ITestExecutorCapabilities
    {
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="extensionUri">Uri identifying the executor</param>
        public TestExecutorMetadata(string extensionUri)
        {
            this.ExtensionUri = extensionUri;
        }

        /// <summary>
        /// Gets Uri identifying the executor.
        /// </summary>
        public string ExtensionUri
        {
            get;
            private set;
        }
    }
}
