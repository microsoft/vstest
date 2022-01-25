﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionAttachmentManagerTests
    {
        private const int Timeout = 10 * 60 * 1000;
        private readonly DataCollectionAttachmentManager attachmentManager;
        private readonly Mock<IMessageSink> messageSink;
        private readonly SessionId sessionId;
        private static readonly string TempDirectoryPath = Path.GetTempPath();

        public DataCollectionAttachmentManagerTests()
        {
            attachmentManager = new DataCollectionAttachmentManager();
            messageSink = new Mock<IMessageSink>();
            var guid = Guid.NewGuid();
            sessionId = new SessionId(guid);
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
            Assert.ThrowsException<ArgumentNullException>(() => attachmentManager.Initialize((SessionId)null, string.Empty, messageSink.Object));
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfMessageSinkIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => attachmentManager.Initialize(sessionId, string.Empty, null));
        }

        [TestMethod]
        public void InitializeShouldSetDefaultPathIfOutputDirectoryPathIsNull()
        {
            attachmentManager.Initialize(sessionId, string.Empty, messageSink.Object);

            Assert.AreEqual(attachmentManager.SessionOutputDirectory, Path.Combine(Path.GetTempPath(), "TestResults", sessionId.Id.ToString()));
        }

        [TestMethod]
        public void InitializeShouldSetCorrectGuidAndOutputPath()
        {
            attachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);

            Assert.AreEqual(Path.Combine(TempDirectoryPath, sessionId.Id.ToString()), attachmentManager.SessionOutputDirectory);
        }

        [TestMethod]
        public void AddAttachmentShouldNotAddNewFileTransferIfSessionIsNotConfigured()
        {
            var filename = "filename.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);

            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), false);

            attachmentManager.AddAttachment(dataCollectorDataMessage, null, uri, friendlyName);

            Assert.AreEqual(0, attachmentManager.AttachmentSets.Count);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndCopyFileToOutputDirectoryIfDeleteFileIsFalse()
        {
            var filename = "filename.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);


            attachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => waitHandle.Set());
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), false);


            attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            Assert.IsTrue(File.Exists(Path.Combine(TempDirectoryPath, filename)));
            Assert.IsTrue(File.Exists(Path.Combine(TempDirectoryPath, sessionId.Id.ToString(), filename)));
            Assert.AreEqual(1, attachmentManager.AttachmentSets[datacollectioncontext][uri].Attachments.Count);
        }

        [TestMethod]
        public void AddAttachmentsShouldAddFilesCorrespondingToDifferentDataCollectors()
        {
            var filename = "filename.txt";
            var filename1 = "filename1.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename1), string.Empty);

            attachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");
            var uri1 = new Uri("datacollector://Company/Product/Version1");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => waitHandle.Set());
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), false);

            attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            waitHandle.Reset();
            dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename1), false);
            attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri1, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            Assert.AreEqual(1, attachmentManager.AttachmentSets[datacollectioncontext][uri].Attachments.Count);
            Assert.AreEqual(1, attachmentManager.AttachmentSets[datacollectioncontext][uri1].Attachments.Count);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndMoveFileToOutputDirectoryIfDeleteFileIsTrue()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);


            attachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => waitHandle.Set());
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), true);

            attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            Assert.AreEqual(1, attachmentManager.AttachmentSets[datacollectioncontext][uri].Attachments.Count);
            Assert.IsTrue(File.Exists(Path.Combine(TempDirectoryPath, sessionId.Id.ToString(), filename)));
            Assert.IsFalse(File.Exists(Path.Combine(TempDirectoryPath, filename)));
        }

        [TestMethod]
        public void AddAttachmentShouldAddMultipleAttachmentsForSameDC()
        {
            var filename = "filename.txt";
            var filename1 = "filename1.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename1), string.Empty);

            attachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => waitHandle.Set());
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), false);

            attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            waitHandle.Reset();
            dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename1), false);
            attachmentManager.AddAttachment(dataCollectorDataMessage, handler, uri, friendlyName);

            // Wait for file operations to complete
            waitHandle.WaitOne(Timeout);

            Assert.AreEqual(2, attachmentManager.AttachmentSets[datacollectioncontext][uri].Attachments.Count);
        }

        [TestMethod]
        public void AddAttachmentShouldNotAddNewFileTransferIfNullIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() => attachmentManager.AddAttachment(null, null, null, null));
        }

        [TestMethod]
        public void GetAttachmentsShouldReturnAllAttachmets()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(TempDirectoryPath, filename), string.Empty);

            attachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, Path.Combine(TempDirectoryPath, filename), true);

            attachmentManager.AddAttachment(dataCollectorDataMessage, null, uri, friendlyName);

            Assert.AreEqual(1, attachmentManager.AttachmentSets.Count);
            var result = attachmentManager.GetAttachments(datacollectioncontext);

            Assert.AreEqual(0, attachmentManager.AttachmentSets.Count);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(friendlyName, result[0].DisplayName);
            Assert.AreEqual(uri, result[0].Uri);
            Assert.AreEqual(1, result[0].Attachments.Count);
        }

        [TestMethod]
        public void GetAttachmentsShouldNotReutrnAnyDataWhenActiveFileTransferAreNotPresent()
        {
            attachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);

            var datacollectioncontext = new DataCollectionContext(sessionId);

            var result = attachmentManager.GetAttachments(datacollectioncontext);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetAttachmentsShouldNotReturnAttachmentsAfterCancelled()
        {
            var fileHelper = new Mock<IFileHelper>();
            var testableAttachmentManager = new TestableDataCollectionAttachmentManager(fileHelper.Object);
            var attachmentPath = Path.Combine(TempDirectoryPath, "filename.txt");
            File.WriteAllText(attachmentPath, string.Empty);
            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");
            var dataCollectorDataMessage = new FileTransferInformation(datacollectioncontext, attachmentPath, true);
            var waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => Assert.Fail("Handler shouldn't be called since operation is canceled."));

            // We cancel the operation in the actual operation. This ensures the follow up task to is never called, attachments
            // are not added.
            Action cancelAddAttachment = () => testableAttachmentManager.Cancel();
            fileHelper.Setup(fh => fh.MoveFile(It.IsAny<string>(), It.IsAny<string>())).Callback(cancelAddAttachment);
            testableAttachmentManager.Initialize(sessionId, TempDirectoryPath, messageSink.Object);
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