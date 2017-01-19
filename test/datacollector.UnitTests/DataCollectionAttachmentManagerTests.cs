// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionFileManagerTests
    {
        DataCollectionAttachmentManager attachmentManager;
        Mock<IDataCollectionLog> dataCollectionLog;
        SessionId sessionId;


        [TestInitialize]
        public void Init()
        {
            this.attachmentManager = new DataCollectionAttachmentManager();
            this.dataCollectionLog = new Mock<IDataCollectionLog>();
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
            Assert.ThrowsException<ArgumentNullException>((Action)(() =>
            {
                this.attachmentManager.Initialize((SessionId)null, string.Empty, this.dataCollectionLog.Object);
            }));
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfDataCollectionLogIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>((Action)(() =>
            {
                this.attachmentManager.Initialize(this.sessionId, string.Empty, null);
            }));
        }

        [TestMethod]
        public void InitializeShouldSetDefaultPathIfOutputDirectoryPathIsNull()
        {
            this.attachmentManager.Initialize(this.sessionId, string.Empty, this.dataCollectionLog.Object);

            Assert.AreEqual(this.attachmentManager.SessionOutputDirectory, Path.Combine(Path.GetTempPath(), "TestResults", this.sessionId.Id.ToString()));
        }

        [TestMethod]
        public void InitializeShouldSetCorrectGuidAndOutputPath()
        {
            this.attachmentManager.Initialize(this.sessionId, System.AppContext.BaseDirectory, this.dataCollectionLog.Object);

            Assert.IsNotNull(this.attachmentManager.AttachmentRequests);
            Assert.AreEqual(0, this.attachmentManager.AttachmentRequests.Count);
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

            var dataCollectorDataMessage = new FileTransferInformationExtension(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), "description", false, new object(), null, uri, friendlyName);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage);

            Assert.AreEqual(this.attachmentManager.AttachmentRequests.Count, 0);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndCopyFileToOutputDirectoryIfDeleteFileIsFalse()
        {
            var filename = "filename.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);


            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, dataCollectionLog.Object);

            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            var dataCollectorDataMessage = new FileTransferInformationExtension(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), "description", false, new object(), handler, uri, friendlyName);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage);

            // Wait for file operations to complete
            waitHandle.WaitOne();

            Assert.IsTrue(File.Exists(Path.Combine(System.AppContext.BaseDirectory, filename)));
            Assert.AreEqual(this.attachmentManager.AttachmentRequests.Count, 1);
        }

        [TestMethod]
        public void AddAttachmentShouldAddNewFileTransferAndMoveFileToOutputDirectoryIfDeleteFileIsTrue()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);


            this.attachmentManager.Initialize(this.sessionId, System.AppContext.BaseDirectory, this.dataCollectionLog.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            AsyncCompletedEventHandler handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            var dataCollectorDataMessage = new FileTransferInformationExtension(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), "description", true, new object(), handler, uri, friendlyName);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage);

            // Wait for file operations to complete
            waitHandle.WaitOne();

            Assert.AreEqual(this.attachmentManager.AttachmentRequests.Count, 1);
            Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, this.sessionId.Id.ToString(), filename)));
            Assert.IsFalse(File.Exists(filename));

        }

        [TestMethod]
        public void AddAttachmentShouldNotAddNewFileTransferIfNullIsPassed()
        {
            this.attachmentManager.AddAttachment(null);

            Assert.AreEqual(this.attachmentManager.AttachmentRequests.Count, 0);
        }

        [TestMethod]
        public void GetAttachmentsShouldReturnAllAttachmets()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);

            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, this.dataCollectionLog.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformationExtension(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), "description", true, new object(), null, uri, friendlyName);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage);

            Assert.AreEqual(1, this.attachmentManager.AttachmentRequests.Count);
            var result = this.attachmentManager.GetAttachments(datacollectioncontext);

            Assert.AreEqual(1, this.attachmentManager.AttachmentRequests.Count);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(friendlyName, result[0].DisplayName);
            Assert.AreEqual(uri, result[0].Uri);
            Assert.AreEqual(1, result[0].Attachments.Count);
        }

        [TestMethod]
        public void GetAttachmentsShouldNotReutrnAnyDataWhenActiveFileTransferAreNotPresent()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);

            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, this.dataCollectionLog.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);

            var result = this.attachmentManager.GetAttachments(datacollectioncontext);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DisposeShouldDisposeAllResources()
        {
            var filename = "filename1.txt";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, filename), string.Empty);

            this.attachmentManager.Initialize(this.sessionId, AppContext.BaseDirectory, this.dataCollectionLog.Object);

            var datacollectioncontext = new DataCollectionContext(this.sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            var dataCollectorDataMessage = new FileTransferInformationExtension(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, filename), "description", true, new object(), null, uri, friendlyName);

            this.attachmentManager.AddAttachment(dataCollectorDataMessage);

            Assert.AreEqual(1, this.attachmentManager.AttachmentRequests.Count);

            this.attachmentManager.Dispose();
            Assert.AreEqual(0, this.attachmentManager.AttachmentRequests.Count);
            Assert.IsNull(this.attachmentManager.SessionOutputDirectory);
        }
    }
}
