// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Microsoft.VisualStudio.TestPlatform.DataCollector;
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using System.IO;
    using System.ComponentModel;

    [TestClass]
    public class TestPlatformDataCollectionSinkTests
    {
        private Mock<IDataCollectionAttachmentManager> attachmentManager;

        private DataCollectorConfig dataCollectorConfig;

        private TestPlatformDataCollectionSink dataCollectionSink;

        private bool isEventHandlerInvoked = false;

        [TestInitialize]
        public void Init()
        {
            this.attachmentManager = new Mock<IDataCollectionAttachmentManager>();
            this.dataCollectorConfig = new DataCollectorConfig(typeof(CustomDataCollector), string.Empty);
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

            Guid guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            FileTransferInformation fileTransferInfo = new FileTransferInformation(context, filename, false);

            this.dataCollectionSink.SendFileAsync(fileTransferInfo);

            this.attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformationExtension>()), Times.Once());
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeSendFileCompletedIfRegistered()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            Guid guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            FileTransferInformation fileTransferInfo = new FileTransferInformation(context, filename, false);

            var attachmentManager = new DataCollectionAttachmentManager();
            attachmentManager.Initialize(sessionId, AppContext.BaseDirectory, new Mock<IDataCollectionLog>().Object);

            this.dataCollectionSink = new TestPlatformDataCollectionSink(attachmentManager, this.dataCollectorConfig);
            this.dataCollectionSink.SendFileCompleted += SendFileCompleted_Handler;
            this.dataCollectionSink.SendFileAsync(fileTransferInfo);

            var result = attachmentManager.GetAttachments(context);

            Assert.IsNotNull(result);
            Assert.IsTrue(isEventHandlerInvoked);
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfoOverLoaded1()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            Guid guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            this.dataCollectionSink.SendFileAsync(context, filename, false);

            this.attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformationExtension>()), Times.Once());
        }

        [TestMethod]
        public void SendFileAsyncShouldInvokeAttachmentManagerWithValidFileTransferInfoOverLoaded2()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);

            Guid guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var context = new DataCollectionContext(sessionId);

            this.dataCollectionSink.SendFileAsync(context, filename, string.Empty, false);

            this.attachmentManager.Verify(x => x.AddAttachment(It.IsAny<FileTransferInformationExtension>()), Times.Once());
        }

        void SendFileCompleted_Handler(object sender, AsyncCompletedEventArgs e)
        {
            this.isEventHandlerInvoked = true;
        }


    }
}
