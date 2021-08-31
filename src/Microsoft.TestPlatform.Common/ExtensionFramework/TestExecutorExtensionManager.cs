// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Manages the Test Executor extensions.
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

        #region Private Methods
        /// <summary>
        /// Merges two test extension lists.
        /// </summary>
        /// 
        /// <typeparam name="TExecutor1">Type of first test extension.</typeparam>
        /// <typeparam name="TExecutor2">Type of second test extension.</typeparam>
        /// <typeparam name="TValue">Type of the value used in the lazy extension expression.</typeparam>
        /// 
        /// <param name="testExtensions1">First test extension list.</param>
        /// <param name="testExtensions2">Second test extension list.</param>
        /// 
        /// <returns>A merged list of test extensions.</returns>
        private static IEnumerable<LazyExtension<TExecutor1, TValue>> MergeTestExtensionLists<TExecutor1, TExecutor2, TValue>(
            IEnumerable<LazyExtension<TExecutor1, TValue>> testExtensions1,
            IEnumerable<LazyExtension<TExecutor2, TValue>> testExtensions2) where TExecutor1 : ITestExecutor where TExecutor2 : TExecutor1
        {
            if (!testExtensions2.Any())
            {
                return testExtensions1;
            }

            var mergedTestExtensions = new List<LazyExtension<TExecutor1, TValue>>();
            var cache = new Dictionary<string, LazyExtension<TExecutor1, TValue>>();

            // Create the cache used for merging by adding all extensions from the first list.
            foreach (var testExtension in testExtensions1)
            {
                cache.Add(testExtension.TestPluginInfo.IdentifierData, testExtension);
            }

            // Update the cache with extensions from the second list. Should there be any conflict
            // we prefer the second extension to the first.
            foreach (var testExtension in testExtensions2)
            {
                if (cache.ContainsKey(testExtension.TestPluginInfo.IdentifierData))
                {
                    cache[testExtension.TestPluginInfo.IdentifierData] =
                        new LazyExtension<TExecutor1, TValue>(
                            (TExecutor1)testExtension.Value, testExtension.Metadata);
                }
            }

            // Create the merged test extensions list from the cache.
            foreach (var kvp in cache)
            {
                mergedTestExtensions.Add(kvp.Value);
            }

            return mergedTestExtensions;
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

                        // Get all extensions for ITestExecutor.
                        TestPluginManager.Instance
                            .GetSpecificTestExtensions<TestExecutorPluginInformation, ITestExecutor, ITestExecutorCapabilities, TestExecutorMetadata>(
                                TestPlatformConstants.TestAdapterEndsWithPattern,
                                out var unfilteredTestExtensions1,
                                out var testExtensions1);

                        // Get all extensions for ITestExecutor2.
                        TestPluginManager.Instance
                            .GetSpecificTestExtensions<TestExecutorPluginInformation2, ITestExecutor2, ITestExecutorCapabilities, TestExecutorMetadata>(
                                TestPlatformConstants.TestAdapterEndsWithPattern,
                                out var unfilteredTestExtensions2,
                                out var testExtensions2);

                        // Merge the extension lists.
                        var mergedUnfilteredTestExtensions = TestExecutorExtensionManager.MergeTestExtensionLists(
                            unfilteredTestExtensions1,
                            unfilteredTestExtensions2);

                        var mergedTestExtensions = TestExecutorExtensionManager.MergeTestExtensionLists(
                            testExtensions1,
                            testExtensions2);

                        // Create the TestExecutorExtensionManager using the merged extension list.
                        testExecutorExtensionManager = new TestExecutorExtensionManager(
                            mergedUnfilteredTestExtensions, mergedTestExtensions, TestSessionMessageLogger.Instance);
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

            // Get all extensions for ITestExecutor.
            TestPluginManager.Instance
                .GetTestExtensions<TestExecutorPluginInformation, ITestExecutor, ITestExecutorCapabilities, TestExecutorMetadata>(
                    extensionAssembly,
                    out var unfilteredTestExtensions1,
                    out var testExtensions1);

            // Get all extensions for ITestExecutor2.
            TestPluginManager.Instance
                .GetTestExtensions<TestExecutorPluginInformation2, ITestExecutor2, ITestExecutorCapabilities, TestExecutorMetadata>(
                    extensionAssembly,
                    out var unfilteredTestExtensions2,
                    out var testExtensions2);

            // Merge the extension lists.
            var mergedUnfilteredTestExtensions = TestExecutorExtensionManager.MergeTestExtensionLists(
                    unfilteredTestExtensions1,
                    unfilteredTestExtensions2);

            var mergedTestExtensions = TestExecutorExtensionManager.MergeTestExtensionLists(
                testExtensions1,
                testExtensions2);

            // TODO: This can be optimized - The base class's populate map would be called repeatedly for the same extension assembly.
            // Have a single instance of TestExecutorExtensionManager that keeps populating the map iteratively.
            return new TestExecutorExtensionManager(
                mergedUnfilteredTestExtensions,
                mergedTestExtensions,
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
                        "TestExecutorExtensionManager: LoadAndInitialize: Exception occurred while loading extensions {0}",
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
