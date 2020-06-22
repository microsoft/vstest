namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
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
                coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            resultAttachmentSets = coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(null, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void HandleDataCollectionAttachmentSetsShouldReturnInputIfOnly1Attachment()
        {
            var attachmentSet = new AttachmentSet(new Uri("//badrui//"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa"), "coverage"));

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> { attachmentSet };
            ICollection<AttachmentSet> resultAttachmentSets =
                coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 1);
            Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", resultAttachmentSets.First().Uri.AbsoluteUri);
            Assert.AreEqual("file:///C:/temp/aa", resultAttachmentSets.First().Attachments.First().Uri.AbsoluteUri);
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
                attachmentSet,
                attachmentSet
            };

            Assert.ThrowsException<OperationCanceledException>(() => coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, mockProgressReporter.Object, null, cts.Token));

            Assert.AreEqual(2, attachment.Count);

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

            var result = coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, mockProgressReporter.Object, null, cts.Token);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(attachmentSet1));
            Assert.IsTrue(result.Contains(attachmentSet2));

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }
    }
}
