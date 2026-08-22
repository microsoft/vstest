// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Extensions.TestIdsLogger;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using TestIdsLoggerConstants = Microsoft.VisualStudio.TestPlatform.Extensions.TestIdsLogger.Constants;
using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.TestPlatform.Extensions.TestIdsLogger.UnitTests;

[TestClass]
public class TestIdsLoggerTests
{
    // Lower case on purpose: Uri.ToString lower cases the authority, and the seed is built from
    // that rendering, so a mixed case uri here would make the expected seeds below wrong in a way
    // that says nothing about the code under test.
    private const string ExecutorUri = "executor://testidsloggertests/v1";
    private const string Source = @"c:\some\where\SampleTests.dll";
    private const string SourceFileName = "SampleTests.dll";

    private readonly VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger _logger;
    private readonly Mock<TestLoggerEvents> _events;
    private readonly string _testRunDirectory;

    public TestIdsLoggerTests()
    {
        _events = new Mock<TestLoggerEvents>();
        _logger = new VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger();
        _testRunDirectory = Path.Combine(Path.GetTempPath(), "TestIdsLoggerTests", Guid.NewGuid().ToString("d"));
    }

    #region Id computation

    [TestMethod]
    public void CreateRecordComputesBothIdsFromTheSeedThePlatformHashes()
    {
        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source);

        // The seed is the executor uri, then the file name of the source, then the fully qualified
        // name. Spelled out here rather than taken from the shared source, so that this test fails
        // if the composition the logger uses ever drifts from what a test id is actually hashed from.
        string expectedSeed = ExecutorUri + SourceFileName + "SampleTests.UnitTest.PassingTest";

        var record = VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger.CreateRecord(testCase);

