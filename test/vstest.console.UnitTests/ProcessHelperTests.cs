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
        // The stream has already reached EOF (all ErrorDataReceived callbacks have been delivered).
        var errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        errorStreamClosed.TrySetResult(true);

        var stopwatch = Stopwatch.StartNew();
        await ProcessHelper.WaitForErrorStreamToDrainAsync(errorStreamClosed, timeoutMilliseconds: 5000);
        stopwatch.Stop();

        Assert.IsTrue(errorStreamClosed.Task.IsCompleted, "The method must wait until the error stream is drained.");
        Assert.IsLessThan(
            3000L,
            stopwatch.ElapsedMilliseconds,
            $"The method should return as soon as the stream is drained, not at the timeout (took {stopwatch.ElapsedMilliseconds} ms).");
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
}
