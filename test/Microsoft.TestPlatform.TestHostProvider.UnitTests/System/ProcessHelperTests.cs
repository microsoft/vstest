// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Tests for PR #15396 — "Determine architecture of remote process on windows"
//
// Key changes in that PR:
//   • Extracted x86 early-return from GetProcessArchitecture into GetCurrentProcessArchitecture
//     so caching works correctly for the current process regardless of call order.
//   • Removed the .NET Core GetProcessArchitecture override that simply returned current-process
//     arch; Windows-specific P/Invoke is now the only implementation.
//   • Added a non-Windows guard in GetProcessArchitecture that throws NotImplementedException
//     (hang-dumper usage is Windows-only).

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.TestHostProvider.UnitTests.System;

[TestClass]
public class ProcessHelperTests
{
    private readonly IProcessHelper _processHelper = new ProcessHelper();

    [TestMethod]
    [TestCategory("PR15396")]
    public void GetCurrentProcessArchitectureShouldReturnValidArchitecture()
    {
        var arch = _processHelper.GetCurrentProcessArchitecture();

        // Must be one of the known values — never throw.
        Assert.IsTrue(
            arch is PlatformArchitecture.X64
                  or PlatformArchitecture.X86
                  or PlatformArchitecture.ARM64
                  or PlatformArchitecture.ARM,
            $"Unexpected architecture value: {arch}");
    }

    [TestMethod]
    [TestCategory("PR15396")]
    public void GetCurrentProcessArchitectureShouldReturnConsistentResultOnRepeatedCalls()
    {
        // PR #15396 introduced caching via _currentProcessArchitecture to avoid
        // re-querying native APIs on every call.
        var first = _processHelper.GetCurrentProcessArchitecture();
        var second = _processHelper.GetCurrentProcessArchitecture();

        Assert.AreEqual(first, second,
            "Repeated calls must return identical architecture (cached).");
    }

    [TestMethod]
    [TestCategory("PR15396")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MSTEST0049", Justification = "Platform guard, no cancellation needed")]
    public void GetProcessArchitectureShouldThrowNotImplementedExceptionOnNonWindows()
    {
        // GetProcessArchitecture for a remote process uses Windows-only P/Invoke.
        // The PR explicitly added a non-Windows guard that throws NotImplementedException.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: method is implemented — just verify it doesn't throw for current pid.
            int pid;
#if NET5_0_OR_GREATER
            pid = Environment.ProcessId;
#else
            using var p = global::System.Diagnostics.Process.GetCurrentProcess();
            pid = p.Id;
#endif
            _processHelper.GetProcessArchitecture(pid); // should not throw
        }
        else
        {
            // Non-Windows: any remote process lookup must throw.
            int pid;
#if NET5_0_OR_GREATER
            pid = Environment.ProcessId;
#else
            using var p = global::System.Diagnostics.Process.GetCurrentProcess();
            pid = p.Id;
#endif
            Assert.ThrowsExactly<NotImplementedException>(
                () => _processHelper.GetProcessArchitecture(pid));
        }
    }
}
