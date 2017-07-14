// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.CoverageLogger.UnitTests
{
    using System;

    using Microsoft.VisualStudio.Setup.Interop;
    using Microsoft.VisualStudio.TestPlatform.Extensions.CoverageLogger;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class CoverageLoggerTests
    {
        private Mock<TestLoggerEvents> events;
        private CoverageLogger coverageLogger;
        private Mock<ISetupConfiguration> mockVSSetUp;

        public CoverageLoggerTests()
        {
            this.events = new Mock<TestLoggerEvents>();
            this.coverageLogger = new CoverageLogger();
            this.mockVSSetUp = new Mock<ISetupConfiguration>();
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.coverageLogger.Initialize(null, string.Empty);
                    });
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfEventsIsNotNull()
        {
            this.coverageLogger.Initialize(this.events.Object, string.Empty);
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldNotThrowExceptionIfAttachmentSetIsNull()
        {
            this.coverageLogger.TestRunCompleteHandler(new object(), CreateTestRunCompleteEventArgs());
        }

        [TestMethod]
        public void CodeCoverageExeNotFoundIfNoEnterpriseVSIsInstalled()
        {
            this.mockVSSetUp.Setup(setup => setup.EnumInstances()).Returns((IEnumSetupInstances)null);
            Assert.AreEqual(string.Empty, this.coverageLogger.GetCodeCoverageInstallPath(this.mockVSSetUp.Object));
        }

        private static TestRunCompleteEventArgs CreateTestRunCompleteEventArgs()
        {
            var testRunCompleteEventArgs = new TestRunCompleteEventArgs(
                null,
                false,
                false,
                null,
                null,
                new TimeSpan(1, 0, 0, 0));

            return testRunCompleteEventArgs;
        }
    }
}
