// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.TestPlatform.Hashing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.TestIdsLogger;

/// <summary>
/// Reports every test in a run together with both the SHA1 derived and the xxHash128 derived test
/// id, so that ids stored before the id hashing algorithm changed can be mapped onto the ids that
/// will replace them.
/// </summary>
/// <remarks>
/// <para>
/// ==================================================================================
/// TEMPORARY - THIS LOGGER WILL BE REMOVED.
/// ==================================================================================
/// It exists for exactly one reason: to let consumers who persisted platform computed test case ids
/// build an old id to new id mapping before <c>xxhash128</c> becomes the default algorithm. It is
/// deleted at the same time as the SHA1 implementation it reports on, and there will be no
/// replacement, because once SHA1 is gone there is nothing left to map from. Anything that depends
/// on this logger as a permanent part of a pipeline is using it wrong and will break when it goes.
/// </para>
/// <para>
/// Without it the only way to obtain the mapping is to run the suite twice with
/// <c>VSTEST_TESTCASE_ID_ALGORITHM</c> flipped and join the two reports, and that join is genuinely
/// ambiguous for data driven tests whose arguments do not render distinctly. This logger removes the
/// join entirely: one run, both ids on the same row.
/// </para>
/// <para>
/// The report distinguishes the id a test actually carries from the two the platform would compute,
/// because they are not always the same thing. An adapter may assign an id itself instead of letting
/// the platform hash one - MSTest v3 and v4 do, through their own id generation strategy - and such
/// an id matches neither candidate and will not move when the default changes. Those rows are
/// reported as <see cref="TestIdSource.SelfAssigned"/> rather than being quietly presented as though
/// they were about to change.
/// </para>
/// </remarks>
[FriendlyName(Constants.FriendlyName)]
[ExtensionUri(Constants.ExtensionUri)]
public class TestIdsLogger : ITestLoggerWithParameters
{
    /// <summary>
    /// The reported tests, keyed on the identity that determines the id, so that a test reported
    /// more than once is reported here once.
    /// </summary>
    private ConcurrentDictionary<string, TestIdRecord>? _records;

    private Dictionary<string, string?>? _parametersDictionary;
    private string? _testResultsDirPath;

    /// <summary>
    /// The path the report was written to, once it has been written.
    /// </summary>
    public string? ReportFilePath { get; private set; }

    /// <inheritdoc/>
    public void Initialize(TestLoggerEvents events, string testResultsDirPath)
    {
        ValidateArg.NotNull(events, nameof(events));
        ValidateArg.NotNullOrEmpty(testResultsDirPath, nameof(testResultsDirPath));

        _testResultsDirPath = testResultsDirPath;
        _records = new ConcurrentDictionary<string, TestIdRecord>();

        events.TestResult += TestResultHandler;
        events.TestRunComplete += TestRunCompleteHandler;

        // Discovery is subscribed to as well, so that listing tests is enough to produce the
        // mapping. Migrating stored ids does not require anything to actually be executed, and
        // asking someone to run a suite they only want the ids of is a needless cost.
        events.DiscoveredTests += DiscoveredTestsHandler;
        events.DiscoveryComplete += DiscoveryCompleteHandler;
    }

    /// <inheritdoc/>
    public void Initialize(TestLoggerEvents events, Dictionary<string, string?> parameters)
    {
        ValidateArg.NotNull(parameters, nameof(parameters));
        if (parameters.Count == 0)
        {
            throw new ArgumentException("No default parameters added", nameof(parameters));
        }

        _parametersDictionary = parameters;
        Initialize(events, parameters[DefaultLoggerParameterNames.TestRunDirectory]!);
    }

    /// <summary>
    /// Records the test a result belongs to.
    /// </summary>
    public void TestResultHandler(object? sender, TestResultEventArgs e)
    {
        ValidateArg.NotNull(e, nameof(e));
        Record(e.Result.TestCase);
    }

    /// <summary>
    /// Records the tests reported by a discovery.
    /// </summary>
    public void DiscoveredTestsHandler(object? sender, DiscoveredTestsEventArgs e)
    {
        ValidateArg.NotNull(e, nameof(e));

        if (e.DiscoveredTestCases is null)
        {
            return;
        }

        foreach (TestCase testCase in e.DiscoveredTestCases)
        {
            Record(testCase);
        }
    }

    /// <summary>
    /// Writes the report at the end of a run.
    /// </summary>
    public void TestRunCompleteHandler(object? sender, TestRunCompleteEventArgs e) => WriteReport();

    /// <summary>
    /// Writes the report at the end of a discovery.
    /// </summary>
    public void DiscoveryCompleteHandler(object? sender, DiscoveryCompleteEventArgs e) => WriteReport();

