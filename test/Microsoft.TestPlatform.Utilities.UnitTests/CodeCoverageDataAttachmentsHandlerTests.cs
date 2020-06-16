using System.Collections.Generic;

namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.ObjectModel;
    using System.Threading;

    [TestClass]
    public class CodeCoverageDataAttachmentsHandlerTests
    {
        private CodeCoverageDataAttachmentsHandler coverageDataAttachmentsHandler;

        public CodeCoverageDataAttachmentsHandlerTests()
        {
            coverageDataAttachmentsHandler = new CodeCoverageDataAttachmentsHandler();
        }

        [TestMethod]
        public void HandleDataCollectionAttachmentSetsShouldReturnEmptySetWhenNoAttachmentsOrAttachmentsAreNull()
        {
            Collection<AttachmentSet> attachment = new Collection<AttachmentSet>();
            ICollection<AttachmentSet> resultAttachmentSets =
                coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            resultAttachmentSets = coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);
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

            Assert.ThrowsException<OperationCanceledException>(() => coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, cts.Token));

            Assert.AreEqual(1, attachment.Count);
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

            var result = coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment, cts.Token);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(attachmentSet1));
            Assert.IsTrue(result.Contains(attachmentSet2));
        }
    }
}
