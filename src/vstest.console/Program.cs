// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CommandLine;

using Execution;

using Utilities;

/// <summary>
/// Main entry point for the command line runner.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point. Hands off execution to the executor class.
    /// </summary>
    /// <param name="args">Arguments provided on the command line.</param>
    /// <returns>0 if everything was successful and 1 otherwise.</returns>
    public static int Main(string[] args)
    {
        DebuggerBreakpoint.AttachVisualStudioDebugger("VSTEST_RUNNER_DEBUG_ATTACHVS");
        DebuggerBreakpoint.WaitForDebugger("VSTEST_RUNNER_DEBUG");
        UiLanguageOverride.SetCultureSpecifiedByUser();
        return new Executor(ConsoleOutput.Instance).Execute(args);
    }
}
