namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class CodeCoverageDataAttachmentsHandlerTests
    {
        private readonly Mock<IProgress<int>> mockProgressReporter;
        private readonly XmlElement configurationElement;
        private readonly CodeCoverageDataAttachmentsHandler coverageDataAttachmentsHandler;
        private readonly string _filePrefix;

        public TestContext TestContext { get; set; }

        internal string TestFilesDirectory => Path.Combine(TestContext.DeploymentDirectory, "TestFiles");

        public CodeCoverageDataAttachmentsHandlerTests()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<configurationElement/>");
            configurationElement = doc.DocumentElement;
            mockProgressReporter = new Mock<IProgress<int>>();
            coverageDataAttachmentsHandler = new CodeCoverageDataAttachmentsHandler();
#if NETFRAMEWORK
            _filePrefix = "file:///";
#else
            _filePrefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "file:///" : "file://";
#endif
        }

#if NETFRAMEWORK
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Copying test files to correct place,
            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            var testFilesDirectory = Path.Combine(context.DeploymentDirectory, "TestFiles");
            Directory.CreateDirectory(testFilesDirectory);
            var files = Directory.GetFiles(Path.Combine(assemblyPath, "TestFiles"));
            foreach (var file in files)
                File.Copy(file, Path.Combine(testFilesDirectory, Path.GetFileName(file)));
        }
#endif

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldReturnEmptySetWhenNoAttachmentsOrAttachmentsAreNull()
        {
            Collection<AttachmentSet> attachment = new Collection<AttachmentSet>();
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 0);

            resultAttachmentSets = await coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, null, mockProgressReporter.Object, null, CancellationToken.None);

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
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 1);
            Assert.IsTrue(resultAttachmentSets.First().Attachments.Count == 1);
            Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", resultAttachmentSets.First().Uri.AbsoluteUri);
            Assert.AreEqual("file:///C:/temp/aa.coverage", resultAttachmentSets.First().Attachments.First().Uri.AbsoluteUri);
        }

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldReturnInputIf2DifferentFormatAttachments()
        {
            var file1Path = Path.Combine(TestFilesDirectory, "fullcovered.cobertura.xml");
            var file2Path = Path.Combine(Path.Combine(TestFilesDirectory, "fullcovered.coverage"));
            var attachmentSet = new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri(file1Path), "coverage"));
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri(file2Path), "coverage"));

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> { attachmentSet };
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 1);
            Assert.IsTrue(resultAttachmentSets.First().Attachments.Count == 2);
            Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", resultAttachmentSets.First().Uri.AbsoluteUri);
            Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", resultAttachmentSets.Last().Uri.AbsoluteUri);
            Assert.AreEqual(_filePrefix + file1Path.Replace("\\", "/").Replace(" ", "%20"), resultAttachmentSets.First().Attachments.First().Uri.AbsoluteUri);
            Assert.AreEqual(_filePrefix + file2Path.Replace("\\", "/").Replace(" ", "%20"), resultAttachmentSets.First().Attachments.Last().Uri.AbsoluteUri);
        }

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldReturnInputIf2SameFormatAttachments()
        {
            var file1Path = Path.Combine(TestFilesDirectory, "fullcovered.cobertura.xml");
            var attachmentSet = new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri(file1Path), "coverage"));
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri(file1Path), "coverage"));

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> { attachmentSet };
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, attachment, mockProgressReporter.Object, null, CancellationToken.None);

            Assert.IsNotNull(resultAttachmentSets);
            Assert.IsTrue(resultAttachmentSets.Count == 1);
            Assert.IsTrue(resultAttachmentSets.First().Attachments.Count == 1);
            Assert.AreEqual("datacollector://microsoft/CodeCoverage/2.0", resultAttachmentSets.First().Uri.AbsoluteUri);
            Assert.AreEqual(_filePrefix + file1Path.Replace("\\", "/").Replace(" ", "%20"), resultAttachmentSets.First().Attachments.First().Uri.AbsoluteUri);
        }

        [TestMethod]
        public async Task HandleDataCollectionAttachmentSetsShouldReturnInputIfOnly1LogsAttachment()
        {
            var attachmentSet = new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), string.Empty);
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri("C:\\temp\\aa.logs"), "coverage"));

            Collection<AttachmentSet> attachment = new Collection<AttachmentSet> { attachmentSet };
            ICollection<AttachmentSet> resultAttachmentSets = await
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, attachment, mockProgressReporter.Object, null, CancellationToken.None);

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
                coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, attachment, mockProgressReporter.Object, null, CancellationToken.None);

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

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await coverageDataAttachmentsHandler.ProcessAttachmentSetsAsync(configurationElement, attachment, mockProgressReporter.Object, null, cts.Token));

            Assert.AreEqual(2, attachment.Count);

            mockProgressReporter.Verify(p => p.Report(It.IsAny<int>()), Times.Never);
        }
    }
}
