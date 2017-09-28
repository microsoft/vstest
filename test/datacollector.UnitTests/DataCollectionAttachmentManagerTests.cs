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
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionAttachmentManagerTests
    {
        private const int Timeout = 10 * 60 * 1000;
        private DataCollectionAttachmentManager attachmentManager;
        private Mock<IMessageSink> messageSink;
        private SessionId sessionId;
        private static readonly string TempDirectoryPath = Path.GetTempPath();

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
            File.Delete(Path.Combine(TempDirectoryPath, "filename.txt"));
            File.Delete(Path.Combine(TempDirectoryPath, "filename1.txt"));
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
            this.attachmentManager.Initialize(this.sessionId, TempDirectoryPath, this.messageSink.Object);

            Assert.AreEqual(Path.Combine(TempDirectoryPath, this.sessionId.Id.ToString()), this.attachmentManager.SessionOutputDirectory);
        }

        [TestMethod]
        public void AddAttachmentShouldNotAddNewFileTransferIfSessionIsNotConfigured()
        {
            var filename = "filename.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), false);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage, null, uri, friendlyName);

            Assert.AreEqual(this.attachmentManager.AttachmentSets.Count, 0);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndCopyFileToOutputDirectoryIfDeleteFileIsFalse()
        {
            var filename = "filename.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);


            this.attachmentManager.Initialize(this.sessionId, TempDirectoryPath, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), false);


            this.attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            Assert.IsTrue(File.Exists(Path.Combine(TempDirectoryPath, filename)));
            Assert.IsTrue(File.Exists(Path.Combine(TempDirectoryPath, this.sessionId.Id.ToString(), filename)));
            Assert.AreEqual(1, this.attachmentManager.AttachmentSets[datacollectioncontext][uri].Attachments.Count);
        }

        [TestMethod]
        public void AddAttachmentsShouldAddFilesCorrespondingToDifferentDataCollectors()
        {
            var filename = "filename.txt";
            var filename1 = "filename1.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename1), string.Empty);

            this.attachmentManager.Initialize(this.sessionId, TempDirectoryPath, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");
            var uri1 = new Uri("datacollector://Company/Product/Version1");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), false);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            waitHandle.Reset();
            dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename1), false);
            this.attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri1, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            Assert.AreEqual(1, this.attachmentManager.AttachmentSets[datacollectioncontext][uri].Attachments.Count);
            Assert.AreEqual(1, this.attachmentManager.AttachmentSets[datacollectioncontext][uri1].Attachments.Count);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndMoveFileToOutputDirectoryIfDeleteFileIsTrue()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);


            this.attachmentManager.Initialize(this.sessionId, TempDirectoryPath, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), true);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            Assert.AreEqual(1, this.attachmentManager.AttachmentSets[datacollectioncontext][uri].Attachments.Count);
            Assert.IsTrue(File.Exists(Path.Combine(TempDirectoryPath, this.sessionId.Id.ToString(), filename)));
            Assert.IsFalse(File.Exists(Path.Combine(TempDirectoryPath, filename)));
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
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);

            this.attachmentManager.Initialize(this.sessionId, TempDirectoryPath, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), true);

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
            this.attachmentManager.Initialize(this.sessionId, TempDirectoryPath, this.messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);

            var result = this.attachmentManager.GetAttachments(datacollectioncontext);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetAttachmentsShouldNotReturnAttachmentsAfterCancelled()
        {
            var fileHelper = new Mock<IFileHelper>();
            var testableAttachmentManager = new TestableDataCollectionAttachmentManager(fileHelper.Object);
            var attachmentPath = Path.Combine(TempDirectoryPath, "filename.txt");
            File.WriteAllText(attachmentPath, string.Empty);
            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, attachmentPath, true);
            var waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { Assert.Fail("Handler shouldn't be called since operation is canceled."); });

            // We cancel the operation in the actual operation. This ensures the follow up task to is never called, attachments
            // are not added.
            Action cancelAddAttachment = () => testableAttachmentManager.Cancel();
            fileHelper.Setup(fh => fh.MoveFile(It.IsAny<string>(), It.IsAny<string>())).Callback(cancelAddAttachment);
            testableAttachmentManager.Initialize(this.sessionId, TempDirectoryPath, this.messageSink.Object);
            testableAttachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for the attachment transfer tasks to complete
            var result = testableAttachmentManager.GetAttachments(datacollectioncontext);
            Assert.AreEqual(0, result[0].Attachments.Count);
        }

        private class TestableDataCollectionAttachmentManager : DataCollectionAttachmentManager
        {
            public TestableDataCollectionAttachmentManager(IFileHelper fileHelper)
                : base(fileHelper)
            {
            }
        }
    }
}