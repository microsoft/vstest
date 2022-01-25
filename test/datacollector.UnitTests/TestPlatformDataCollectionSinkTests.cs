// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestPlatformDataCollectionSinkTests
    {
        private readonly Mock<IDataCollectionAttachmentManager> attachmentManager;

        private readonly DataCollectorConfig dataCollectorConfig;

        private TestPlatformDataCollectionSink dataCollectionSink;

        private bool isEventHandlerInvoked = false;
        private static readonly string TempDirectoryPath = Path.GetTempPath();

        public TestPlatformDataCollectionSinkTests()
        {
            attachmentManager = new Mock<IDataCollectionAttachmentManager>();
            dataCollectorConfig = new DataCollectorConfig(typeof(CustomDataCollector));
            dataCollectionSink = new TestPlatformDataCollectionSink(attachmentManager.Object, dataCollectorConfig);
            isEventHandlerInvoked = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(Path.Combine(TempDirectoryPath, "filename.txt"));
        }

        [TestMethod]
        public void SendFileAsyncShouldThrowExceptionIfFileTransferInformationIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => dataCollectionSink.SendFileAsync(default));
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfo()
        {
            var filename = Path.Combine(TempDirectoryPath, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            var fileTransferInfo = new FileTransferInformation(context, filename, false);

            dataCollectionSink.SendFileAsync(fileTransferInfo);

            attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformation>(), It.IsAny<AsyncCompletedEventHandler>(), It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeSendFileCompletedIfRegistered()
        {
            var filename = Path.Combine(TempDirectoryPath, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            var fileTransferInfo = new FileTransferInformation(context, filename, false);

            var attachmentManager = new DataCollectionAttachmentManager();
            attachmentManager.Initialize(sessionId, TempDirectoryPath, new Mock<IMessageSink>().Object);

            dataCollectionSink = new TestPlatformDataCollectionSink(attachmentManager, dataCollectorConfig);
            dataCollectionSink.SendFileCompleted += SendFileCompleted_Handler;
            dataCollectionSink.SendFileAsync(fileTransferInfo);

            var result = attachmentManager.GetAttachments(context);

            Assert.IsNotNull(result);
            Assert.IsTrue(isEventHandlerInvoked);
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfoOverLoaded1()
        {
            var filename = Path.Combine(TempDirectoryPath, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            dataCollectionSink.SendFileAsync(context, filename, false);

            attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformation>(), It.IsAny<AsyncCompletedEventHandler>(), It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfoOverLoaded2()
        {
            var filename = Path.Combine(TempDirectoryPath, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            dataCollectionSink.SendFileAsync(context, filename, string.Empty, false);

            attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformation>(), It.IsAny<AsyncCompletedEventHandler>(), It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
        }

        void SendFileCompleted_Handler(object sender, AsyncCompletedEventArgs e)
        {
            isEventHandlerInvoked = true;
        }
    }
}