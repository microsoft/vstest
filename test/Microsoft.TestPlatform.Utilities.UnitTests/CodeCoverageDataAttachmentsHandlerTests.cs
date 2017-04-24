using System.Collections.Generic;

namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.ObjectModel;

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
                coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            resultAttachmentSets = coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(null);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);
        }

        [TestMethod]
        public void HandleDataCollectionAttachmentSetsShouldReturnEmptySetWhenNoCodeCoverageAttachments()
        {
            Collection<AttachmentSet> attachment = new Collection<AttachmentSet>();
            var attachmentSet = new AttachmentSet(new Uri("//badrui//"), string.Empty);

            ICollection<AttachmentSet> resultAttachmentSets =
                coverageDataAttachmentsHandler.HandleDataCollectionAttachmentSets(attachment);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);
        }
    }
}
