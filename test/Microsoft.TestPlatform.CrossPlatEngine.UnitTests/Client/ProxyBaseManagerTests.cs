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
        private Mock<ICommunicationEndPoint> mockCommunicationEndpoint;
        private ITestRequestSender testRequestSender;
        readonly ProtocolConfig protocolConfig = new() { Version = 2 };
        private readonly Mock<IRequestData> mockRequestData;
        protected readonly Mock<ITestRuntimeProvider> mockTestHostManager;
        protected Mock<IDataSerializer> mockDataSerializer;
        protected Mock<ICommunicationChannel> mockChannel;
        private readonly Mock<IFileHelper> mockFileHelper;

        public ProxyBaseManagerTests()
        {
            mockTestHostManager = new Mock<ITestRuntimeProvider>();
            mockDataSerializer = new Mock<IDataSerializer>();
            mockRequestData = new Mock<IRequestData>();
            mockChannel = new Mock<ICommunicationChannel>();
            mockFileHelper = new Mock<IFileHelper>();

            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(new Mock<IMetricsCollection>().Object);
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(null)).Returns(new Message());
            mockDataSerializer.Setup(mds => mds.DeserializeMessage(string.Empty)).Returns(new Message());
            mockTestHostManager.SetupGet(th => th.Shared).Returns(true);
            mockTestHostManager.Setup(
                    m => m.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(new TestProcessStartInfo());
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(
                    () => mockTestHostManager.Raise(thm => thm.HostLaunched += null, new HostProviderEventArgs(string.Empty)))
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
            mockCommunicationEndpoint = new Mock<ICommunicationEndPoint>();
            mockDataSerializer = new Mock<IDataSerializer>();
            testRequestSender = new TestRequestSender(mockCommunicationEndpoint.Object, connectionInfo, mockDataSerializer.Object, protocolConfig, CLIENTPROCESSEXITWAIT);
            mockCommunicationEndpoint.Setup(mc => mc.Start(connectionInfo.Endpoint)).Returns(connectionInfo.Endpoint).Callback(() => mockCommunicationEndpoint.Raise(
                    s => s.Connected += null,
                    mockCommunicationEndpoint.Object,
                    new ConnectedEventArgs(mockChannel.Object)));
            SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, protocolConfig.Version);

            testRequestSender.InitializeCommunication();
        }

        public void SetupChannelMessage<TPayload>(string messageType, string returnMessageType, TPayload returnPayload)
        {
            mockChannel.Setup(mc => mc.Send(It.Is<string>(s => s.Contains(messageType))))
                .Callback(() => mockChannel.Raise(c => c.MessageReceived += null, mockChannel.Object, new MessageReceivedEventArgs { Data = messageType }));

            mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>())).Returns(messageType);
            mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>(), It.IsAny<int>())).Returns(messageType);
            mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.Is<string>(s => s.Equals(messageType)))).Returns(new Message { MessageType = returnMessageType });
            mockDataSerializer.Setup(ds => ds.DeserializePayload<TPayload>(It.Is<Message>(m => m.MessageType.Equals(messageType)))).Returns(returnPayload);
        }

        public void RaiseMessageReceived(string data)
        {
            mockChannel.Raise(c => c.MessageReceived += null, mockChannel.Object,
                new MessageReceivedEventArgs { Data = data });
        }

        protected ProxyDiscoveryManager GetProxyDiscoveryManager()
        {
            SetupAndInitializeTestRequestSender();
            var testDiscoveryManager = new ProxyDiscoveryManager(
                mockRequestData.Object,
                testRequestSender,
                mockTestHostManager.Object,
                mockDataSerializer.Object,
                mockFileHelper.Object);

            return testDiscoveryManager;
        }

        internal ProxyExecutionManager GetProxyExecutionManager()
        {
            SetupAndInitializeTestRequestSender();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            var testExecutionManager = new ProxyExecutionManager(mockRequestData.Object, testRequestSender,
                mockTestHostManager.Object, mockDataSerializer.Object, mockFileHelper.Object);

            return testExecutionManager;
        }
    }
}
