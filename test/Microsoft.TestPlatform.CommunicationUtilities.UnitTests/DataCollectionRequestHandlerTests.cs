// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;

    using Microsoft.TestPlatform.CommunicationUtilities.UnitTests.TestDoubles;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Newtonsoft.Json.Linq;
    using VisualStudio.TestPlatform.CoreUtilities.Helpers;

    using CommunicationUtilitiesResources = VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = VisualStudio.TestPlatform.CoreUtilities.Constants;

    [TestClass]
    public class DataCollectionRequestHandlerTests
    {
        private readonly Mock<ICommunicationManager> mockCommunicationManager;
        private readonly Mock<IMessageSink> mockMessageSink;
        private readonly Mock<IDataCollectionManager> mockDataCollectionManager;
        private readonly Mock<IDataCollectionTestCaseEventHandler> mockDataCollectionTestCaseEventHandler;
        private readonly TestableDataCollectionRequestHandler requestHandler;
        private readonly Mock<IDataSerializer> mockDataSerializer;
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IMetricsCollection> mockMetricsCollection;
        private readonly Message afterTestRunEnd = new() { MessageType = MessageType.AfterTestRunEnd, Payload = "false" };
        private readonly Message beforeTestRunStart = new()
        {
            MessageType = MessageType.BeforeTestRunStart,
            Payload = JToken.FromObject(new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } })
        };

        public DataCollectionRequestHandlerTests()
        {
            mockCommunicationManager = new Mock<ICommunicationManager>();
            mockMessageSink = new Mock<IMessageSink>();
            mockDataCollectionManager = new Mock<IDataCollectionManager>();
            mockDataSerializer = new Mock<IDataSerializer>();
            mockDataCollectionTestCaseEventHandler = new Mock<IDataCollectionTestCaseEventHandler>();
            mockDataCollectionTestCaseEventHandler.Setup(x => x.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            mockFileHelper = new Mock<IFileHelper>();
            mockRequestData = new Mock<IRequestData>();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(r => r.MetricsCollection).Returns(mockMetricsCollection.Object);
            requestHandler = new TestableDataCollectionRequestHandler(mockCommunicationManager.Object, mockMessageSink.Object, mockDataCollectionManager.Object, mockDataCollectionTestCaseEventHandler.Object, mockDataSerializer.Object, mockFileHelper.Object, mockRequestData.Object);

            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(beforeTestRunStart).Returns(afterTestRunEnd);

            mockDataCollectionManager.Setup(x => x.SessionStarted(It.IsAny<SessionStartEventArgs>())).Returns(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, string.Empty);
        }

        [TestMethod]
        public void CreateInstanceShouldThrowExceptionIfInstanceCommunicationManagerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DataCollectionRequestHandler.Create(null, mockMessageSink.Object));
        }

        [TestMethod]
        public void CreateInstanceShouldThrowExceptinIfInstanceMessageSinkIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DataCollectionRequestHandler.Create(mockCommunicationManager.Object, null));
        }

        [TestMethod]
        public void CreateInstanceShouldCreateInstance()
        {
            var result = DataCollectionRequestHandler.Create(mockCommunicationManager.Object, mockMessageSink.Object);

            Assert.AreEqual(result, DataCollectionRequestHandler.Instance);
        }

        [TestMethod]
        public void InitializeCommunicationShouldInitializeCommunication()
        {
            requestHandler.InitializeCommunication(123);

            mockCommunicationManager.Verify(x => x.SetupClientAsync(new IPEndPoint(IPAddress.Loopback, 123)), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.SetupClientAsync(It.IsAny<IPEndPoint>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => requestHandler.InitializeCommunication(123));
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldInvokeCommunicationManager()
        {
            requestHandler.WaitForRequestSenderConnection(0);

            mockCommunicationManager.Verify(x => x.WaitForServerConnection(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.WaitForServerConnection(It.IsAny<int>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => requestHandler.WaitForRequestSenderConnection(0));
        }

        [TestMethod]
        public void SendDataCollectionMessageShouldSendMessageToCommunicationManager()
        {
            var message = new DataCollectionMessageEventArgs(TestMessageLevel.Error, "message");

            requestHandler.SendDataCollectionMessage(message);

            mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionMessage, message), Times.Once);
        }

        [TestMethod]
        public void SendDataCollectionMessageShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionMessage, It.IsAny<DataCollectionMessageEventArgs>())).Throws<Exception>();
            var message = new DataCollectionMessageEventArgs(TestMessageLevel.Error, "message");

            Assert.ThrowsException<Exception>(() => requestHandler.SendDataCollectionMessage(message));
        }

        [TestMethod]
        public void CloseShouldCloseCommunicationChannel()
        {
            requestHandler.Close();

            mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.StopClient()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => requestHandler.Close());
        }

        [TestMethod]
        public void DisposeShouldCloseCommunicationChannel()
        {
            requestHandler.Dispose();

            mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldProcessRequests()
        {
            var testHostLaunchedPayload = new TestHostLaunchedPayload();
            testHostLaunchedPayload.ProcessId = 1234;

            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(beforeTestRunStart)
                                                                                .Returns(new Message() { MessageType = MessageType.TestHostLaunched, Payload = JToken.FromObject(testHostLaunchedPayload) })
                                                                                .Returns(afterTestRunEnd);

            mockDataCollectionManager.Setup(x => x.SessionStarted(It.IsAny<SessionStartEventArgs>())).Returns(true);
            mockDataCollectionManager.Setup(x => x.TestHostLaunched(It.IsAny<int>()));
            mockDataSerializer.Setup(x => x.DeserializePayload<TestHostLaunchedPayload>(It.Is<Message>(y => y.MessageType == MessageType.TestHostLaunched)))
                                   .Returns(testHostLaunchedPayload);
            var beforeTestRunSTartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunSTartPayload);

            requestHandler.ProcessRequests();

            mockDataCollectionTestCaseEventHandler.Verify(x => x.InitializeCommunication(), Times.Once);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.WaitForRequestHandlerConnection(It.IsAny<int>()), Times.Once);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.ProcessRequests(), Times.Once);

            // Verify SessionStarted events
            mockDataCollectionManager.Verify(x => x.SessionStarted(It.IsAny<SessionStartEventArgs>()), Times.Once);
            mockCommunicationManager.Verify(x => x.SendMessage(MessageType.BeforeTestRunStartResult, It.IsAny<BeforeTestRunStartResult>()), Times.Once);

            // Verify TestHostLaunched events
            mockDataCollectionManager.Verify(x => x.TestHostLaunched(1234), Times.Once);

            // Verify AfterTestRun events.
            mockDataCollectionManager.Verify(x => x.SessionEnded(It.IsAny<bool>()), Times.Once);
            mockCommunicationManager.Verify(x => x.SendMessage(MessageType.AfterTestRunEndResult, It.IsAny<AfterTestRunEndResult>()), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldDisposeDataCollectorsOnAfterTestRunEnd()
        {
            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.AfterTestRunEnd, Payload = "false" });

            requestHandler.ProcessRequests();

            mockDataCollectionManager.Verify(x => x.Dispose());
        }

        [TestMethod]
        public void ProcessRequestsShouldAddSourceDirectoryToTestPluginCache()
        {
            var testHostLaunchedPayload = new TestHostLaunchedPayload();
            testHostLaunchedPayload.ProcessId = 1234;

            var temp = Path.GetTempPath();
            string runSettings = "<RunSettings><RunConfiguration><TestAdaptersPaths></TestAdaptersPaths></RunConfiguration></RunSettings>";

            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(beforeTestRunStart)
                                                                                .Returns(new Message() { MessageType = MessageType.TestHostLaunched, Payload = JToken.FromObject(testHostLaunchedPayload) })
                                                                                .Returns(afterTestRunEnd);

            mockDataCollectionManager.Setup(x => x.SessionStarted(It.IsAny<SessionStartEventArgs>())).Returns(true);
            mockDataCollectionManager.Setup(x => x.TestHostLaunched(It.IsAny<int>()));
            mockDataSerializer.Setup(x => x.DeserializePayload<TestHostLaunchedPayload>(It.Is<Message>(y => y.MessageType == MessageType.TestHostLaunched)))
                                   .Returns(testHostLaunchedPayload);
            var beforeTestRunSTartPayload = new BeforeTestRunStartPayload
            {
                SettingsXml = runSettings,
                Sources = new List<string>
                {
                    Path.Combine(temp, "dir1", "test1.dll"),
                    Path.Combine(temp, "dir2", "test2.dll"),
                    Path.Combine(temp, "dir3", "test3.dll")
                }
            };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunSTartPayload);
            mockFileHelper.Setup(x => x.DirectoryExists($@"{temp}dir1")).Returns(true);
            mockFileHelper.Setup(x => x.EnumerateFiles($@"{temp}dir1", SearchOption.AllDirectories, @"Collector.dll")).Returns(new List<string> { Path.Combine(temp, "dir1", "abc.DataCollector.dll") });

            requestHandler.ProcessRequests();

            mockFileHelper.Verify(x => x.EnumerateFiles($@"{temp}dir1", SearchOption.AllDirectories, @"Collector.dll"), Times.Once);
            Assert.IsTrue(TestPluginCache.Instance.GetExtensionPaths(@"Collector.dll").Contains(Path.Combine(temp, "dir1", "abc.DataCollector.dll")));
        }

        [TestMethod]
        public void ProcessRequestsShouldThrowExceptionIfThrownByCommunicationManager()
        {
            mockCommunicationManager.Setup(x => x.ReceiveMessage()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() => requestHandler.ProcessRequests());
        }

        [TestMethod]
        public void ProcessRequestsShouldInitializeTestCaseEventHandlerIfTestCaseLevelEventsAreEnabled()
        {
            var beforeTestRunSTartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunSTartPayload);

            requestHandler.ProcessRequests();

            mockDataCollectionTestCaseEventHandler.Verify(x => x.InitializeCommunication(), Times.Once);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.ProcessRequests(), Times.Once);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.WaitForRequestHandlerConnection(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestsShouldSetDefaultTimeoutIfNoEnvVarialbeSet()
        {
            var beforeTestRunSTartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunSTartPayload);

            requestHandler.ProcessRequests();

            mockDataCollectionTestCaseEventHandler.Verify(h => h.WaitForRequestHandlerConnection(EnvironmentHelper.DefaultConnectionTimeout * 1000));
        }

        [TestMethod]
        public void ProcessRequestsShouldSetTimeoutBasedOnEnvVariable()
        {
            var timeout = 10;
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, timeout.ToString());
            var beforeTestRunSTartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunSTartPayload);

            requestHandler.ProcessRequests();

            mockDataCollectionTestCaseEventHandler.Verify(h => h.WaitForRequestHandlerConnection(timeout * 1000));
        }

        [TestMethod]
        public void ProcessRequestsShouldNotInitializeTestCaseEventHandlerIfTestCaseLevelEventsAreNotEnabled()
        {
            mockDataCollectionManager.Setup(x => x.SessionStarted(It.IsAny<SessionStartEventArgs>())).Returns(false);
            var beforeTestRunSTartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunSTartPayload);

            requestHandler.ProcessRequests();

            mockDataCollectionTestCaseEventHandler.Verify(x => x.InitializeCommunication(), Times.Never);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.ProcessRequests(), Times.Never);
            mockDataCollectionTestCaseEventHandler.Verify(x => x.WaitForRequestHandlerConnection(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void ProcessRequestsShouldReceiveCorrectPayloadInBeforeTestRunStart()
        {
            var beforeTestRunStartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunStartPayload);
            var message = new Message() { MessageType = MessageType.BeforeTestRunStart, Payload = JToken.FromObject(beforeTestRunStartPayload) };
            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(afterTestRunEnd);
            requestHandler.ProcessRequests();

            mockDataSerializer.Verify(x => x.DeserializePayload<BeforeTestRunStartPayload>(message), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestShouldInitializeDataCollectorsWithCorrectSettings()
        {
            var beforeTestRunStartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunStartPayload);
            var message = new Message() { MessageType = MessageType.BeforeTestRunStart, Payload = JToken.FromObject(beforeTestRunStartPayload) };
            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(afterTestRunEnd);
            requestHandler.ProcessRequests();

            mockDataCollectionManager.Verify(x => x.InitializeDataCollectors("settingsxml"), Times.Once);
        }

        [TestMethod]
        public void ProcessRequestShouldCallSessionStartWithCorrectTestSources()
        {
            var beforeTestRunStartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll", "test2.dll" } };
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunStartPayload);
            var message = new Message() { MessageType = MessageType.BeforeTestRunStart, Payload = JToken.FromObject(beforeTestRunStartPayload) };
            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(afterTestRunEnd);
            requestHandler.ProcessRequests();

            mockDataCollectionManager.Verify(x => x.SessionStarted(It.Is<SessionStartEventArgs>(
                y => y.GetPropertyValue<IEnumerable<string>>("TestSources").Contains("test1.dll") &&
                y.GetPropertyValue<IEnumerable<string>>("TestSources").Contains("test2.dll"))));
        }

        [TestMethod]
        public void ProcessRequestShouldEnableTelemetry()
        {
            var beforeTestRunStartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll", "test2.dll" }, IsTelemetryOptedIn = true };
            mockRequestData.Setup(r => r.IsTelemetryOptedIn).Returns(false);
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunStartPayload);
            var message = new Message() { MessageType = MessageType.BeforeTestRunStart, Payload = JToken.FromObject(beforeTestRunStartPayload) };
            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(afterTestRunEnd);
            requestHandler.ProcessRequests();

            mockRequestData.VerifySet(r => r.IsTelemetryOptedIn = true);
            mockRequestData.VerifySet(r => r.MetricsCollection = It.IsAny<MetricsCollection>());
        }

        [TestMethod]
        public void ProcessRequestShouldNotEnableTelemetryIfTelemetryEnabled()
        {
            var beforeTestRunStartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll", "test2.dll" }, IsTelemetryOptedIn = true };
            mockRequestData.Setup(r => r.IsTelemetryOptedIn).Returns(true);
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunStartPayload);
            var message = new Message() { MessageType = MessageType.BeforeTestRunStart, Payload = JToken.FromObject(beforeTestRunStartPayload) };
            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(afterTestRunEnd);
            requestHandler.ProcessRequests();

            mockRequestData.VerifySet(r => r.IsTelemetryOptedIn = It.IsAny<bool>(), Times.Never);
            mockRequestData.VerifySet(r => r.MetricsCollection = It.IsAny<IMetricsCollection>(), Times.Never);
        }

        [TestMethod]
        public void ProcessRequestShouldNotEnableTelemetryIfTelemetryEnablingNotRequested()
        {
            var beforeTestRunStartPayload = new BeforeTestRunStartPayload { SettingsXml = "settingsxml", Sources = new List<string> { "test1.dll", "test2.dll" }, IsTelemetryOptedIn = false };
            mockRequestData.Setup(r => r.IsTelemetryOptedIn).Returns(false);
            mockDataSerializer.Setup(x => x.DeserializePayload<BeforeTestRunStartPayload>(It.Is<Message>(y => y.MessageType == MessageType.BeforeTestRunStart)))
                                   .Returns(beforeTestRunStartPayload);
            var message = new Message() { MessageType = MessageType.BeforeTestRunStart, Payload = JToken.FromObject(beforeTestRunStartPayload) };
            mockCommunicationManager.SetupSequence(x => x.ReceiveMessage()).Returns(message).Returns(afterTestRunEnd);
            requestHandler.ProcessRequests();

            mockRequestData.VerifySet(r => r.IsTelemetryOptedIn = It.IsAny<bool>(), Times.Never);
            mockRequestData.VerifySet(r => r.MetricsCollection = It.IsAny<IMetricsCollection>(), Times.Never);
        }
    }
}