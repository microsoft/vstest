// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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

    [TestMethod]
    public async Task WaitForErrorStreamToDrainShouldReturnOnceTheErrorStreamCloses()
    {
        // EOF arrives AFTER we start waiting, mimicking a slow ErrorDataReceived delivery that lands just after
        // the exit handler begins draining. Start the wait first and assert it is still in progress, then signal
        // EOF: the wait must observe that late completion and return promptly - not return early (dropping the
        // crash callstack) and not block to the timeout.
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var drainTask = ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, timeoutMilliseconds: 5000);
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
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, timeoutMilliseconds: 5000);
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
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, BudgetMs);
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
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, timeoutMilliseconds: 0);
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
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed: null, BudgetMs);
        stopwatch.Stop();

        Assert.IsLessThan(
            250L,
            stopwatch.ElapsedMilliseconds,
            $"With no redirected error stream the method must be a no-op (took {stopwatch.ElapsedMilliseconds} ms).");
    }

    [TestMethod]
    public void GetErrorDrainTimeoutShouldUseTheGenerousBudgetOnlyForACrash()
    {
        // A crash is an abnormal exit we did not cause: not a clean exit and not something we killed.
        var crash = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: false, deliberatelyTerminated: false);
        var cleanExit = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: true, deliberatelyTerminated: false);
        var aborted = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: false, deliberatelyTerminated: true);
        var cleanAndAborted = ProcessHelper.GetErrorDrainTimeout(exitedCleanly: true, deliberatelyTerminated: true);

        Assert.IsGreaterThan(
            cleanExit,
            crash,
            "A crash must wait longer for stderr to drain than a clean exit, so a late crash callstack is captured.");

        // A process we deliberately killed (e.g. aborting from an IDE) must drain as fast as a clean exit, so
        // an abort never hangs for seconds when a grandchild keeps the stderr pipe open.
        Assert.AreEqual(cleanExit, aborted, "A deliberately terminated process must use the short (clean-exit) budget.");
        Assert.AreEqual(cleanExit, cleanAndAborted, "A clean, deliberately terminated process must use the short budget.");
    }
}
