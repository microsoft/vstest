// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class ProxyBaseManagerTests
    {
        private const int CLIENTPROCESSEXITWAIT = 10 * 1000;
        private int clientConnectionTimeout = 400;
        private Mock<ICommunicationEndPoint> mockCommunicationEndpoint;
        private ITestRequestSender testRequestSender;

        ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };
        private readonly Mock<IRequestData> mockRequestData;
        protected readonly Mock<ITestRuntimeProvider> mockTestHostManager;
        protected Mock<IDataSerializer> mockDataSerializer;
        protected Mock<ICommunicationChannel> mockChannel;
        private Mock<IFileHelper> mockFileHelper;

        public ProxyBaseManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockChannel = new Mock<ICommunicationChannel>();
            this.mockFileHelper = new Mock<IFileHelper>();

            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new Mock<IMetricsCollection>().Object);
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(null)).Returns(new Message());
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(string.Empty)).Returns(new Message());
            this.mockTestHostManager.SetupGet(th => th.Shared).Returns(true);
            this.mockTestHostManager.Setup(
                    m => m.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(new TestProcessStartInfo());
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(
                    () =>
                    {
                        this.mockTestHostManager.Raise(thm => thm.HostLaunched += null, new HostProviderEventArgs(string.Empty));
                    })
                .Returns(Task.FromResult(true));
        }

        private void SetupAndInitializeTestRequestSender()
        {
            var connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":0",
                Role = ConnectionRole.Client,
                Transport = Transport.Sockets
            };
            this.mockCommunicationEndpoint = new Mock<ICommunicationEndPoint>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.testRequestSender = new TestRequestSender(this.mockCommunicationEndpoint.Object, connectionInfo, this.mockDataSerializer.Object, this.protocolConfig, CLIENTPROCESSEXITWAIT);
            this.mockCommunicationEndpoint.Setup(mc => mc.Start(connectionInfo.Endpoint)).Returns(connectionInfo.Endpoint).Callback(() =>
            {
                this.mockCommunicationEndpoint.Raise(
                    s => s.Connected += null,
                    this.mockCommunicationEndpoint.Object,
                    new ConnectedEventArgs(this.mockChannel.Object));
            });
            this.SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, this.protocolConfig.Version);

            this.testRequestSender.InitializeCommunication();
        }

        public void SetupChannelMessage<TPayload>(string messageType, string returnMessageType, TPayload returnPayload)
        {
            this.mockChannel.Setup(mc => mc.Send(It.Is<string>(s => s.Contains(messageType))))
                .Callback(() => this.mockChannel.Raise(c => c.MessageReceived += null, this.mockChannel.Object, new MessageReceivedEventArgs { Data = messageType }));

            this.mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>())).Returns(messageType);
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>(), It.IsAny<int>())).Returns(messageType);
            this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.Is<string>(s => s.Equals(messageType)))).Returns(new Message { MessageType = returnMessageType });
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TPayload>(It.Is<Message>(m => m.MessageType.Equals(messageType)))).Returns(returnPayload);
        }

        public void RaiseMessageReceived(string data)
        {
            this.mockChannel.Raise(c => c.MessageReceived += null, this.mockChannel.Object,
                new MessageReceivedEventArgs { Data = data });
        }

        protected ProxyDiscoveryManager GetProxyDiscoveryManager()
        {
            this.SetupAndInitializeTestRequestSender();
            var testDiscoveryManager = new ProxyDiscoveryManager(
                mockRequestData.Object,
                testRequestSender,
                mockTestHostManager.Object,
                mockDataSerializer.Object,
                clientConnectionTimeout,
                this.mockFileHelper.Object);

            return testDiscoveryManager;
        }

        internal ProxyExecutionManager GetProxyExecutionManager()
        {
            this.SetupAndInitializeTestRequestSender();
            var testExecutionManager = new ProxyExecutionManager(mockRequestData.Object, testRequestSender,
                mockTestHostManager.Object, mockDataSerializer.Object, clientConnectionTimeout, this.mockFileHelper.Object);

            return testExecutionManager;
        }
    }
}
