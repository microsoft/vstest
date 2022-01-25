// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Logging
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestSessionMessageLoggerTests
    {
        private TestSessionMessageLogger testSessionMessageLogger;

        private TestRunMessageEventArgs currentEventArgs;

        [TestInitialize]
        public void TestInit()
        {
            testSessionMessageLogger = TestSessionMessageLogger.Instance;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestSessionMessageLogger.Instance = null;
        }

        [TestMethod]
        public void InstanceShouldReturnALoggerInstance()
        {
            Assert.IsNotNull(testSessionMessageLogger);
        }

        [TestMethod]
        public void SendMessageShouldLogErrorMessages()
        {
            testSessionMessageLogger.TestRunMessage += OnMessage;

            var message = "Alert";
            testSessionMessageLogger.SendMessage(TestMessageLevel.Error, message);

            Assert.AreEqual(TestMessageLevel.Error, currentEventArgs.Level);
            Assert.AreEqual(message, currentEventArgs.Message);
        }

        [TestMethod]
        public void SendMessageShouldLogErrorAsWarningIfSpecifiedSo()
        {
            testSessionMessageLogger.TestRunMessage += OnMessage;
            testSessionMessageLogger.TreatTestAdapterErrorsAsWarnings = true;

            var message = "Alert";
            testSessionMessageLogger.SendMessage(TestMessageLevel.Error, message);

            Assert.AreEqual(TestMessageLevel.Warning, currentEventArgs.Level);
            Assert.AreEqual(message, currentEventArgs.Message);
        }

        private void OnMessage(object sender, TestRunMessageEventArgs e)
        {
            currentEventArgs = e;
        }
    }
}
