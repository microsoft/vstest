// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;

/// <summary>
/// The test execution recorder used for recording test results and test messages.
/// </summary>
internal class TestExecutionRecorder : TestSessionMessageLogger, ITestExecutionRecorder
{
    private readonly List<AttachmentSet> _attachmentSets;
    private readonly ITestRunCache _testRunCache;
    private readonly ITestCaseEventsHandler? _testCaseEventsHandler;

    /// <summary>
    /// Tracks the number of in-progress starts per test case ID.
    /// Multiple data-driven test executions sharing the same ID are each counted.
    /// </summary>
    private readonly Dictionary<Guid, int> _testCaseInProgressMap;

    /// <summary>
    /// Tracks test case IDs for which <see cref="RecordEnd"/> has been called at least once
    /// while the entry is still in progress (count &gt; 0). Used to suppress the
    /// <see cref="RecordResult"/> safety-net in nested data-driven scenarios: once an
    /// explicit RecordEnd fires for an ID, subsequent RecordResult calls must not send a
    /// spurious extra TestCaseEnd that would consume the parent's pending count slot.
    /// The ID is removed when the last in-progress count reaches zero.
    /// <para>
    /// <b>Known limitation:</b> suppression is ID-scoped, not per-invocation. If an adapter
    /// calls <see cref="RecordEnd"/> for some rows sharing the same <see cref="TestCase.Id"/>
    /// but relies on the <see cref="RecordResult"/> safety-net for others, those latter rows
    /// will have their safety-net suppressed. In practice this is not a concern because
    /// real adapters apply <see cref="RecordEnd"/> uniformly across all rows with the same ID.
    /// </para>
    /// </summary>
    private readonly HashSet<Guid> _testCaseEndCalledSet;

    private readonly object _testCaseInProgressSyncObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TestExecutionRecorder"/> class.
    /// </summary>
    /// <param name="testCaseEventsHandler"> The test Case Events Handler. </param>
    /// <param name="testRunCache"> The test run cache.  </param>
    public TestExecutionRecorder(ITestCaseEventsHandler? testCaseEventsHandler, ITestRunCache testRunCache)
    {
        _testRunCache = testRunCache;
        _testCaseEventsHandler = testCaseEventsHandler;
        _attachmentSets = new List<AttachmentSet>();

        // As a framework guideline, we should get events in this order:
        // 1. Test Case Start.
        // 2. Test Case End.
        // 3. Test Case Result.
        // If that is not that case.
        // If Test Adapters don't send the events in the above order, Test Case Results are cached till the Test Case End event is received.
        _testCaseInProgressMap = new Dictionary<Guid, int>();
        _testCaseEndCalledSet = new HashSet<Guid>();
    }

    /// <summary>
    /// Gets the attachments received from adapters.
    /// </summary>
    internal Collection<AttachmentSet> Attachments
    {
        get
        {
            return new Collection<AttachmentSet>(_attachmentSets);
        }
    }

    /// <summary>
    /// Notify the framework about starting of the test case.
    /// Framework sends this event to data collectors enabled in the run. If no data collector is enabled, then the event is ignored.
    /// </summary>
    /// <param name="testCase">test case which will be started.</param>
    public void RecordStart(TestCase testCase)
    {
        EqtTrace.Verbose("TestExecutionRecorder.RecordStart: Starting test: {0}.", testCase.FullyQualifiedName);
        _testRunCache.OnTestStarted(testCase);

        if (_testCaseEventsHandler != null)
        {
            lock (_testCaseInProgressSyncObject)
            {
                // Track the number of in-progress starts for this test case ID.
                // Data-driven tests may call RecordStart multiple times with the same ID
                // (when rows share the same fully-qualified name), so we must forward
                // every start rather than deduplicating by ID.
                _testCaseInProgressMap[testCase.Id] = _testCaseInProgressMap.TryGetValue(testCase.Id, out int count) ? count + 1 : 1;
                _testCaseEventsHandler.SendTestCaseStart(testCase);
            }
        }
    }

    /// <summary>
    /// Notify the framework about the test result.
    /// </summary>
    /// <param name="testResult">Test Result to be sent to the framework.</param>
    /// <exception cref="TestCanceledException">Exception thrown by the framework when an executor attempts to send
    /// test result to the framework when the test(s) is canceled. </exception>
    public void RecordResult(TestResult testResult)
    {
        EqtTrace.Verbose("TestExecutionRecorder.RecordResult: Received result for test: {0}.", testResult.TestCase.FullyQualifiedName);
        if (_testCaseEventsHandler != null)
        {
            lock (_testCaseInProgressSyncObject)
            {
                // Safety net: send TestCaseEnd in case RecordEnd was not called.
                // Guard: skip if RecordEnd was already called for this ID (indicated by presence in
                // _testCaseEndCalledSet) to avoid consuming a count slot belonging to a parent or
                // sibling in a nested data-driven scenario where all rows share the same TestCase.Id.
                if (_testCaseInProgressMap.TryGetValue(testResult.TestCase.Id, out int count)
                    && count > 0
                    && !_testCaseEndCalledSet.Contains(testResult.TestCase.Id))
                {
                    _testCaseEventsHandler.SendTestCaseEnd(testResult.TestCase, testResult.Outcome);

                    if (count == 1)
                    {
                        _testCaseInProgressMap.Remove(testResult.TestCase.Id);
                    }
                    else
                    {
                        _testCaseInProgressMap[testResult.TestCase.Id] = count - 1;
                    }
                }
            }

            _testCaseEventsHandler.SendTestResult(testResult);
        }

        // Test Result should always be flushed, even if datacollecter attachment is missing
        _testRunCache.OnNewTestResult(testResult);
    }

    /// <summary>
    /// Notify the framework about completion of the test case.
    /// Framework sends this event to data collectors enabled in the run. If no data collector is enabled, then the event is ignored.
    /// </summary>
    /// <param name="testCase">test case which has completed.</param>
    /// <param name="outcome">outcome of the test case.</param>
    public void RecordEnd(TestCase testCase, TestOutcome outcome)
    {
        EqtTrace.Verbose("TestExecutionRecorder.RecordEnd: test: {0} execution completed.", testCase.FullyQualifiedName);
        _testRunCache.OnTestCompletion(testCase);

        if (_testCaseEventsHandler != null)
        {
            lock (_testCaseInProgressSyncObject)
            {
                // TestCaseEnd must always be preceded by TestCaseStart for a given test case id.
                // Use the reference count to ensure we send exactly one End for each Start.
                if (_testCaseInProgressMap.TryGetValue(testCase.Id, out int count) && count > 0)
                {
                    // Mark that RecordEnd was called for this ID while it is still in progress.
                    // This suppresses the RecordResult safety-net for any subsequent Result calls
                    // that share the same ID (e.g., row results in a nested data-driven test).
                    _testCaseEndCalledSet.Add(testCase.Id);

                    _testCaseEventsHandler.SendTestCaseEnd(testCase, outcome);

                    // Decrement the count; remove both tracking entries when there are no more in-progress starts.
                    if (count == 1)
                    {
                        _testCaseInProgressMap.Remove(testCase.Id);
                        _testCaseEndCalledSet.Remove(testCase.Id);
                    }
                    else
                    {
                        _testCaseInProgressMap[testCase.Id] = count - 1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Notify the framework about run level attachments.
    /// </summary>
    /// <param name="attachments"> The attachment sets. </param>
    public void RecordAttachments(IList<AttachmentSet> attachments)
    {
        _attachmentSets.AddRange(attachments);
    }
}
