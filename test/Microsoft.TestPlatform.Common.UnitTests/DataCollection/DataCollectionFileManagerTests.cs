// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.DataCollection
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionFileManagerTests
    {
        private Mock<IDataCollectionLog> mockDataCollectionLog;
        private DataCollectionFileManager dataCollectionFileManager;
        private SessionId sessionId;
        private Guid guid;
        private DataCollectionContext dataCollectionContext;
        private string fileName;
        private FileStream fileStream;
        private DataCollectorDataMessage dataCollectorDataMessage;
        private string friendlyName = "TestDataCollector";
        private Uri uri = new Uri("datacollector://Company/Product/Version");
        private EventWaitHandle waitHandle;

        [TestInitialize]
        public void Init()
        {
            this.mockDataCollectionLog = new Mock<IDataCollectionLog>();
            this.dataCollectionFileManager = new DataCollectionFileManager(this.mockDataCollectionLog.Object);
            this.guid = Guid.NewGuid();
            this.sessionId = new SessionId(guid);
            this.dataCollectionContext = new DataCollectionContext(this.sessionId);
            this.fileName = "filename1.txt";

            this.waitHandle = new AutoResetEvent(false);
            var handler = new AsyncCompletedEventHandler((a, e) => { waitHandle.Set(); });
            this.dataCollectorDataMessage = new FileDataHeaderMessage(this.dataCollectionContext, this.fileName, "description", false, new object(), handler, this.uri, this.friendlyName);
            this.fileStream = File.Create(this.fileName);
            this.fileStream.Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(this.fileStream.Name);
            this.waitHandle.Reset();
        }

        [TestMethod]
        public void ConfigureSessionShouldThrowExceptionIfNullIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dataCollectionFileManager.ConfigureSession(null, string.Empty);
            });
        }

        [TestMethod]
        public void ConfigureSessionShouldSetDefaultPathIfOutputDirectoryPathIsNull()
        {
            this.dataCollectionFileManager.ConfigureSession(this.sessionId, string.Empty);

            Assert.AreEqual(this.dataCollectionFileManager.SessionInfo.First().Value.OutputDirectory, Path.Combine(Path.GetTempPath(), "TestPlatformResults", this.guid.ToString()));
        }

        [TestMethod]
        public void ConfigureSessionShouldSetCorrectGuidAndOutputPath()
        {
            this.dataCollectionFileManager.ConfigureSession(this.sessionId, Directory.GetCurrentDirectory());

            Assert.IsNotNull(this.dataCollectionFileManager.SessionInfo);
            Assert.AreEqual(1, this.dataCollectionFileManager.SessionInfo.Count);
            Assert.AreEqual(this.sessionId, this.dataCollectionFileManager.SessionInfo.Keys.First());
            Assert.AreEqual(this.dataCollectionFileManager.SessionInfo.First().Value.SessionId, this.sessionId);
            Assert.AreEqual(Path.Combine(Directory.GetCurrentDirectory(), this.guid.ToString()), this.dataCollectionFileManager.SessionInfo.First().Value.OutputDirectory);
        }

        [TestMethod]
        public void DispatchMessageShouldNotAddNewFileTransferIfSessionIsNotConfigured()
        {
            this.dataCollectionFileManager.DispatchMessage(this.dataCollectorDataMessage);

            Assert.AreEqual(this.dataCollectionFileManager.CopyRequestDataDictionary.Count(), 0);
        }

        [TestMethod]
        public void DispatchMessageShouldAddNewFileTransferAndCopyFileToOutputDirectoryIfDeleteFileIsFalse()
        {
            this.dataCollectionFileManager.ConfigureSession(this.sessionId, Directory.GetCurrentDirectory());

            this.dataCollectionFileManager.DispatchMessage(this.dataCollectorDataMessage);

            // Wait for file operations to complete
            this.waitHandle.WaitOne();

            Assert.IsTrue(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), this.sessionId.Id.ToString(), this.fileName)));
            Assert.IsTrue(File.Exists(this.fileName));
            Assert.AreEqual(this.dataCollectionFileManager.CopyRequestDataDictionary.Count, 1);
        }

        [TestMethod]
        public void DispatchMessageShouldAddNewFileTransferAndMoveFileToOutputDirectoryIfDeleteFileIsTrue()
        {
            this.dataCollectionFileManager.ConfigureSession(this.sessionId, Directory.GetCurrentDirectory());
            AsyncCompletedEventHandler handler = new AsyncCompletedEventHandler((a, e) => { this.waitHandle.Set(); });
            this.dataCollectorDataMessage = new FileDataHeaderMessage(this.dataCollectionContext, this.fileName, "description", true, new object(), handler, uri, friendlyName);

            this.dataCollectionFileManager.DispatchMessage(dataCollectorDataMessage);

            // Wait for file operations to complete
            waitHandle.WaitOne();

            Assert.AreEqual(this.dataCollectionFileManager.CopyRequestDataDictionary.Count, 1);
            Assert.IsTrue(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), guid.ToString(), this.fileName)));
            Assert.IsFalse(File.Exists(this.fileName));

        }

        [TestMethod]
        public void DispatchMessageShouldNotAddNewFileTransferIfNullIsPassed()
        {
            this.dataCollectionFileManager.DispatchMessage(null);

            Assert.AreEqual(this.dataCollectionFileManager.CopyRequestDataDictionary.Count, 0);
        }

        [TestMethod]
        public void GetDataShouldReturnAllActiveFileTransferData()
        {
            this.dataCollectionFileManager.ConfigureSession(sessionId, Directory.GetCurrentDirectory());

            this.dataCollectionFileManager.DispatchMessage(this.dataCollectorDataMessage);

            Assert.AreEqual(1, this.dataCollectionFileManager.CopyRequestDataDictionary.Count);
            var result = this.dataCollectionFileManager.GetData(this.dataCollectionContext);

            Assert.AreEqual(0, this.dataCollectionFileManager.CopyRequestDataDictionary.Count);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(friendlyName, result[0].DisplayName);
            Assert.AreEqual(uri, result[0].Uri);
            Assert.AreEqual(1, result[0].Attachments.Count);
        }

        [TestMethod]
        public void GetDataShouldNotReutrnAnyDataWhenActiveFileTransferAreNotPresent()
        {
            this.dataCollectionFileManager.ConfigureSession(sessionId, Directory.GetCurrentDirectory());

            var result = this.dataCollectionFileManager.GetData(this.dataCollectionContext);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void CloseSessionShouldThrowExcetionIfSessionIdIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                var dataCollectionLog = new Mock<IDataCollectionLog>();
                DataCollectionFileManager dcFileManager = new DataCollectionFileManager(dataCollectionLog.Object);
                dcFileManager.CloseSession(null);
            });
        }

        [TestMethod]
        public void CloseSessionShouldCloseSessionForGivenSessionId()
        {
            this.dataCollectionFileManager.ConfigureSession(sessionId, Directory.GetCurrentDirectory());

            this.dataCollectionFileManager.DispatchMessage(this.dataCollectorDataMessage);

            var count = this.dataCollectionFileManager.CopyRequestDataDictionary.Count;

            this.dataCollectionFileManager.CloseSession(this.sessionId);
            Assert.AreEqual(count - 1, this.dataCollectionFileManager.CopyRequestDataDictionary.Count);
        }

        [TestMethod]
        public void CloseSessionShouldNotCloseOtherSessions()
        {
            this.dataCollectionFileManager.ConfigureSession(sessionId, Directory.GetCurrentDirectory());

            this.dataCollectionFileManager.DispatchMessage(this.dataCollectorDataMessage);

            SessionId sessionId1 = new SessionId(Guid.NewGuid());
            this.dataCollectionFileManager.CloseSession(sessionId1);
            Assert.AreEqual(1, this.dataCollectionFileManager.CopyRequestDataDictionary.Count);
        }
    }
}