        Assert.AreEqual(EqtHash.GuidFromString(expectedSeed), record.Sha1Id);
        Assert.AreEqual(EqtHash.GuidFromString2(expectedSeed), record.XxHash128Id);
        Assert.AreNotEqual(record.Sha1Id, record.XxHash128Id);
    }

    [TestMethod]
    public void CreateRecordUsesTheManagedNameWhenTheTestCaseCarriesBothManagedProperties()
    {
        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source);
        SetManagedName(testCase, "SampleTests.UnitTest", "PassingTest(1,2)");

        string expectedSeed = ExecutorUri + SourceFileName + "SampleTests.UnitTest.PassingTest(1,2)";

        var record = VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger.CreateRecord(testCase);

        Assert.AreEqual(EqtHash.GuidFromString(expectedSeed), record.Sha1Id);
        Assert.AreEqual(EqtHash.GuidFromString2(expectedSeed), record.XxHash128Id);

        // The reported fully qualified name stays the one the adapter reported, so that the row can
        // still be joined against records keyed on it.
        Assert.AreEqual("SampleTests.UnitTest.PassingTest", record.FullyQualifiedName);
    }

    [TestMethod]
    public void CreateRecordIgnoresTheManagedNameWhenOnlyOneHalfOfItIsPresent()
    {
        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source);
        SetManagedName(testCase, "SampleTests.UnitTest", managedMethod: null);

        string expectedSeed = ExecutorUri + SourceFileName + "SampleTests.UnitTest.PassingTest";

        var record = VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger.CreateRecord(testCase);

        Assert.AreEqual(EqtHash.GuidFromString(expectedSeed), record.Sha1Id);
    }

    #endregion

    #region Id source classification

    [TestMethod]
    public void PlatformComputedIdIsNotReportedAsSelfAssigned()
    {
        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source);

        // The id is whatever the algorithm the run selected produced, which is deliberately not
        // pinned here: either way it is a platform computed id and must be reported as one.
        var record = VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger.CreateRecord(testCase);

        Assert.AreNotEqual(TestIdSource.SelfAssigned, record.IdSource);
    }

    [TestMethod]
    public void SelfAssignedIdIsReportedAsSelfAssigned()
    {
        // This is what MSTest v3 and v4 do: they assign the id themselves, so it matches neither
        // hash of the seed and will not change when the default algorithm moves.
        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source)
        {
            Id = new Guid("11111111-2222-3333-4444-555555555555")
        };

        var record = VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger.CreateRecord(testCase);

        Assert.AreEqual(TestIdSource.SelfAssigned, record.IdSource);
        Assert.AreEqual(new Guid("11111111-2222-3333-4444-555555555555"), record.Id);
        Assert.AreNotEqual(record.Id, record.Sha1Id);
        Assert.AreNotEqual(record.Id, record.XxHash128Id);
    }

    [TestMethod]
    public void IdMatchingTheSha1CandidateIsReportedAsSha1()
    {
        string seed = ExecutorUri + SourceFileName + "SampleTests.UnitTest.PassingTest";
        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source)
        {
            Id = EqtHash.GuidFromString(seed)
        };

        var record = VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger.CreateRecord(testCase);

        Assert.AreEqual(TestIdSource.Sha1, record.IdSource);
    }

    [TestMethod]
    public void IdMatchingTheXxHash128CandidateIsReportedAsXxHash128()
    {
        string seed = ExecutorUri + SourceFileName + "SampleTests.UnitTest.PassingTest";
        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source)
        {
            Id = EqtHash.GuidFromString2(seed)
        };

        var record = VisualStudio.TestPlatform.Extensions.TestIdsLogger.TestIdsLogger.CreateRecord(testCase);

        Assert.AreEqual(TestIdSource.XxHash128, record.IdSource);
    }

    #endregion

    #region Report format

    [TestMethod]
    public void ReportStartsWithTheDocumentedHeader()
    {
        string report = WriteReport(Array.Empty<TestIdRecord>());

        Assert.AreEqual("Source,ExecutorUri,FullyQualifiedName,DisplayName,Id,Sha1Id,XxHash128Id,IdSource\r\n", report);
    }

    [TestMethod]
    public void ReportQuotesFieldsContainingSeparatorsAndDoublesEmbeddedQuotes()
    {
        var record = new TestIdRecord(
            Source,
            ExecutorUri,
            "SampleTests.UnitTest.PassingTest",
            "PassingTest (1,2) says \"hi\"\r\nand more",
            Guid.Empty,
            Guid.Empty,
            Guid.Empty);

        string report = WriteReport(new[] { record });
        string row = report.Substring(report.IndexOf("\r\n", StringComparison.Ordinal) + 2);

        Assert.Contains("\"PassingTest (1,2) says \"\"hi\"\"\r\nand more\"", row);
    }

    [TestMethod]
    public void ReportLeavesFieldsWithoutSeparatorsUnquoted()
    {
        var record = new TestIdRecord(
            "SampleTests.dll",
            ExecutorUri,
            "SampleTests.UnitTest.PassingTest",
            "PassingTest",
            Guid.Empty,
            Guid.Empty,
            Guid.Empty);

        string report = WriteReport(new[] { record });

        Assert.Contains("SampleTests.dll," + ExecutorUri + ",SampleTests.UnitTest.PassingTest,PassingTest,", report);
        Assert.DoesNotContain("\"", report);
    }

    #endregion

    #region End to end through the logger

    [TestMethod]
    public void ReportContainsOneRowPerTestRegardlessOfHowManyResultsItProduced()
    {
        _logger.Initialize(_events.Object, BuildParameters());

        var testCase = new TestCase("SampleTests.UnitTest.FlakyTest", new Uri(ExecutorUri), Source);

        // A retried test reports several results for the same test case. The report is a mapping of
        // ids, and one id needs exactly one row.
        _logger.TestResultHandler(this, new TestResultEventArgs(new ObjectModel.TestResult(testCase)));
        _logger.TestResultHandler(this, new TestResultEventArgs(new ObjectModel.TestResult(testCase)));
        _logger.TestRunCompleteHandler(this, new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero));

        string[] lines = ReadReportLines();

        Assert.HasCount(2, lines, "Expected a header and exactly one row.");
        Assert.Contains("SampleTests.UnitTest.FlakyTest", lines[1]);
    }

    [TestMethod]
    public void ReportKeepsDataDrivenRowsThatCarryDistinctIdsApart()
    {
        _logger.Initialize(_events.Object, BuildParameters());

        // Same fully qualified name, distinct self assigned ids - which is exactly the shape MSTest
        // produces for data driven tests, and exactly the case a two run join cannot disambiguate.
        var first = new TestCase("SampleTests.UnitTest.DataDriven", new Uri(ExecutorUri), Source)
        {
            DisplayName = "DataDriven (1)",
            Id = new Guid("aaaaaaaa-0000-0000-0000-000000000001")
        };
        var second = new TestCase("SampleTests.UnitTest.DataDriven", new Uri(ExecutorUri), Source)
        {
            DisplayName = "DataDriven (2)",
            Id = new Guid("aaaaaaaa-0000-0000-0000-000000000002")
        };

        _logger.TestResultHandler(this, new TestResultEventArgs(new ObjectModel.TestResult(first)));
        _logger.TestResultHandler(this, new TestResultEventArgs(new ObjectModel.TestResult(second)));
        _logger.TestRunCompleteHandler(this, new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero));

        string[] lines = ReadReportLines();

        Assert.HasCount(3, lines, "Expected a header and one row per distinct id.");
        Assert.Contains("aaaaaaaa-0000-0000-0000-000000000001", string.Join("\n", lines));
        Assert.Contains("aaaaaaaa-0000-0000-0000-000000000002", string.Join("\n", lines));
        Assert.Contains(nameof(TestIdSource.SelfAssigned), string.Join("\n", lines));
    }

    [TestMethod]
    public void DiscoveryAloneProducesTheReport()
    {
        _logger.Initialize(_events.Object, BuildParameters());

        var testCase = new TestCase("SampleTests.UnitTest.PassingTest", new Uri(ExecutorUri), Source);

        _logger.DiscoveredTestsHandler(this, new DiscoveredTestsEventArgs(new List<TestCase> { testCase }));
        _logger.DiscoveryCompleteHandler(this, new DiscoveryCompleteEventArgs(1, false));

        string[] lines = ReadReportLines();

        Assert.HasCount(2, lines);
        Assert.Contains("SampleTests.UnitTest.PassingTest", lines[1]);
    }

    [TestMethod]
    public void ReportIsWrittenToTheTestResultsDirectoryUnderTheDefaultNameWhenNoLogFileNameIsGiven()
    {
        var parameters = new Dictionary<string, string?>
        {
            [DefaultLoggerParameterNames.TestRunDirectory] = _testRunDirectory,
        };
        _logger.Initialize(_events.Object, parameters);

        _logger.TestRunCompleteHandler(this, new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero));

        Assert.AreEqual(
            Path.Combine(_testRunDirectory, TestIdsLoggerConstants.DefaultReportFileName),
            _logger.ReportFilePath);
        Assert.IsTrue(File.Exists(_logger.ReportFilePath));
    }

    [TestMethod]
    public void RelativeLogFileNameIsResolvedAgainstTheTestResultsDirectory()
    {
        var parameters = new Dictionary<string, string?>
        {
            [DefaultLoggerParameterNames.TestRunDirectory] = _testRunDirectory,
            [TestIdsLoggerConstants.LogFileNameKey] = Path.Combine("ids", "mapping.csv"),
        };
        _logger.Initialize(_events.Object, parameters);

        _logger.TestRunCompleteHandler(this, new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.Zero));

        Assert.AreEqual(Path.Combine(_testRunDirectory, "ids", "mapping.csv"), _logger.ReportFilePath);
        Assert.IsTrue(File.Exists(_logger.ReportFilePath));
    }

    #endregion

    #region Helpers

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testRunDirectory))
            {
                Directory.Delete(_testRunDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Leaving a temp directory behind is not worth failing a test over.
        }
    }

    private Dictionary<string, string?> BuildParameters() => new()
    {
        [DefaultLoggerParameterNames.TestRunDirectory] = _testRunDirectory,
        [TestIdsLoggerConstants.LogFileNameKey] = "TestIds.csv",
    };

    private string[] ReadReportLines()
    {
        Assert.IsNotNull(_logger.ReportFilePath, "The logger did not write a report.");

        return File.ReadAllLines(_logger.ReportFilePath!)
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private static string WriteReport(IEnumerable<TestIdRecord> records)
    {
        using var writer = new StringWriter();
        TestIdReportWriter.Write(writer, records);

        return writer.ToString();
    }

    private static void SetManagedName(TestCase testCase, string? managedType, string? managedMethod)
    {
        if (managedType is not null)
        {
            testCase.SetPropertyValue(
                TestProperty.Register(TestIdsLoggerConstants.ManagedTypePropertyId, "ManagedType", typeof(string), typeof(TestCase)),
                managedType);
        }

        if (managedMethod is not null)
        {
            testCase.SetPropertyValue(
                TestProperty.Register(TestIdsLoggerConstants.ManagedMethodPropertyId, "ManagedMethod", typeof(string), typeof(TestCase)),
                managedMethod);
        }
    }

    #endregion
}
