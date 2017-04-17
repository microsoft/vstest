// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionAttachmentManagerTests
    {
        private DataCollectionAttachmentManager attachmentManager;
        private Mock<IMessageSink> messageSink;
        private SessionId sessionId;

        public DataCollectionAttachmentManagerTests()
        {
            this.attachmentManager = new DataCollectionAttachmentManager();
            this.messageSink = new Mock<IMessageSink>();
            var guid = Guid.NewGuid();
            this.sessionId = new SessionId(guid);
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(Path.Combine(AppContext.BaseDirectory, "filename.txt"));
            File.Delete(Path.Combine(AppContext.BaseDirectory, "filename1.txt"));
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfSessionIdIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.attachmentManager.Initialize((SessionId)null, string.Empty, this.messageSink.Object);
            });
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfMessageSinkIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.attachmentManager.Initialize(this.sessionId, string.Empty, null);
            });
        }

        [TestMethod]
        public void InitializeShouldSetDefaultPathIfOutputDirectoryPathIsNull()
        {
            this.attachmentManager.Initialize(this.sessionId, string.Empty, this.messageSink.Object);

            Assert.AreEqual(this.attachmentManager.SessionOutputDirectory, Path.Combine(Path.GetTempPath(), "TestResults", this.sessionId.Id.ToString()));
        }

        [TestMethod]
        public void InitializeShouldSetCorrectGuidAndOutputPath()
        {
            this.attachmentManager.Initialize(this.sessionId, System.AppContext.BaseDirectory, this.messageSink.Object);

            Assert.AreEqual(Path.Combine(System.AppContext.BaseDirectory, this.sessionId.Id.ToString()), this.attachmentManager.SessionOutputDirectory);
        }

        [TestMethod]
        public void AddAttachmentShouldNotAddNewFileTransferIfSessionIsNotConfigured()
        {
            var filename = "filename.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), false);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage, null, uri, friendlyName);

            Assert.AreEqual(this.attachmentManager.AttachmentSets.Count, 0);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndCopyFileToOutputDirectoryIfDeleteFileIsFalse()
        {
            var filename = "filename.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);


            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), false);


            this.attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne();

            Assert.IsTrue(File.Exists(Path.Combine(System.AppContext.BaseDirectory, filename)));
            Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, this.sessionId.Id.ToString(), filename)));
            Assert.AreEqual(1, this.attachmentManager.AttachmentSets[datacollectioncontext][friendlyName].Attachments.Count);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndMoveFileToOutputDirectoryIfDeleteFileIsTrue()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);


            this.attachmentManager.Initialize(this.sessionId, System.AppContext.BaseDirectory, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), true);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne();

            Assert.AreEqual(1, this.attachmentManager.AttachmentSets[datacollectioncontext][friendlyName].Attachments.Count);
            Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, this.sessionId.Id.ToString(), filename)));
            Assert.IsFalse(File.Exists(Path.Combine(AppContext.BaseDirectory, filename)));
        }

        [TestMethod]
        public void AddAttachmentShouldNotAddNewFileTransferIfNullIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.attachmentManager.AddAttachment(null, null, null, null);
            });
        }

        [TestMethod]
        public void GetAttachmentsShouldReturnAllAttachmets()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);

            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), true);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage, null, uri, friendlyName);

            Assert.AreEqual(1, this.attachmentManager.AttachmentSets.Count);
            var result = this.attachmentManager.GetAttachments(datacollectioncontext);

            Assert.AreEqual(0, this.attachmentManager.AttachmentSets.Count);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(friendlyName, result[0].DisplayName);
            Assert.AreEqual(uri, result[0].Uri);
            Assert.AreEqual(1, result[0].Attachments.Count);
        }

        [TestMethod]
        public void GetAttachmentsShouldNotReutrnAnyDataWhenActiveFileTransferAreNotPresent()
        {
            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);

            var result = this.attachmentManager.GetAttachments(datacollectioncontext);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetAttachmentsShouldNotReturnAttachmentsAfterCancelled()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);

            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), true);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage, null, uri, friendlyName);

            this.attachmentManager.Cancel();

            var result = this.attachmentManager.GetAttachments(datacollectioncontext);

            Assert.AreEqual(0, result[0].Attachments.Count);
        }
    }
}
