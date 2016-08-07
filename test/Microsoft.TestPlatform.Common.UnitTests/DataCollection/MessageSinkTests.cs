// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MessageSinkTests
    {
        private MessageSink messageSink;
        private MockDataCollectionFileManager mockDataCollectionFileManager;
        private DataCollectionMessageEventArgs args;
        bool IsMessageSink_OnDataCollectionMessageInvoked;
        private DataCollectorDataMessage dataCollectorDataMessage;
        private FileDataHeaderMessage fileDataHeaderMessage;

        [TestInitialize]
        public void Init()
        {
            this.args = new DataCollectionMessageEventArgs(TestMessageLevel.Informational, "Message");
            this.mockDataCollectionFileManager = new MockDataCollectionFileManager();
            this.messageSink = new MessageSink(this.mockDataCollectionFileManager);

            var dataCollectionContext = new DataCollectionContext(new SessionId(Guid.NewGuid()));
            this.dataCollectorDataMessage = new DataCollectorDataMessage(dataCollectionContext, new Uri("File://Message"), "FileMessage");

            this.fileDataHeaderMessage = new FileDataHeaderMessage(dataCollectionContext, "filename", "description", false, new object(), null, new Uri("File://Message"), "FileMessage");
        }

        [TestMethod]
        public void SendMessageShouldInvokeOnDataCollectionMessageEventHandlerIfRegistered()
        {
            this.messageSink.SendMessage(this.args);
            Assert.IsFalse(this.IsMessageSink_OnDataCollectionMessageInvoked);

            // Register event handler.
            this.messageSink.OnDataCollectionMessage += new EventHandler<DataCollectionMessageEventArgs>(MessageSink_OnDataCollectionMessage);

            this.messageSink.SendMessage(this.args);
            Assert.IsTrue(this.IsMessageSink_OnDataCollectionMessageInvoked);
        }

        [TestMethod]
        public void SendMessageShouldThrowExceptionIfDataCollectorDataMessageIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.messageSink.SendMessage(default(DataCollectorDataMessage));
            });
        }

        [TestMethod]
        public void SendMessageShouldInvokeFileManagerDispatchMessage()
        {
            this.messageSink.SendMessage(this.fileDataHeaderMessage);

            Assert.IsTrue(this.mockDataCollectionFileManager.IsDispatchMessageInvoked);
            Assert.AreEqual(this.fileDataHeaderMessage, this.mockDataCollectionFileManager.DataCollectorDataMessage);
        }

        [TestMethod]
        public void SendMessageShouldInvokeInvalidOperationExceptionIfArgumentIfNotOfTypeFileDataHeaderMessage()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                this.messageSink.SendMessage(this.dataCollectorDataMessage);
            });
        }

        [TestMethod]
        public void SendMessageShouldThrowExceptionIfExcectpionIsThrownByDataCollectionFileManager()
        {
            this.mockDataCollectionFileManager.DispatchMessageThrowException = true;

            Assert.ThrowsException<Exception>(() =>
            {
                this.messageSink.SendMessage(this.fileDataHeaderMessage);
            });

            Assert.IsTrue(this.mockDataCollectionFileManager.IsDispatchMessageInvoked);
            Assert.AreEqual(this.fileDataHeaderMessage, this.mockDataCollectionFileManager.DataCollectorDataMessage);
        }


        private void MessageSink_OnDataCollectionMessage(object sender, DataCollectionMessageEventArgs args)
        {
            this.IsMessageSink_OnDataCollectionMessageInvoked = true;
            Assert.AreEqual(args, this.args);
        }
    }
}
