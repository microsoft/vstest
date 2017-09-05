// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestExtensionManagerTests
    {
        private ITestExtensionManager testExtensionManager;

        public TestExtensionManagerTests()
        {
            this.testExtensionManager = new TestExtensionManager();
        }

        [TestCleanup]
        public void CleanUp()
        {
            TestPluginCache.Instance = null;
        }

        [TestMethod]
        public void UseAdditionalExtensionsShouldUpdateAdditionalExtensionsInCache()
        {
            var extensions = new List<string> { typeof(TestExtensionManagerTests).GetTypeInfo().Assembly.Location };

            this.testExtensionManager.UseAdditionalExtensions(extensions, true);

            Assert.IsTrue(TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
            CollectionAssert.AreEqual(extensions, TestPluginCache.Instance.PathToExtensions.ToList());
        }

        [TestMethod]
        public void ClearExtensionsShouldClearExtensionsInCache()
        {
            var extensions = new List<string> { @"Foo.dll" };
            this.testExtensionManager.UseAdditionalExtensions(extensions, true);

            this.testExtensionManager.ClearExtensions();

            Assert.AreEqual(0, TestPluginCache.Instance.PathToExtensions.Count());
        }
    }
}
