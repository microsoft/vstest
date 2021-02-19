namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        public async Task HandleDataCollectionAttachmentSetsShouldReturnEmptySetWhenNoAttachmentsOrAttachmentsAreNull()
        {
            Collection<AttachmentSet> attachment = new Collection<AttachmentSet>();
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            resultAttachmentSets = await coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(null, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldReturnInputIfOnly1Attachment()
        {
            var attachmentSet = new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa.coverage"), "coverage"));

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> { attachmentSet };
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 1);
            Assert.IsTrue(resultAttachmentSets.First().Attachments.Count == 1);
            Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", resultAttachmentSets.First().Uri.AbsoluteUri);
            Assert.AreEqual("file:///C:/temp/aa.coverage", resultAttachmentSets.First().Attachments.First().Uri.AbsoluteUri);
        }

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldReturnInputIfOnly1LogsAttachment()
        {
            var attachmentSet = new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa.logs"), "coverage"));

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> { attachmentSet };
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 1);
            Assert.IsTrue(resultAttachmentSets.First().Attachments.Count == 1);
            Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", resultAttachmentSets.First().Uri.AbsoluteUri);
            Assert.AreEqual("file:///C:/temp/aa.logs", resultAttachmentSets.First().Attachments.First().Uri.AbsoluteUri);
        }

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldReturnInputIfOnlySeveralLogsAttachmentAnd1Report()
        {
            var attachmentSet = new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa.coverage"), "coverage"));

            var attachmentSet1 = new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), string.Empty);
            attachmentSet1.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa.logs"), "coverage"));
            attachmentSet1.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\bb.logs"), "coverage"));

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> { attachmentSet, attachmentSet1 };
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 2);
            Assert.IsTrue(resultAttachmentSets.First().Attachments.Count == 1);
            Assert.IsTrue(resultAttachmentSets.Last().Attachments.Count == 2);
        }

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldThrowIfCancellationRequested()
        {
            var attachmentSet = new AttachmentSet(new Uri("//badrui//"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa.coverage"), "coverage"));
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> 
            {
                attachmentSet,
                attachmentSet
            };

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(attachment, mockProgressReporter.Object, null, cts.Token));

            Assert.AreEqual(2, attachment.Count);

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }
    }
}
