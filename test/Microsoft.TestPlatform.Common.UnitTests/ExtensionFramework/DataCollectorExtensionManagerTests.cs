// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System;

    using global::TestPlatform.Common.UnitTests.ExtensionFramework;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class DataCollectorExtensionManagerTests
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
                var dataCollectionExtensionManager = DataCollectorExtensionManager.Create(null);
            });
        }

        [TestMethod]
        public void CreateShouldReturnInstanceOfDataCollectorExtensionManager()
        {
            try
            {
                var dataCollectorExtensionManager = DataCollectorExtensionManager.Create(TestSessionMessageLogger.Instance);
                Assert.IsNotNull(dataCollectorExtensionManager);
                Assert.IsInstanceOfType(dataCollectorExtensionManager, typeof(DataCollectorExtensionManager));
            }
            finally
            {
                TestSessionMessageLogger.Instance = null;
            }
        }
    }
}
