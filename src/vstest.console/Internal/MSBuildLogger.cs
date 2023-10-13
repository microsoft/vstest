// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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
    private const string TestMessageFormattingPrefix = " ";
    private const string TestResultPrefix = "  ";

    public const string ExtensionUri = "logger://Microsoft/TestPlatform/MSBuildLogger/v1";

    // This name is not so friendly on purpose, because MSBuild seems like a name that someone might have
    // already claimed, and we will use this just programmatically.
    public const string FriendlyName = "Microsoft.TestPlatform.MSBuildLogger";

    public const string VerbosityParam = "verbosity";

    public Verbosity VerbosityLevel { get; private set; } = Verbosity.Minimal;

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
        ValidateArg.NotNull(parameters, nameof(parameters));

        if (parameters.Count == 0)
        {
            throw new ArgumentException("No default parameters added", nameof(parameters));
        }

        var verbosityExists = parameters.TryGetValue(VerbosityParam, out var verbosity);
        if (verbosityExists && Enum.TryParse(verbosity, true, out Verbosity verbosityLevel))
        {
            // We don't use this anywhere for now.
            VerbosityLevel = verbosityLevel;
        }

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
                    var stringBuilder = new StringBuilder();
                    var testDisplayName = e.Result.DisplayName;

                    if (e.Result.DisplayName.IsNullOrWhiteSpace())
                    {
                        testDisplayName = e.Result.TestCase.DisplayName;
                    }

                    stringBuilder.Append(testDisplayName ?? "<unknown>").Append(": ");
                    AppendFullError(e.Result, stringBuilder);
                    Output.Error(false, stringBuilder.ToString());
                    break;
                }
        }
    }

    private static void AppendFullError(TestResult result, StringBuilder stringBuilder)
    {
        TPDebug.Assert(result != null, "a null result can not be displayed");
        TPDebug.Assert(Output != null, "Initialize should have been called.");

        if (!result.ErrorMessage.IsNullOrEmpty())
        {
            stringBuilder.Append(result.ErrorMessage);
        }

        stringBuilder.AppendLine();

        if (!result.ErrorStackTrace.IsNullOrEmpty())
        {
            stringBuilder.AppendLine(CommandLineResources.StacktraceBanner);
            stringBuilder.AppendLine(result.ErrorStackTrace);
        }
    }

    internal enum Verbosity
    {
        Quiet,
        Minimal,
        Normal,
        Detailed
    }
}
