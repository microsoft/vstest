// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyOperationManagerTests
    {
        private TestableProxyOperationManager testOperationManager;

        private Mock<ITestHostManager> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestHostManager = new Mock<ITestHostManager>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.testOperationManager = new TestableProxyOperationManager(this.mockRequestSender.Object, this.testableClientConnectionTimeout);
        }

        [TestMethod]
        public void InitializeShouldLaunchTestHost()
        {
            this.testOperationManager.Initialize(this.mockTestHostManager.Object);

            // construct the command line arguments.
            var commandLineArguments = new List<string> { "--port", "0" };
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHost(null, commandLineArguments), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldSetupServerForCommunication()
        {
            this.testOperationManager.Initialize(this.mockTestHostManager.Object);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
        }

        #region Testable Implementation

        private class TestableProxyOperationManager : ProxyOperationManager
        {
            public TestableProxyOperationManager()
                : base()
            {
            }

            internal TestableProxyOperationManager(
                ITestRequestSender requestSender,
                int clientConnectionTimeout)
                : base(requestSender, null, clientConnectionTimeout)
            {
            }
        }

        #endregion
    }
}
