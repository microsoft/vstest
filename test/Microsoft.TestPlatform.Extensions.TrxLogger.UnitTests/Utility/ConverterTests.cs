// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests.Utility
{
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using TestOutcome = VisualStudio.TestPlatform.ObjectModel.TestOutcome;
    using TrxLoggerOutcome = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel.TestOutcome;
    using UriDataAttachment = VisualStudio.TestPlatform.ObjectModel.UriDataAttachment;

    [TestClass]
    public class ConverterTests
    {
        [TestMethod]
        public void ToOutcomeShouldMapFailedToFailed()
        {
            Assert.AreEqual(TrxLoggerOutcome.Failed, Converter.ToOutcome(TestOutcome.Failed));
        }

        [TestMethod]
        public void ToOutcomeShouldMapPassedToPassed()
        {
            Assert.AreEqual(TrxLoggerOutcome.Passed, Converter.ToOutcome(TestOutcome.Passed));
        }

        [TestMethod]
        public void ToOutcomeShouldMapSkippedToNotExecuted()
        {
            Assert.AreEqual(TrxLoggerOutcome.NotExecuted, Converter.ToOutcome(TestOutcome.Skipped));
        }

        [TestMethod]
        public void ToOutcomeShouldMapNoneToNotExecuted()
        {
            Assert.AreEqual(TrxLoggerOutcome.NotExecuted, Converter.ToOutcome(TestOutcome.None));
        }

        [TestMethod]
        public void ToOutcomeShouldMapNotFoundToNotExecuted()
        {
            Assert.AreEqual(TrxLoggerOutcome.NotExecuted, Converter.ToOutcome(TestOutcome.NotFound));
        }

        [TestMethod]
        public void ToCollectionEntriesShouldRenameAttachmentUriIfTheAttachmentNameIsSame()
        {
            ConverterTests.SetupForToCollectionEntries(out var tempDir, out var attachmentSets, out var testRun, out var testResultsDirectory);

            List<CollectorDataEntry> collectorDataEntries = Converter.ToCollectionEntries(attachmentSets, testRun, testResultsDirectory);

            Assert.AreEqual($@"{Environment.MachineName}\123.coverage", ((ObjectModel.UriDataAttachment) collectorDataEntries[0].Attachments[0]).Uri.OriginalString);
            Assert.AreEqual($@"{Environment.MachineName}\123[1].coverage", ((ObjectModel.UriDataAttachment)collectorDataEntries[0].Attachments[1]).Uri.OriginalString);

            Directory.Delete(tempDir, true);
        }

        private static void SetupForToCollectionEntries(out string tempDir, out List<AttachmentSet> attachmentSets, out TestRun testRun,
            out string testResultsDirectory)
        {
            ConverterTests.CreateTempCoverageFiles(out tempDir, out var coverageFilePath1, out var coverageFilePath2);

            UriDataAttachment uriDataAttachment1 =
                new UriDataAttachment(new Uri($"file:///{coverageFilePath1}"), "Description 1");
            UriDataAttachment uriDataAttachment2 =
                new UriDataAttachment(new Uri($"file:///{coverageFilePath2}"), "Description 2");
            attachmentSets = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri("datacollector://microsoft/CodeCoverage/2.0"), "Code Coverage")
            };

            testRun = new TestRun(Guid.NewGuid());
            testRun.RunConfiguration = new TestRunConfiguration("Testrun 1");
            attachmentSets[0].Attachments.Add(uriDataAttachment1);
            attachmentSets[0].Attachments.Add(uriDataAttachment2);
            testResultsDirectory = Path.Combine(tempDir, "TestResults");
        }

        private static void CreateTempCoverageFiles(out string tempDir, out string coverageFilePath1,
            out string coverageFilePath2)
        {
            tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var covDir1 = Path.Combine(tempDir, Guid.NewGuid().ToString());
            var covDir2 = Path.Combine(tempDir, Guid.NewGuid().ToString());

            Directory.CreateDirectory(covDir1);
            Directory.CreateDirectory(covDir2);

            coverageFilePath1 = Path.Combine(covDir1, "123.coverage");
            coverageFilePath2 = Path.Combine(covDir2, "123.coverage");

            File.WriteAllText(coverageFilePath1, string.Empty);
            File.WriteAllText(coverageFilePath2, string.Empty);
        }
    }
}
