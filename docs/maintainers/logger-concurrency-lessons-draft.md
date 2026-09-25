# Draft: Making parallel loggers safe without changing their output

> **Unpublished review artifact.** This is a maintainer-facing draft based on the merged implementation in [microsoft/vstest#16326](https://github.com/microsoft/vstest/pull/16326), the original report in [microsoft/vstest#16320](https://github.com/microsoft/vstest/issues/16320), its regression tests, and the PR checks. It is not published guidance yet.

`TrxLogger` and `HtmlLogger` receive result and message events concurrently during parallel test execution. Before this fix, both loggers stored some of those events in mutable collections that assumed a single writer. The difficult part was not just replacing `List<T>` and `Dictionary<TKey, TValue>`. The fix also had to preserve TRX formatting, ordered-test link order, and the XML shape consumed by `Html.xslt`.

## What was unsafe?

These are verified facts from the merged source:

- `TrxLogger` appended run-level warnings and errors to a `List<RunInfo>`, and informational messages to a `StringBuilder`. Neither supports concurrent writers. The merged code uses [`ConcurrentQueue<RunInfo>` and `ConcurrentQueue<string>`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.TrxLogger/TrxLogger.cs#L74-L82).
- Test-run creation was check-then-act: multiple result callbacks could observe no run and each create one. The merged [`GetOrCreateTestRun`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.TrxLogger/TrxLogger.cs#L630-L668) uses double-checked locking.
- Ordered-test links used a plain `Dictionary<Guid, TestLink>` with a separate `ContainsKey` and `Add`. The merged [`TestElementAggregation`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.TrxLogger/ObjectModel/TestElementAggregation.cs#L17-L65) locks mutation and snapshot enumeration.
- `HtmlLogger` used a `TryGetValue` / create / `TryAdd` sequence and then independently appended to `ResultCollectionList`. Concurrent callbacks could publish duplicate per-source collections. The merged [`TestResultHandler`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.HtmlLogger/HtmlLogger.cs#L190-L253) uses atomic `GetOrAdd` and publishes only the instance that won.
- HTML result lists, failed-result lists, inner results, and run-level message lists were still ordinary `List<T>` instances with concurrent writers. The merged helpers serialize those mutations with locks in [`TestResultCollection`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.HtmlLogger/ObjectModel/TestResultCollection.cs#L18-L72), [`HtmlTestResult`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.HtmlLogger/ObjectModel/HtmlTestResult.cs#L18-L73), and [`TestRunDetails`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.HtmlLogger/ObjectModel/TestRunDetails.cs#L17-L77).

The original issue correctly identified the races, but two initial remedies were not compatible enough. [microsoft/vstest#16320](https://github.com/microsoft/vstest/issues/16320) suggested a `ConcurrentDictionary` for ordered-test links and eager initialization for HTML message lists. The merged implementation deliberately kept an ordered dictionary behind a lock and kept the HTML lists lazy.

## Why `ConcurrentQueue` and `Volatile`?

The run-level TRX messages are append-only until the run summary is created. `ConcurrentQueue<T>` fits that ownership model: concurrent handlers enqueue, then completion reads a snapshot. Informational messages are materialized with one `AppendLine` per queued item in [`GetRunLevelInformationalMessage`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.TrxLogger/TrxLogger.cs#L197-L213), preserving the previous newline-per-message TRX format instead of changing the serialized value.

The test run has different requirements. It must be initialized once, and readers use a lock-free fast path after creation. The lock provides uniqueness. [`Volatile.Read` and `Volatile.Write`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.TrxLogger/TrxLogger.cs#L217-L228) provide publication semantics for that fast path. `GetOrCreateTestRun` assigns `Started` and `RunConfiguration` before publishing the reference, so a thread that observes a non-null run also observes the fully initialized object.

## Why keep a lock for ordered-test links?

`ConcurrentDictionary<TKey, TValue>` would make the add atomic, but it does not promise insertion-order enumeration. Ordered tests serialize links whose order is meaningful. The fix therefore keeps `Dictionary<Guid, TestLink>`, performs check-and-add under one lock, and copies the values under the same lock before serialization.

This is a compatibility constraint, not a preference for locks in general. Thread safety was required, but changing link order could change the meaning of an ordered test even when no exception or lost update occurred.

## Why keep null different from empty in HTML XML?

`RunLevelMessageInformational` and `RunLevelMessageErrorAndWarning` are data members. A null member and an empty list do not necessarily produce the same serialized XML, and `Html.xslt` consumes that XML shape. Eagerly creating both lists in `Initialize` would remove a race, but it could also turn an absent XML element into an empty one.

The merged [`TestRunDetails` helpers](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.HtmlLogger/ObjectModel/TestRunDetails.cs#L45-L77) create each list lazily while holding the lock. The existing [`TestMessageHandlerShouldNotInitializelistForInformationErrorAndWarningMessages`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/test/Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests/HtmlLoggerTests.cs#L115-L120) test verifies the no-message case remains null.

## How did TRX write failures become visible?

`PopulateTrxFile` previously handled only `UnauthorizedAccessException`. Other expected write failures could escape without the logger emitting its normal user-facing diagnostic.

The merged [`PopulateTrxFile`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/src/Microsoft.TestPlatform.Extensions.TrxLogger/TrxLogger.cs#L477-L505) reports `IOException`, `UnauthorizedAccessException`, `XmlException`, `NotSupportedException`, and `SecurityException` through both `EqtTrace.Error` and `ConsoleOutput.Instance.Error`. The exception filter is intentionally limited to expected write and serialization failures. It does not turn arbitrary programming errors into a successful-looking logger result.

## Evidence

The source and test rows below point to the merge commit. The Release test counts are the validation recorded in the [microsoft/vstest#16326](https://github.com/microsoft/vstest/pull/16326) description. The platform rows are successful checks on the PR head commit [`dedcf4c8`](https://github.com/microsoft/vstest/commit/dedcf4c80509d95048e5ea4d4ef5845afb9b88dd).

| Evidence | Result | Primary source |
|---|---|---|
| `TestMessageHandlerShouldBeThreadSafeForRunLevelErrorsAndWarnings` | 10 threads enqueue 1,000 warnings without loss | [`TrxLoggerTests.cs`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/test/Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests/TrxLoggerTests.cs#L1181-L1202) |
| `TestMessageHandlerShouldBeThreadSafeForRunLevelInformationalMessages` | 10 threads enqueue 1,000 informational messages without loss or corruption | [`TrxLoggerTests.cs`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/test/Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests/TrxLoggerTests.cs#L1204-L1227) |
| `TestResultHandlerShouldCreateExactlyOneTestRunUnderConcurrency` | 500 concurrent results observe one run ID | [`TrxLoggerTests.cs`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/test/Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests/TrxLoggerTests.cs#L1229-L1253) |
| `PopulateTrxFileShouldNotThrowWhenTheFileCannotBeWritten` | An `IOException` does not escape `PopulateTrxFile` | [`TrxLoggerTests.cs`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/test/Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests/TrxLoggerTests.cs#L1255-L1265) |
| `TestResultHandlerShouldCreateExactlyOneResultCollectionPerSourceUnderConcurrency` | 500 concurrent results produce one source collection with no missing total or failed results | [`HtmlLoggerTests.cs`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/test/Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests/HtmlLoggerTests.cs#L684-L714) |
| `TestMessageHandlerShouldNotLoseMessagesUnderConcurrency` | 10 threads add 1,000 informational and 1,000 error messages without loss | [`HtmlLoggerTests.cs`](https://github.com/microsoft/vstest/blob/17c14fe899f1633f8d4805f67abd2fd51a8da77b/test/Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests/HtmlLoggerTests.cs#L716-L737) |
| TrxLogger Release unit tests | 154 passed on `net481`; 154 passed on `net11.0` | [PR validation record](https://github.com/microsoft/vstest/pull/16326) |
| HtmlLogger Release unit tests | 78 passed on `net481`; 78 passed on `net11.0` | [PR validation record](https://github.com/microsoft/vstest/pull/16326) |
| Windows Release | Successful | [Azure DevOps check](https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1533196&view=logs&jobId=7c8326b9-0a5f-532a-e6de-db8515c72d9a) |
| macOS | Successful | [Azure DevOps check](https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1533196&view=logs&jobId=ed924aa2-5d5f-549d-e70d-8f7493522721) |
| Ubuntu | Successful | [Azure DevOps check](https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1533196&view=logs&jobId=04e19d90-9c8b-56b5-ee92-0c5f364944c3) |
| Source-build (managed) | Successful | [Azure DevOps check](https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1533196&view=logs&jobId=2f0d093c-1064-5c86-fc5b-b7b1eca8e66a) |

## General advice

The following points are broader guidance derived from this change, not additional claims proved by its tests:

- Start from the data contract, not from a preferred concurrent collection. Ask whether ordering, absence, formatting, or public types are observable before changing storage.
- Use a concurrent collection when its semantics match the operation. `ConcurrentQueue<T>` matched append-then-snapshot messages. It was not a safe replacement for an insertion-ordered dictionary.
- Treat compound operations as the concurrency boundary. Making one dictionary concurrent does not make `lookup → create → publish to another list` atomic.
- Separate uniqueness from publication. A lock can ensure one initializer; `Volatile` can make the initialized object safely visible to readers that skip the lock.
- Add contention tests that assert counts and identity, but keep compatibility tests for serialized shape and ordering. A race fix can preserve every item and still change output.
- Report expected output failures explicitly, and keep the exception filter narrow enough that unrelated defects still fail normally.

The reusable lesson is simple: make the mutation safe, then prove that the serialized contract stayed the same.

🤖
