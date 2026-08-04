// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// Converts the Microsoft.Testing.Platform (MTP) <c>dotnettestcli</c> wire messages
/// (<see cref="DiscoveredTestMessage"/>, <see cref="SuccessfulTestResultMessage"/>,
/// <see cref="FailedTestResultMessage"/>) into vstest ObjectModel <see cref="TestCase"/> and
/// <see cref="TestResult"/> instances.
/// </summary>
internal static class MtpMessageConverter
{
    /// <summary>
    /// Property used to round-trip the MTP node uid on a vstest <see cref="TestCase"/> so a discovered
    /// test can later be run by uid (via <c>--filter-uid</c>).
    /// </summary>
    internal static readonly TestProperty MtpUidProperty = TestProperty.Register(
        MtpConstants.MtpUidPropertyId,
        "MTP Uid",
        typeof(string),
        typeof(TestCase));

    private static readonly Uri ExecutorUri = new(MtpConstants.DefaultExecutorUri);

    /// <summary>
    /// Builds a rich <see cref="TestCase"/> from a discovery message (namespace/type/method, file/line
    /// and traits are available at discovery time).
    /// </summary>
    public static TestCase ToTestCase(DiscoveredTestMessage message, string source)
    {
        string uid = message.Uid;
        string fullyQualifiedName = BuildFullyQualifiedName(message)
            ?? (string.IsNullOrEmpty(message.DisplayName) ? uid : message.DisplayName);

        var testCase = new TestCase(fullyQualifiedName, ExecutorUri, source)
        {
            DisplayName = string.IsNullOrEmpty(message.DisplayName) ? fullyQualifiedName : message.DisplayName,
        };

        testCase.SetPropertyValue(MtpUidProperty, uid);

        if (!string.IsNullOrEmpty(message.FilePath))
        {
            testCase.CodeFilePath = message.FilePath;
            if (message.LineNumber is { } line)
            {
                testCase.LineNumber = line;
            }
        }

        foreach (TestMetadataProperty trait in message.Traits)
        {
            if (!string.IsNullOrEmpty(trait.Key))
            {
                testCase.Traits.Add(new Trait(trait.Key!, trait.Value ?? string.Empty));
            }
        }

        return testCase;
    }

    /// <summary>
    /// Builds a <see cref="TestCase"/> from the identity carried on a result message. Result messages
    /// only carry the uid and display name, so the case is thinner than a discovered one. It is built
    /// deterministically from the uid/display name so the started and terminal notifications for the
    /// same test produce the same <see cref="TestCase.Id"/>.
    /// </summary>
    public static TestCase ToTestCase(string uid, string? displayName, string source)
    {
        string fullyQualifiedName = string.IsNullOrEmpty(displayName) ? uid : displayName!;

        var testCase = new TestCase(fullyQualifiedName, ExecutorUri, source)
        {
            DisplayName = fullyQualifiedName,
        };

        testCase.SetPropertyValue(MtpUidProperty, uid);
        return testCase;
    }

    public static TestResult ToTestResult(SuccessfulTestResultMessage message, string source)
    {
        var testCase = ToTestCase(message.Uid ?? Guid.NewGuid().ToString(), message.DisplayName, source);

        var result = new TestResult(testCase)
        {
            Outcome = ToOutcome(message.State),
            DisplayName = testCase.DisplayName,
            ErrorMessage = message.Reason,
            Duration = ToDuration(message.Duration),
        };

        AddOutput(result, message.StandardOutput, message.ErrorOutput);
        return result;
    }

    public static TestResult ToTestResult(FailedTestResultMessage message, string source)
    {
        var testCase = ToTestCase(message.Uid ?? Guid.NewGuid().ToString(), message.DisplayName, source);

        var result = new TestResult(testCase)
        {
            Outcome = ToOutcome(message.State),
            DisplayName = testCase.DisplayName,
            Duration = ToDuration(message.Duration),
        };

        (result.ErrorMessage, result.ErrorStackTrace) = Flatten(message.Exceptions, message.Reason);

        AddOutput(result, message.StandardOutput, message.ErrorOutput);
        return result;
    }

    /// <summary>
    /// Returns <see langword="true"/> for the non-terminal "in progress" state, which is surfaced as a
    /// test-case start rather than a completed result.
    /// </summary>
    public static bool IsInProgress(byte? state) => state == TestStates.InProgress;

    private static void AddOutput(TestResult result, string? standardOutput, string? errorOutput)
    {
        if (!string.IsNullOrEmpty(standardOutput))
        {
            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, standardOutput));
        }

        if (!string.IsNullOrEmpty(errorOutput))
        {
            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, errorOutput));
        }
    }

    private static TimeSpan ToDuration(long? durationInTicks)
        => durationInTicks.HasValue ? TimeSpan.FromTicks(durationInTicks.Value) : TimeSpan.Zero;

    private static TestOutcome ToOutcome(byte? state)
        => state switch
        {
            TestStates.Passed => TestOutcome.Passed,
            TestStates.Skipped => TestOutcome.Skipped,
            TestStates.Failed => TestOutcome.Failed,
            TestStates.Error => TestOutcome.Failed,
            TestStates.Timeout => TestOutcome.Failed,
            TestStates.Cancelled => TestOutcome.None,
            _ => TestOutcome.None,
        };

    private static (string? ErrorMessage, string? StackTrace) Flatten(ExceptionMessage[]? exceptions, string? reason)
    {
        if (exceptions is null || exceptions.Length == 0)
        {
            return (reason, null);
        }

        if (exceptions.Length == 1)
        {
            ExceptionMessage single = exceptions[0];
            return (string.IsNullOrEmpty(single.ErrorMessage) ? reason : single.ErrorMessage, single.StackTrace);
        }

        var messageBuilder = new StringBuilder();
        var stackBuilder = new StringBuilder();
        foreach (ExceptionMessage exception in exceptions)
        {
            if (!string.IsNullOrEmpty(exception.ErrorMessage))
            {
                if (messageBuilder.Length > 0)
                {
                    messageBuilder.AppendLine();
                }

                messageBuilder.Append(exception.ErrorMessage);
            }

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                if (stackBuilder.Length > 0)
                {
                    stackBuilder.AppendLine();
                }

                stackBuilder.Append(exception.StackTrace);
            }
        }

        return (
            messageBuilder.Length > 0 ? messageBuilder.ToString() : reason,
            stackBuilder.Length > 0 ? stackBuilder.ToString() : null);
    }

    private static string? BuildFullyQualifiedName(DiscoveredTestMessage message)
    {
        if (string.IsNullOrEmpty(message.MethodName))
        {
            return null;
        }

        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(message.Namespace))
        {
            builder.Append(message.Namespace).Append('.');
        }

        if (!string.IsNullOrEmpty(message.TypeName))
        {
            builder.Append(message.TypeName).Append('.');
        }

        builder.Append(message.MethodName);

        if (message.ParameterTypeFullNames is { Length: > 0 } parameters)
        {
            builder.Append('(').Append(string.Join(",", parameters)).Append(')');
        }

        return builder.ToString();
    }
}
