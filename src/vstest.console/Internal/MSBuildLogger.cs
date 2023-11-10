// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;

// Not using FriendlyName because it 
[ExtensionUri(ExtensionUri)]
[FriendlyName(FriendlyName)]
internal class MSBuildLogger : ITestLoggerWithParameters
{
    public const string ExtensionUri = "logger://Microsoft/TestPlatform/MSBuildLogger/v1";

    // This name is not so friendly on purpose, because MSBuild seems like a name that someone might have
    // already claimed, and we will use this just programmatically.
    public const string FriendlyName = "Microsoft.TestPlatform.MSBuildLogger";

    /// <summary>
    /// Gets instance of IOutput used for sending output.
    /// </summary>
    /// <remarks>Protected so this can be detoured for testing purposes.</remarks>
    protected static IOutput? Output
    {
        get;
        private set;
    }

    // Default constructor is needed for a logger to be able to activate it.
    public MSBuildLogger() { }

    /// <summary>
    /// Constructor added for testing purpose
    /// </summary>
    internal MSBuildLogger(IOutput output)
    {
        Output = output;
    }

    [MemberNotNull(nameof(Output))]
    public void Initialize(TestLoggerEvents events, string testRunDirectory)
    {
        ValidateArg.NotNull(events, nameof(events));

        Output ??= ConsoleOutput.Instance;

        // Register for the events.
        // events.TestRunMessage += TestMessageHandler;
        events.TestResult += TestResultHandler;
        events.TestRunComplete += TestRunCompleteHandler;
        // events.TestRunStart += TestRunStartHandler;

        // Register for the discovery events.
        // events.DiscoveryMessage += TestMessageHandler;
    }

    public void Initialize(TestLoggerEvents events, Dictionary<string, string?> parameters)
    {
        Initialize(events, string.Empty);
    }

    private void TestRunCompleteHandler(object? sender, TestRunCompleteEventArgs e)
    {
        TPDebug.Assert(Output != null, "Initialize should have been called");

        if (e.IsCanceled)
        {
            Output.Error(false, CommandLineResources.TestRunCanceled);
        }
        else if (e.IsAborted)
        {
            if (e.Error == null)
            {
                Output.Error(false, CommandLineResources.TestRunAborted);
            }
            else
            {
                Output.Error(false, CommandLineResources.TestRunAbortedWithError, e.Error);
            }
        }
    }

    private void TestResultHandler(object? sender, TestResultEventArgs e)
    {
        ValidateArg.NotNull(sender, nameof(sender));
        ValidateArg.NotNull(e, nameof(e));
        TPDebug.Assert(Output != null, "Initialize should have been called.");
        switch (e.Result.Outcome)
        {
            case TestOutcome.Failed:
                {
                    var result = e.Result;
                    if (!StringUtils.IsNullOrWhiteSpace(result.ErrorStackTrace))
                    {
                        var error = result.ErrorMessage == null ? null : Regex.Split(result.ErrorMessage, Environment.NewLine)[0];
                        string? stackFrame = null;
                        var stackFrames = Regex.Split(result.ErrorStackTrace, Environment.NewLine);
                        if (stackFrames.Length > 0)
                        {
                            stackFrame = stackFrames[0];
                        }
                        if (stackFrame != null)
                        {
                            // stack frame looks like this '   at Program.<Main>$(String[] args) in S:\t\ConsoleApp81\ConsoleApp81\Program.cs:line 9'
                            var match = Regex.Match(stackFrame, @"^\s+at (?<code>.+) in (?<file>.+):line (?<line>\d+)$?");

                            string? line;
                            string? file;
                            string? code;
                            if (match.Success)
                            {
                                // get the exact info from stack frame.
                                code = match.Groups["code"].Value;
                                file = match.Groups["file"].Value;
                                line = match.Groups["line"].Value;
                            }
                            else
                            {
                                // if there are no symbols but we collect source info, us the source info.
                                file = result.TestCase.CodeFilePath;
                                line = result.TestCase.LineNumber > 0 ? result.TestCase.LineNumber.ToString(CultureInfo.InvariantCulture) : null;
                                code = stackFrame;
                            }

                            var message = $"||||{ReplaceSeparator(file)}||||{line}||||{ReplaceSeparator(code)}||||{ReplaceSeparator(error)}";

                            Output.Error(false, message);
                            return;
                        }
                    }
                    else
                    {
                        Output.Error(false, result.DisplayName?.Replace(Environment.NewLine, " ") ?? string.Empty);
                    }


                    break;
                }
        }
    }

    private static string? ReplaceSeparator(string? text)
    {
        if (text == null)
        {
            return null;
        }

        return text.Replace("||||", "____");
    }

}
