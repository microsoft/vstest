// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework;

using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Microsoft.TestPlatform.TestUtilities;

[TestClass]
public class TestExecutorExtensionManagerTests
{
    [TestCleanup]
    public void TestCleanup()
    {
        TestExecutorExtensionManager.Destroy();
    }

    [TestMethod]
    public void CreateShouldDiscoverExecutorExtensions()
    {
        TestPluginCacheHelper.SetupMockExtensions(typeof(TestExecutorExtensionManagerTests));

        var extensionManager = TestExecutorExtensionManager.Create();

        Assert.IsNotNull(extensionManager.TestExtensions);
        Assert.IsTrue(extensionManager.TestExtensions.Any());
    }

    [TestMethod]
    public void CreateShouldCacheDiscoveredExtensions()
    {
        TestPluginCacheHelper.SetupMockExtensions(typeof(TestExecutorExtensionManagerTests), () => { });

        var extensionManager = TestExecutorExtensionManager.Create();
        TestExecutorExtensionManager.Create();

        Assert.IsNotNull(extensionManager.TestExtensions);
        Assert.IsTrue(extensionManager.TestExtensions.Any());
    }

    [TestMethod]
    public void GetExecutorExtensionManagerShouldReturnAnExecutionManagerWithExtensions()
    {
        var extensionManager =
            TestExecutorExtensionManager.GetExecutionExtensionManager(
                typeof(TestExecutorExtensionManagerTests).GetTypeInfo().Assembly.Location);

        Assert.IsNotNull(extensionManager.TestExtensions);
        Assert.IsTrue(extensionManager.TestExtensions.Any());
    }

    #region LoadAndInitialize tests

    [TestMethod]
    public void LoadAndInitializeShouldInitializeAllExtensions()
    {
        TestPluginCacheHelper.SetupMockExtensions(typeof(TestExecutorExtensionManagerTests));

        TestExecutorExtensionManager.LoadAndInitializeAllExtensions(false);

        var allExecutors = TestExecutorExtensionManager.Create().TestExtensions;

        foreach (var executor in allExecutors)
        {
            Assert.IsTrue(executor.IsExtensionCreated);
        }
    }

    #endregion
}

[TestClass]
public class TestExecutorMetadataTests
{
    [TestMethod]
    public void TestExecutorMetadataCtorShouldSetExtensionUri()
    {
        var metadata = new TestExecutorMetadata("random");

        Assert.AreEqual("random", metadata.ExtensionUri);
    }
}
