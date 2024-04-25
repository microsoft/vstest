// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System;

using Microsoft.VisualStudio.TestPlatform.Execution;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine;

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
    public static int Main(string[]? args) => Run(args, new());

    internal static int Run(string[]? args, UiLanguageOverride uiLanguageOverride)
    {
        if (!FeatureFlag.Instance.IsSet(FeatureFlag.VSTEST_DISABLE_UTF8_CONSOLE_ENCODING))
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        uiLanguageOverride.SetCultureSpecifiedByUser();
        return new Executor(ConsoleOutput.Instance).Execute(args);
    }
}
