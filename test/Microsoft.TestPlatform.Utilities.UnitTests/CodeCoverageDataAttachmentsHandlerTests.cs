namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;

    [TestClass]
    public class CodeCoverageDataAttachmentsHandlerTests
    {
        private readonly Mock<IProgress<int>> mockProgressReporter;
        private readonly CodeCoverageDataAttachmentsHandler coverageDataAttachmentsHandler;

        public CodeCoverageDataAttachmentsHandlerTests()
        {
            mockProgressReporter = new Mock<IProgress<int>>();
            coverageDataAttachmentsHandler = new CodeCoverageDataAttachmentsHandler();
        }

        [TestMethod]
        public void HandleDataCollectionAttachmentSetsShouldReturnEmptySetWhenNoAttachmentsOrAttachmentsAreNull()
        {
            Collection<AttachmentSet> attachment = new Collection<AttachmentSet>();
            ICollection<AttachmentSet> resultAttachmentSets =
                coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, mockProgressReporter.Object, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            resultAttachmentSets = coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(null, mockProgressReporter.Object, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void HandleDataCollectionAttachmentSetsShouldThrowIfCancellationRequested()
        {
            var attachmentSet = new AttachmentSet(new Uri("//badrui//"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa"), "coverage"));
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> 
            {
                attachmentSet
            };

            Assert.ThrowsException<OperationCanceledException>(() => coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, mockProgressReporter.Object, cts.Token));

            Assert.AreEqual(1, attachment.Count);

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void HandleDataCollectionAttachmentSetsShouldReturnExistingAttachmentsIfFailedToLoadLibrary()
        {
            var attachmentSet1 = new AttachmentSet(new Uri("//badrui//"), string.Empty);
            attachmentSet1.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa"), "coverage"));

            var attachmentSet2 = new AttachmentSet(new Uri("//badruj//"), string.Empty);
            attachmentSet2.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\ab"), "coverage"));

            CancellationTokenSource cts = new CancellationTokenSource();

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet>
            {
                attachmentSet1,
                attachmentSet2
            };

            var result = coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, mockProgressReporter.Object, cts.Token);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(attachmentSet1));
            Assert.IsTrue(result.Contains(attachmentSet2));

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }
    }
}
