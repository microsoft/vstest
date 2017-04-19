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

        [TestMethod]
        public void UseAdditionalExtensionsShouldUpdateAdditionalExtensionsInCache()
        {
            TestPluginCache.Instance = null;
            var extensions = new List<string> { typeof(TestExtensionManagerTests).GetTypeInfo().Assembly.Location };

            try
            {
                this.testExtensionManager.UseAdditionalExtensions(extensions, true);

                Assert.IsTrue(TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
                CollectionAssert.AreEqual(extensions, TestPluginCache.Instance.PathToExtensions.ToList());
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }
    }
}
