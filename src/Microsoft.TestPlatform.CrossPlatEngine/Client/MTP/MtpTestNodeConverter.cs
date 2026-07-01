// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System;
using System.Text.Json;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// Converts Microsoft.Testing.Platform (MTP) test nodes into vstest ObjectModel
/// <see cref="TestCase"/> and <see cref="TestResult"/> instances.
///
/// The converter works purely off the MTP node shape (<c>uid</c>, <c>display-name</c>,
/// <c>execution-state</c>, <c>time.duration-ms</c>, <c>error.*</c>, <c>location.*</c>, <c>traits</c>)
/// so that an MTP application with no dependency on vstest at all still converts correctly. When the
/// optional <c>vstest.*</c> bridge properties are present they are used purely as enrichment.
/// </summary>
internal static class MtpTestNodeConverter
{
    internal static readonly TestProperty MtpUidProperty = TestProperty.Register(
        MtpConstants.MtpUidPropertyId,
        "MTP Uid",
        typeof(string),
        typeof(TestCase));

    /// <summary>
    /// Returns true when the node represents a runnable test (a leaf "action" node) rather than a
    /// grouping node (namespace/class/suite).
    /// </summary>
    public static bool IsActionNode(JsonElement node)
        => GetString(node, MtpConstants.NodeType) is "action";

    public static string? GetExecutionState(JsonElement node)
        => GetString(node, MtpConstants.ExecutionState);

    public static TestCase ToTestCase(JsonElement node, string source)
    {
        string uid = GetString(node, MtpConstants.Uid) ?? Guid.NewGuid().ToString();
        string fullyQualifiedName = GetString(node, MtpConstants.VsTestFullyQualifiedName) ?? uid;
        string executorUri = GetString(node, MtpConstants.VsTestExecutorUri) ?? MtpConstants.DefaultExecutorUri;

        var testCase = new TestCase(fullyQualifiedName, new Uri(executorUri), source)
        {
            DisplayName = GetString(node, MtpConstants.DisplayName) ?? fullyQualifiedName,
        };

        testCase.SetPropertyValue(MtpUidProperty, uid);

        string? file = GetString(node, MtpConstants.LocationFile);
        if (!string.IsNullOrEmpty(file))
        {
            testCase.CodeFilePath = file;
            if (TryGetInt(node, MtpConstants.LocationLineStart, out int line))
            {
                testCase.LineNumber = line;
            }
        }

        AddTraits(node, testCase);
        return testCase;
    }

    public static TestResult ToTestResult(JsonElement node, string source)
    {
        var testCase = ToTestCase(node, source);
        string? state = GetExecutionState(node);

        var result = new TestResult(testCase)
        {
            Outcome = ToOutcome(state),
            DisplayName = testCase.DisplayName,
            ErrorMessage = GetString(node, MtpConstants.ErrorMessage),
            ErrorStackTrace = GetString(node, MtpConstants.ErrorStackTrace),
        };

        if (TryGetDouble(node, MtpConstants.TimeDurationMs, out double durationMs))
        {
            result.Duration = TimeSpan.FromMilliseconds(durationMs);
        }

        return result;
    }

    public static bool IsTerminalState(string? state)
        => state is MtpConstants.StatePassed
            or MtpConstants.StateFailed
            or MtpConstants.StateSkipped
            or MtpConstants.StateError
            or MtpConstants.StateTimedOut;

    private static TestOutcome ToOutcome(string? state)
        => state switch
        {
            MtpConstants.StatePassed => TestOutcome.Passed,
            MtpConstants.StateFailed => TestOutcome.Failed,
            MtpConstants.StateError => TestOutcome.Failed,
            MtpConstants.StateTimedOut => TestOutcome.Failed,
            MtpConstants.StateSkipped => TestOutcome.Skipped,
            _ => TestOutcome.None,
        };

    private static void AddTraits(JsonElement node, TestCase testCase)
    {
        if (!node.TryGetProperty(MtpConstants.Traits, out JsonElement traits) || traits.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement trait in traits.EnumerateArray())
        {
            if (trait.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (JsonProperty property in trait.EnumerateObject())
            {
                string value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : string.Empty;
                testCase.Traits.Add(new Trait(property.Name, value));
            }
        }
    }

    private static string? GetString(JsonElement node, string key)
        => node.ValueKind == JsonValueKind.Object && node.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryGetInt(JsonElement node, string key, out int result)
    {
        if (node.ValueKind == JsonValueKind.Object && node.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out result))
        {
            return true;
        }

        result = 0;
        return false;
    }

    private static bool TryGetDouble(JsonElement node, string key, out double result)
    {
        if (node.ValueKind == JsonValueKind.Object && node.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out result))
        {
            return true;
        }

        result = 0;
        return false;
    }
}

#endif