    private void Record(TestCase? testCase)
    {
        if (testCase is null || _records is null)
        {
            return;
        }

        TestIdRecord record = CreateRecord(testCase);

        // First one reported wins. A test that is retried, or that reports several results, is the
        // same test with the same id, and repeating it would only make the report harder to load.
        _records.TryAdd(BuildKey(record), record);
    }

    /// <summary>
    /// Builds the record for a test case, computing both candidate ids from the same seed the
    /// platform hashes.
    /// </summary>
    internal static TestIdRecord CreateRecord(TestCase testCase)
    {
        ValidateArg.NotNull(testCase, nameof(testCase));

        string executorUri = testCase.ExecutorUri?.ToString() ?? string.Empty;

        // Reproduces TestCase.GetFullyQualifiedName: the managed name wins when the adapter reported
        // both halves of it, because that is what the platform hashes in that case.
        string? managedType = GetPropertyById(testCase, Constants.ManagedTypePropertyId);
        string? managedMethod = GetPropertyById(testCase, Constants.ManagedMethodPropertyId);
        string fullyQualifiedName = !managedType.IsNullOrWhiteSpace() && !managedMethod.IsNullOrWhiteSpace()
            ? $"{managedType}.{managedMethod}"
            : testCase.FullyQualifiedName;

        string seed = TestIdSeed.Compose(executorUri, testCase.Source, fullyQualifiedName);

        return new TestIdRecord(
            testCase.Source ?? string.Empty,
            executorUri,
            testCase.FullyQualifiedName ?? string.Empty,
            testCase.DisplayName ?? string.Empty,
            testCase.Id,
            EqtHash.GuidFromString(seed),
            EqtHash.GuidFromString2(seed));
    }

    /// <summary>
    /// The identity a record is deduplicated on.
    /// </summary>
    /// <remarks>
    /// The id is part of the key rather than the whole of it. Data driven tests can share a fully
    /// qualified name while carrying distinct self assigned ids, and those are distinct rows because
    /// each id needs its own mapping; the same test reported twice carries the same id and collapses.
    /// The display name is deliberately not part of the key: one id maps to one id no matter how many
    /// ways it was rendered, and including it would emit rows that differ only in a column the
    /// mapping does not use.
    /// </remarks>
    private static string BuildKey(TestIdRecord record)
        => string.Join("\u0000", new[] { record.Source, record.ExecutorUri, record.FullyQualifiedName, record.Id.ToString("d", CultureInfo.InvariantCulture) });

    /// <summary>
    /// Reads a test property by its id from the properties the test case actually carries.
    /// </summary>
    private static string? GetPropertyById(TestCase testCase, string propertyId)
    {
        foreach (TestProperty property in testCase.Properties)
        {
            if (string.Equals(property.Id, propertyId, StringComparison.Ordinal))
            {
                return testCase.GetPropertyValue<string>(property, null);
            }
        }

        return null;
    }

    private void WriteReport()
    {
        if (_records is null)
        {
            return;
        }

        try
        {
            ReportFilePath = ResolveReportFilePath();

            string? directory = Path.GetDirectoryName(ReportFilePath);
            if (!directory.IsNullOrEmpty())
            {
                Directory.CreateDirectory(directory);
            }

            // Ordered so that two runs of the same suite produce byte identical reports, which is
            // what makes diffing one against another useful.
            List<TestIdRecord> ordered = _records.Values
                .OrderBy(r => r.Source, StringComparer.Ordinal)
                .ThenBy(r => r.FullyQualifiedName, StringComparer.Ordinal)
                .ThenBy(r => r.Id.ToString("d", CultureInfo.InvariantCulture), StringComparer.Ordinal)
                .ToList();

            using var writer = new StreamWriter(
                new FileStream(ReportFilePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            TestIdReportWriter.Write(writer, ordered);
        }
        catch (Exception ex)
        {
            EqtTrace.Error("TestIdsLogger: Failed to write the test id report. {0}", ex);
        }
    }

    private string ResolveReportFilePath()
    {
        TPDebug.Assert(_testResultsDirPath is not null, "Initialize must be called before this method.");

        string fileName = Constants.DefaultReportFileName;
        if (_parametersDictionary is not null
            && _parametersDictionary.TryGetValue(Constants.LogFileNameKey, out string? logFileNameValue)
            && !logFileNameValue.IsNullOrWhiteSpace())
        {
            fileName = logFileNameValue;
        }

        return Path.IsPathRooted(fileName) ? fileName : Path.Combine(_testResultsDirPath!, fileName);
    }
}
