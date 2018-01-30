// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using global::TestPlatform.Common.UnitTests.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class TestLoggerExtensionManagerTests
    {
        [TestInitialize]
        public void Initialize()
        {
            TestPluginCacheTests.SetupMockExtensions();
        }

        [TestMethod]
        public void CreateShouldThrowExceptionIfMessageLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                var testLoggerExtensionManager = TestLoggerExtensionManager.Create(null);
            });

        }

        [TestMethod]
        public void CreateShouldReturnInstanceOfTestLoggerExtensionManager()
        {
            try
            {
                var testLoggerExtensionManager = TestLoggerExtensionManager.Create(TestSessionMessageLogger.Instance);
                Assert.IsNotNull(testLoggerExtensionManager);
                Assert.IsInstanceOfType(testLoggerExtensionManager, typeof(TestLoggerExtensionManager));
            }
            finally
            {
                TestSessionMessageLogger.Instance = null;
            }
        }
    }
}
