// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AttachmentRequestTests
    {
        private FileTransferInformationExtension fileTransferInfo;
        private string filename = "abc.txt";

        [TestInitialize]
        public void Init()
        {
            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            var datacollectioncontext = new DataCollectionContext(sessionId);
            var friendlyName = "TestDataCollector";
            var uri = new Uri("datacollector://Company/Product/Version");

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, this.filename), string.Empty);

            this.fileTransferInfo = new FileTransferInformationExtension(datacollectioncontext, Path.Combine(AppContext.BaseDirectory, this.filename), "description", false, new object(), null, uri, friendlyName);
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(Path.Combine(AppContext.BaseDirectory, this.filename));
        }

        [TestMethod]
        public void AttachmentRequestShouldInitializeLocalFileNameAndPath()
        {
            var attachmentRequest = new AttachmentRequest(AppContext.BaseDirectory, this.fileTransferInfo);

            Assert.AreEqual(this.filename, attachmentRequest.LocalFileName);
            Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, this.filename), attachmentRequest.LocalFilePath);
        }

        [TestMethod]
        public void AttachmentRequestShouldWaitforCopyComplete()
        {
            var attachmentRequest = new AttachmentRequest(AppContext.BaseDirectory, this.fileTransferInfo);

            var thread = new Thread(attachmentRequest.WaitForCopyComplete);
            thread.Start();

            var finished = thread.Join(500);

            Assert.IsFalse(finished);

            attachmentRequest.CompleteRequest(null);

            thread = new Thread(attachmentRequest.WaitForCopyComplete);
            thread.Start();
            finished = thread.Join(500);

            Assert.IsTrue(finished);
        }
    }
}
