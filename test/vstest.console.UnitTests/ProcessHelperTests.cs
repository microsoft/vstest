// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests;

/// <summary>
/// Tests for <see cref="ProcessHelper.WaitForErrorStreamToDrainAsync"/>, the bounded wait that lets the process
/// exit callback observe the complete standard error output of a crashed test host. Without it, the exit
/// callback could read the asynchronously-collected stderr before all ErrorDataReceived callbacks had run,
/// dropping a crash callstack such as "Stack overflow." (the cause of the flaky
/// RunTestsShouldThrowOnStackOverflowException test).
/// </summary>
[TestClass]
public class ProcessHelperTests
{
    private const int BudgetMs = 500;

    // The drain's token means "we are tearing this process down", which is unrelated to test cancellation, so
    // tests that do not exercise teardown pass a token that is never signaled rather than
    // TestContext.CancellationToken.
    private static readonly CancellationTokenSource NoTearDownSource = new();
    private static CancellationToken NoTearDown => NoTearDownSource.Token;

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldReturnOnceTheErrorStreamCloses()
    {
        // EOF arrives AFTER we start waiting, mimicking a slow ErrorDataReceived delivery that lands just after
        // the exit handler begins draining. Start the wait first and assert it is still in progress, then signal
        // EOF: the wait must observe that late completion and return promptly - not return early (dropping the
        // crash callstack) and not block to the timeout.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var drainTask = ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, timeoutMilliseconds: 5000, NoTearDown);
        Assert.IsFalse(drainTask.IsCompleted, "The wait must still be in progress before the error stream drains.");

        // The late ErrorDataReceived EOF finally arrives; the wait must observe it and return promptly. The
        // budget above is far larger than this should take, so if the wait ignored EOF and always ran to the
        // timeout the elapsed-time assertion below would catch it (the await would take ~5s, not a few ms).
        var stopwatch = Stopwatch.StartNew();
        errorStreamClosed.TrySetResult(true);

        await drainTask;
        stopwatch.Stop();

        Assert.IsTrue(errorStreamClosed.Task.IsCompleted, "The method must wait until the error stream is drained.");
        Assert.IsLessThan(
            2000L,
            stopwatch.ElapsedMilliseconds,
            $"The wait must return as soon as the late EOF is observed, not run to the timeout (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldReturnImmediatelyWhenAlreadyDrained()
    {
        // The stream has already reached EOF (all ErrorDataReceived callbacks have been delivered) before we
        // start waiting, so the common fast path must not add any latency.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        errorStreamClosed.TrySetResult(true);

        var stopwatch = Stopwatch.StartNew();
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, timeoutMilliseconds: 5000, NoTearDown);
        stopwatch.Stop();

        Assert.IsLessThan(
            250L,
            stopwatch.ElapsedMilliseconds,
            $"When the stream is already drained the method must return immediately (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldBeBoundedWhenTheErrorStreamNeverCloses()
    {
        // Models a grandchild process keeping the pipe open: EOF never arrives.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var stopwatch = Stopwatch.StartNew();
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, BudgetMs, NoTearDown);
        stopwatch.Stop();

        Assert.IsFalse(errorStreamClosed.Task.IsCompleted, "Precondition: the stream never closes in this test.");
        Assert.IsGreaterThanOrEqualTo(
            150L,
            stopwatch.ElapsedMilliseconds,
            $"The method should wait roughly the timeout for the stream (waited only {stopwatch.ElapsedMilliseconds} ms).");
        Assert.IsLessThan(
            5000L,
            stopwatch.ElapsedMilliseconds,
            $"The wait must be bounded so it cannot hang (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldNotWaitWhenTimeoutIsNotPositive()
    {
        // A non-positive timeout means there is no time budget left to wait for stderr - we must not add
        // any latency on top.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var stopwatch = Stopwatch.StartNew();
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, timeoutMilliseconds: 0, NoTearDown);
        stopwatch.Stop();

        Assert.IsFalse(errorStreamClosed.Task.IsCompleted);
        Assert.IsLessThan(
            250L,
            stopwatch.ElapsedMilliseconds,
            $"With a non-positive timeout the method must return immediately (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldReturnImmediatelyWhenThereIsNoErrorStream()
    {
        var stopwatch = Stopwatch.StartNew();
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed: null, BudgetMs, NoTearDown);
        stopwatch.Stop();

        Assert.IsLessThan(
            250L,
            stopwatch.ElapsedMilliseconds,
            $"With no redirected error stream the method must be a no-op (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public void GetErrorDrainTimeoutShouldUseTheGenerousBudgetOnlyForACrash()
    {
        // A crash is an abnormal exit of a process we were not tearing down.
        var crash = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: false, tearingDown: false);
        var cleanExit = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: true, tearingDown: false);
        var tearDown = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: false, tearingDown: true);
        var cleanTearDown = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: true, tearingDown: true);

        Assert.IsGreaterThan(
            cleanExit,
            crash,
            "A crash must wait longer for stderr to drain than a clean exit, so a late crash callstack is captured.");

        // A process we are tearing down (e.g. aborting from an IDE) must drain fastest of all, so an abort never
        // stalls for seconds when a grandchild keeps the stderr pipe open.
        Assert.IsLessThan(
            cleanExit,
            tearDown,
            "A process we are tearing down must use a shorter budget than even a clean exit.");
        Assert.AreEqual(
            tearDown,
            cleanTearDown,
            "Tearing down decides the budget on its own; the exit code cannot make it wait longer.");
    }

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldBeCutShortWhenTearDownIsSignaledUpFront()
    {
        // We already asked to tear the process down before its exit was handled (the abort/cleanup case). EOF
        // never arrives because a grandchild keeps the pipe open, so only the short teardown budget may be spent
        // even though the caller passed the generous crash budget.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var tearDown = new CancellationTokenSource();
        tearDown.Cancel();

        var stopwatch = Stopwatch.StartNew();
        await ProcessHelper.WaitForErrorStreamToDrainAsync(
            errorStreamClosed,
            timeoutMilliseconds: 30000,
            tearDown.Token,
            tearDownTimeoutMilliseconds: 100);
        stopwatch.Stop();

        Assert.IsFalse(errorStreamClosed.Task.IsCompleted, "Precondition: the stream never closes in this test.");
        Assert.IsLessThan(
            5000L,
            stopwatch.ElapsedMilliseconds,
            $"An already-signaled teardown must collapse the budget, not spend it (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldBeCutShortWhenTearDownArrivesWhileWaiting()
    {
        // The process crashed, so we started spending the generous budget, and only then did the user abort.
        // The wait must react to that instead of running the crash budget to completion - this is what makes
        // aborting a run from an IDE responsive.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var tearDown = new CancellationTokenSource();

        var stopwatch = Stopwatch.StartNew();
        var drainTask = ProcessHelper.WaitForErrorStreamToDrainAsync(
            errorStreamClosed,
            timeoutMilliseconds: 30000,
            tearDown.Token,
            tearDownTimeoutMilliseconds: 100);
        Assert.IsFalse(drainTask.IsCompleted, "The wait must be in progress before the teardown is signaled.");

        tearDown.Cancel();
        await drainTask;
        stopwatch.Stop();

        Assert.IsFalse(errorStreamClosed.Task.IsCompleted, "Precondition: the stream never closes in this test.");
        Assert.IsLessThan(
            5000L,
            stopwatch.ElapsedMilliseconds,
            $"A teardown signaled mid-wait must cut the wait short (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldStillCaptureOutputThatArrivesDuringTearDown()
    {
        // Tearing down shortens the wait but does not make it give up instantly: stderr that is already sitting
        // in the pipe costs nothing to pick up, and dropping it would replace a real error with a blank one.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var tearDown = new CancellationTokenSource();
        tearDown.Cancel();

        var drainTask = ProcessHelper.WaitForErrorStreamToDrainAsync(
            errorStreamClosed,
            timeoutMilliseconds: 30000,
            tearDown.Token,
            tearDownTimeoutMilliseconds: 5000);

        errorStreamClosed.TrySetResult(true);
        await drainTask;

        Assert.IsTrue(errorStreamClosed.Task.IsCompleted, "EOF that arrives within the teardown budget must still be observed.");
    }
}
