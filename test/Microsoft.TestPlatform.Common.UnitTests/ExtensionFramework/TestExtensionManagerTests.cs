// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestExtensionManagerTests
    {
        private IMessageLogger messageLogger;
        private TestExtensionManager<ITestLogger, ITestLoggerCapabilities> testExtensionManager;
        private IEnumerable<LazyExtension<ITestLogger, ITestLoggerCapabilities>> filteredTestExtensions;
        private IEnumerable<LazyExtension<ITestLogger, Dictionary<string, object>>> unfilteredTestExtensions;

        public TestExtensionManagerTests()
        {
            TestPluginCacheTests.SetupMockExtensions();
            messageLogger = TestSessionMessageLogger.Instance;
            TestPluginManager.Instance.GetSpecificTestExtensions<TestLoggerPluginInformation, ITestLogger, ITestLoggerCapabilities, TestLoggerMetadata>
                (TestPlatformConstants.TestLoggerEndsWithPattern, out unfilteredTestExtensions, out filteredTestExtensions);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestSessionMessageLogger.Instance = null;
        }

        [TestMethod]
        public void TestExtensionManagerConstructorShouldThrowExceptionIfMessageLoggerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    testExtensionManager = new DummyTestExtensionManager(unfilteredTestExtensions, filteredTestExtensions, null);
                }
            );
        }

        [TestMethod]
        public void TryGetTestExtensionShouldReturnExtensionWithCorrectUri()
        {
            testExtensionManager = new DummyTestExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
            var result = testExtensionManager.TryGetTestExtension(new Uri("testlogger://logger"));

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Value, typeof(ITestLogger));

        }

        [TestMethod]
        public void TryGetTestExtensionShouldThrowExceptionWithNullUri()
        {
            testExtensionManager = new DummyTestExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
            TestPluginCacheTests.SetupMockAdditionalPathExtensions();
            Assert.ThrowsException<ArgumentNullException>(() =>
                    {
                        var result = testExtensionManager.TryGetTestExtension(default(Uri));
                    }
            );
        }

        [TestMethod]
        public void TryGetTestExtensionShouldNotReturnExtensionWithIncorrectlUri()
        {
            testExtensionManager = new DummyTestExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
            var result = testExtensionManager.TryGetTestExtension("");
            Assert.IsNull(result);
        }


        [TestMethod]
        public void TryGetTestExtensionWithStringUriUnitTest()
        {
            testExtensionManager = new DummyTestExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
            var result = testExtensionManager.TryGetTestExtension(new Uri("testlogger://logger").AbsoluteUri);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Value, typeof(ITestLogger));
        }

        [ExtensionUri("testlogger://logger")]
        [FriendlyName("TestLoggerExtension")]
        private class ValidLogger3 : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                events.TestRunMessage += TestMessageHandler;
                events.TestRunComplete += Events_TestRunComplete;
                events.TestResult += Events_TestResult;
            }

            private void Events_TestResult(object sender, TestResultEventArgs e)
            {
            }

            private void Events_TestRunComplete(object sender, TestRunCompleteEventArgs e)
            {

            }

            private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
            {
            }
        }
    }

    internal class DummyTestExtensionManager : TestExtensionManager<ITestLogger, ITestLoggerCapabilities>
    {
        public DummyTestExtensionManager(IEnumerable<LazyExtension<ITestLogger, Dictionary<string, object>>> unfilteredTestExtensions, IEnumerable<LazyExtension<ITestLogger, ITestLoggerCapabilities>> testExtensions, IMessageLogger logger) : base(unfilteredTestExtensions, testExtensions, logger)
        {
        }
    }
}
