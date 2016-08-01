// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.Logging
{
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using TestPlatform.Common.UnitTests.ExtensionFramework;

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
