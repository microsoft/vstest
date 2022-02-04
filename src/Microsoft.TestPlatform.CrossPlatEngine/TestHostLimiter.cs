// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

static class TestHostLimiter
{
    private const string VSTEST_RUNNER_MAXPARALLELLEVEL = nameof(VSTEST_RUNNER_MAXPARALLELLEVEL);
    private static readonly int MaxCount;
    private static readonly SemaphoreSlim Semaphore;

    static TestHostLimiter()
    {
        // The maximum amount of testhosts we want to start at the same time is the amount of logical processors
        // we have on this system.
        var env = Environment.GetEnvironmentVariable(VSTEST_RUNNER_MAXPARALLELLEVEL);

        int? parallelLevel = string.IsNullOrWhiteSpace(env)
            ? null
            : int.TryParse(env, out int number) ?
                number
                : null;

        if (EqtTrace.IsVerboseEnabled)
        {
            EqtTrace.Verbose($"TestHostLimiter.ctor: {VSTEST_RUNNER_MAXPARALLELLEVEL} is set to '{env}'.");
        }

        if (parallelLevel != null && parallelLevel < 1)
        {
            parallelLevel = 1;
        }

        MaxCount = parallelLevel ?? Environment.ProcessorCount;

        if (EqtTrace.IsVerboseEnabled)
        {
            EqtTrace.Verbose($"TestHostLimiter.ctor: Maximum parallel level for testhosts is {MaxCount}.");
        }

        // We initialize the semaphore to have as many available slots,
        // as we have processors. And the current count is the same, meaning
        // that are slots are "empty".
        Semaphore = new SemaphoreSlim(MaxCount, MaxCount);
    }

    public static void Wait(CancellationToken cancellationToken, [CallerMemberName] string caller = null)
    {
        if (EqtTrace.IsVerboseEnabled)
        {
            EqtTrace.Verbose($"TestHostLimiter.Wait: Waiting for an empty slot for a testhost. There are currently {MaxCount - Semaphore.CurrentCount}/{MaxCount} test hosts running. Caller {caller}.");
        }
        Semaphore.Wait(cancellationToken);
        EqtTrace.Verbose($"TestHostLimiter.Wait: Got a slot for a testhost. There are currently {MaxCount - Semaphore.CurrentCount}/{MaxCount} test hosts running. Caller {caller}.");
    }

    public static int Release([CallerMemberName] string caller = null)
    {
        var previousCount = Semaphore.Release();
        if (EqtTrace.IsVerboseEnabled)
        {
            // Release returns the previous count, so if there is 1 testhost out of 3 running
            // we will see count 2, and max 3. 3 - 2 - 1 = 0 testhosts running.
            var count = MaxCount - previousCount - 1;
            EqtTrace.Verbose($"TestHostLimiter.Release: Released a slot for testhost, there are currently {count}/{MaxCount} test hosts running. Caller {caller}.");
        }
        return previousCount;
    }
}
