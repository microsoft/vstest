// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestPlatformDataCollectionSinkTests
    {
        private Mock<IDataCollectionAttachmentManager> attachmentManager;

        private DataCollectorConfig dataCollectorConfig;

        private TestPlatformDataCollectionSink dataCollectionSink;

        private bool isEventHandlerInvoked = false;

        public TestPlatformDataCollectionSinkTests()
        {
            this.attachmentManager = new Mock<IDataCollectionAttachmentManager>();
            this.dataCollectorConfig = new DataCollectorConfig(typeof(CustomDataCollector));
            this.dataCollectionSink = new TestPlatformDataCollectionSink(this.attachmentManager.Object, this.dataCollectorConfig);
            this.isEventHandlerInvoked = false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(Path.Combine(AppContext.BaseDirectory, "filename.txt"));
        }

        [TestMethod]
        public void SendFileAsyncShouldThrowExceptionIfFileTransferInformationIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dataCollectionSink.SendFileAsync(default(FileTransferInformation));
            });
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfo()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            var fileTransferInfo = new FileTransferInformation(context, filename, false);

            this.dataCollectionSink.SendFileAsync(fileTransferInfo);

            this.attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformation>(), It.IsAny<AsyncCompletedEventHandler>(), It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeSendFileCompletedIfRegistered()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            var fileTransferInfo = new FileTransferInformation(context, filename, false);

            var attachmentManager = new DataCollectionAttachmentManager();
            attachmentManager.Initialize(sessionId, AppContext.BaseDirectory, new Mock<IMessageSink>().Object);

            this.dataCollectionSink = new TestPlatformDataCollectionSink(attachmentManager, this.dataCollectorConfig);
            this.dataCollectionSink.SendFileCompleted += SendFileCompleted_Handler;
            this.dataCollectionSink.SendFileAsync(fileTransferInfo);

            var result = attachmentManager.GetAttachments(context);

            Assert.IsNotNull(result);
            Assert.IsTrue(this.isEventHandlerInvoked);
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfoOverLoaded1()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            this.dataCollectionSink.SendFileAsync(context, filename, false);

            this.attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformation>(), It.IsAny<AsyncCompletedEventHandler>(), It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfoOverLoaded2()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            this.dataCollectionSink.SendFileAsync(context, filename, string.Empty, false);

            this.attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformation>(), It.IsAny<AsyncCompletedEventHandler>(), It.IsAny<Uri>(), It.IsAny<string>()), Times.Once());
        }

        void SendFileCompleted_Handler(object sender, AsyncCompletedEventArgs e)
        {
            this.isEventHandlerInvoked = true;
        }
    }
}
