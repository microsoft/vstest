// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.TestRunnerConnectionInfo;
    using System.Net;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

    [TestClass]
    public class TestRequestHandlerTests
    {
        private ITestRequestHandler testRequestHandler;
        private Mock<ICommunicationManager> mockCommunicationManager;
        private Mock<IDataSerializer> mockDataSerializer;
        private ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };
        private ConnectionInfo connectionInfo;

        [TestInitialize]
        public void TestInit()
        {
            this.connectionInfo = new ConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Client,
                                          Channel = TransportChannel.Sockets
                                      };
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.testRequestHandler = new TestRequestHandler(this.mockCommunicationManager.Object, this.connectionInfo, this.mockDataSerializer.Object);
        }

        [TestMethod]
        public void InitializeCommunicationShouldActAsClientIfTestHostIsClient()
        {
            this.mockCommunicationManager.Setup(mc => mc.SetupClientAsync(IPAddress.Loopback + ":0"));

            this.testRequestHandler.InitializeCommunication();

            this.mockCommunicationManager.Verify(mc => mc.SetupClientAsync(IPAddress.Loopback + ":0"), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldHostServerAndAcceptClientIfTestHostIsServer()
        {
            this.mockCommunicationManager.Setup(mc => mc.HostServer(IPAddress.Loopback + ":0")).Returns(123);

            // These settings are that received by(testhost)
            this.connectionInfo = new ConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Host,
                                          Channel = TransportChannel.Sockets
                                      };
            this.testRequestHandler = new TestRequestHandler(this.mockCommunicationManager.Object, this.connectionInfo, this.mockDataSerializer.Object);

            this.testRequestHandler.InitializeCommunication();

            this.mockCommunicationManager.Verify(mc => mc.HostServer(IPAddress.Loopback + ":0"), Times.Once);
            this.mockCommunicationManager.Verify(mc => mc.AcceptClientAsync(), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldCallWaitForServerConnection()
        {
            this.testRequestHandler.WaitForRequestSenderConnection(123);

            this.mockCommunicationManager.Verify(mc => mc.WaitForServerConnection(123), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestHandlerConnectionShouldCallWaitForClientConnectionIfTestRunnerIsServer()
        {
            // These settings are that recieved by Test runtime(testhost)
            this.connectionInfo = new ConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Host,
                                          Channel = TransportChannel.Sockets
                                      };

            this.testRequestHandler = new TestRequestHandler(this.mockCommunicationManager.Object, this.connectionInfo, this.mockDataSerializer.Object);

            this.testRequestHandler.WaitForRequestSenderConnection(123);

            this.mockCommunicationManager.Verify(mc => mc.WaitForClientConnection(123), Times.Once);
        }

        [TestMethod]
        public void CloseShouldCallStopClientOnCommunicationManager()
        {
            this.testRequestHandler.Close();

            this.mockCommunicationManager.Verify(mc => mc.StopClient(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldCallStopServerOnCommunicationManagerIfTestRunnerIsServer()
        {
            // These settings are that recieved by Test runtime(testhost)
            this.connectionInfo = new ConnectionInfo
                                      {
                                          Endpoint = IPAddress.Loopback + ":0",
                                          Role = ConnectionRole.Host,
                                          Channel = TransportChannel.Sockets
                                      };

            this.testRequestHandler = new TestRequestHandler(this.mockCommunicationManager.Object, this.connectionInfo, this.mockDataSerializer.Object);
            this.testRequestHandler.Close();

            this.mockCommunicationManager.Verify(mc => mc.StopServer(), Times.Once);
        }
    }
}
