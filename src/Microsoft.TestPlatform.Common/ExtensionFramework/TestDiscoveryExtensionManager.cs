// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;

using System;
using System.Collections.Generic;

using Utilities;
using Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using ObjectModel;
using ObjectModel.Adapter;

/// <summary>
/// Responsible for managing the Test Discoverer extensions which are available.
/// </summary>
internal class TestDiscoveryExtensionManager
{
    private static TestDiscoveryExtensionManager s_testDiscoveryExtensionManager;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <remarks>The factory should be used for getting instances of this type so the constructor is not public.</remarks>
    protected TestDiscoveryExtensionManager(
        IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> discoverers,
        IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> unfilteredDiscoverers)
    {
        ValidateArg.NotNull(discoverers, nameof(discoverers));
        ValidateArg.NotNull(unfilteredDiscoverers, nameof(unfilteredDiscoverers));

        Discoverers = discoverers;
        UnfilteredDiscoverers = unfilteredDiscoverers;
    }

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
        if (s_testDiscoveryExtensionManager == null)
        {
            TestPluginManager.GetSpecificTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                    TestPlatformConstants.TestAdapterEndsWithPattern,
                    out var unfilteredTestExtensions,
                    out var testExtensions);

            s_testDiscoveryExtensionManager = new TestDiscoveryExtensionManager(
                testExtensions,
                unfilteredTestExtensions);
        }

        return s_testDiscoveryExtensionManager;
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

        TestPluginManager.GetTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                extensionAssembly,
                out var unfilteredTestExtensions,
                out var testExtensions);

        return new TestDiscoveryExtensionManager(
            testExtensions,
            unfilteredTestExtensions);
    }

    /// <summary>
    /// Loads and Initializes all the extensions.
    /// </summary>
    /// <param name="throwOnError"> The throw On Error. </param>
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
            EqtTrace.Error("TestDiscoveryManager: LoadExtensions: Exception occurred while loading extensions {0}", ex);

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
        s_testDiscoveryExtensionManager = null;
    }

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
    public TestDiscovererMetadata(IReadOnlyCollection<string> fileExtensions, string defaultExecutorUri, AssemblyType assemblyType = default)
    {
        if (fileExtensions != null && fileExtensions.Count > 0)
        {
            FileExtension = new List<string>(fileExtensions);
        }

        if (!string.IsNullOrWhiteSpace(defaultExecutorUri))
        {
            DefaultExecutorUri = new Uri(defaultExecutorUri);
        }

        AssemblyType = assemblyType;
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

    /// <summary>
    /// Gets assembly type supported by the discoverer.
    /// </summary>
    public AssemblyType AssemblyType
    {
        get;
        private set;
    }
}
