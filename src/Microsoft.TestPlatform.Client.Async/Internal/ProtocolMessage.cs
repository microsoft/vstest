// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.TestPlatform.Client.Async.Internal;

/// <summary>
/// Represents a versioned protocol message sent over the vstest socket connection.
/// Format: {"MessageType":"...","Version":N,"Payload":...}
/// </summary>
internal sealed class ProtocolMessage
{
    [JsonPropertyName("MessageType")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("Version")]
    public int Version { get; set; }

    [JsonPropertyName("Payload")]
    public JsonElement? Payload { get; set; }
}

/// <summary>
/// Payload sent for discovery requests.
/// </summary>
internal sealed class DiscoveryRequestPayloadDto
{
    [JsonPropertyName("Sources")]
    public List<string> Sources { get; set; } = new();

    [JsonPropertyName("RunSettings")]
    public string? RunSettings { get; set; }

    [JsonPropertyName("TestPlatformOptions")]
    public object? TestPlatformOptions { get; set; }

    [JsonPropertyName("TestSessionInfo")]
    public object? TestSessionInfo { get; set; }
}

/// <summary>
/// Payload sent for test run requests.
/// </summary>
internal sealed class TestRunRequestPayloadDto
{
    [JsonPropertyName("Sources")]
    public List<string>? Sources { get; set; }

    [JsonPropertyName("TestCases")]
    public List<TestCaseDto>? TestCases { get; set; }

    [JsonPropertyName("RunSettings")]
    public string? RunSettings { get; set; }

    [JsonPropertyName("KeepAlive")]
    public bool KeepAlive { get; set; }

    [JsonPropertyName("DebuggingEnabled")]
    public bool DebuggingEnabled { get; set; }

    [JsonPropertyName("TestPlatformOptions")]
    public object? TestPlatformOptions { get; set; }

    [JsonPropertyName("TestSessionInfo")]
    public object? TestSessionInfo { get; set; }
}

/// <summary>
/// Lightweight TestCase DTO for serialization over the wire.
/// Uses the vstest property-bag format: { "Properties": [ { "Key": {...}, "Value": ... }, ... ] }
/// </summary>
internal sealed class TestCaseDto
{
    [JsonPropertyName("Properties")]
    public List<TestPropertyPair> Properties { get; set; } = new();

    public static TestCaseDto FromTestCase(TestCase testCase)
    {
        var dto = new TestCaseDto();

        dto.AddProperty("TestCase.Id", "Id", typeof(Guid), TestPropertyAttributes.Hidden, testCase.Id.ToString());
        dto.AddProperty("TestCase.FullyQualifiedName", "FullyQualifiedName", typeof(string), TestPropertyAttributes.Hidden, testCase.FullyQualifiedName);
        dto.AddProperty("TestCase.ExecutorUri", "Executor Uri", typeof(Uri), TestPropertyAttributes.Hidden, testCase.ExecutorUri.OriginalString);
        dto.AddProperty("TestCase.Source", "Source", typeof(string), TestPropertyAttributes.None, testCase.Source);
        dto.AddProperty("TestCase.DisplayName", "Name", typeof(string), TestPropertyAttributes.None, testCase.DisplayName);

        if (testCase.CodeFilePath != null)
        {
            dto.AddProperty("TestCase.CodeFilePath", "File Path", typeof(string), TestPropertyAttributes.None, testCase.CodeFilePath);
        }

        if (testCase.LineNumber >= 0)
        {
            dto.AddProperty("TestCase.LineNumber", "Line Number", typeof(int), TestPropertyAttributes.Hidden, testCase.LineNumber);
        }

        return dto;
    }

    private void AddProperty(string id, string label, Type valueType, TestPropertyAttributes attributes, object value)
    {
        Properties.Add(new TestPropertyPair
        {
            Key = new TestPropertyKey
            {
                Id = id,
                Label = label,
                Category = string.Empty,
                Description = string.Empty,
                Attributes = (int)attributes,
                ValueType = valueType.FullName!,
            },
            Value = JsonSerializer.SerializeToElement(value),
        });
    }
}

/// <summary>
/// A single property in the vstest property-bag format.
/// </summary>
internal sealed class TestPropertyPair
{
    [JsonPropertyName("Key")]
    public TestPropertyKey Key { get; set; } = new();

    [JsonPropertyName("Value")]
    public JsonElement Value { get; set; }
}

/// <summary>
/// The key portion of a property-bag entry.
/// </summary>
internal sealed class TestPropertyKey
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("Attributes")]
    public int Attributes { get; set; }

    [JsonPropertyName("ValueType")]
    public string ValueType { get; set; } = string.Empty;
}

/// <summary>
/// Handles deserialization of TestCase and TestResult from the vstest property-bag wire format.
/// </summary>
internal static class TestObjectDeserializer
{
    public static TestCase DeserializeTestCase(JsonElement element)
    {
        string? fullyQualifiedName = null;
        string? executorUri = null;
        string? source = null;
        string? displayName = null;
        string? id = null;
        string? codeFilePath = null;
        int lineNumber = -1;
        var customProperties = new List<(TestPropertyKey key, JsonElement value)>();

        if (element.TryGetProperty("Properties", out var propertiesElement))
        {
            foreach (var prop in propertiesElement.EnumerateArray())
            {
                if (!prop.TryGetProperty("Key", out var keyElement) ||
                    !prop.TryGetProperty("Value", out var valueElement))
                {
                    continue;
                }

                string propId = keyElement.GetProperty("Id").GetString() ?? string.Empty;

                switch (propId)
                {
                    case "TestCase.FullyQualifiedName":
                        fullyQualifiedName = GetStringValue(valueElement);
                        break;
                    case "TestCase.ExecutorUri":
                        executorUri = GetStringValue(valueElement);
                        break;
                    case "TestCase.Source":
                        source = GetStringValue(valueElement);
                        break;
                    case "TestCase.DisplayName":
                        displayName = GetStringValue(valueElement);
                        break;
                    case "TestCase.Id":
                        id = GetStringValue(valueElement);
                        break;
                    case "TestCase.CodeFilePath":
                        codeFilePath = GetStringValue(valueElement);
                        break;
                    case "TestCase.LineNumber":
                        lineNumber = GetIntValue(valueElement);
                        break;
                    default:
                        var key = JsonSerializer.Deserialize<TestPropertyKey>(keyElement.GetRawText())!;
                        customProperties.Add((key, valueElement));
                        break;
                }
            }
        }

        var testCase = new TestCase(
            fullyQualifiedName ?? "Unknown",
            new Uri(executorUri ?? "executor://unknown"),
            source ?? "unknown");

        if (displayName != null) testCase.DisplayName = displayName;
        if (id != null) testCase.Id = Guid.Parse(id);
        if (codeFilePath != null) testCase.CodeFilePath = codeFilePath;
        if (lineNumber >= 0) testCase.LineNumber = lineNumber;

        foreach (var (key, value) in customProperties)
        {
            var testProperty = TestProperty.Register(
                key.Id,
                key.Label,
                key.Category,
                key.Description,
                GetTypeFromName(key.ValueType),
                null,
                (TestPropertyAttributes)key.Attributes,
                typeof(TestObject));
            testCase.SetPropertyValue(testProperty, GetStringValue(value));
        }

        return testCase;
    }

    public static TestResult DeserializeTestResult(JsonElement element)
    {
        TestCase testCase;

        if (element.TryGetProperty("TestCase", out var testCaseElement))
        {
            testCase = DeserializeTestCase(testCaseElement);
        }
        else
        {
            testCase = new TestCase("Unknown", new Uri("executor://unknown"), "unknown");
        }

        var result = new TestResult(testCase);

        if (element.TryGetProperty("Properties", out var propertiesElement))
        {
            foreach (var prop in propertiesElement.EnumerateArray())
            {
                if (!prop.TryGetProperty("Key", out var keyElement) ||
                    !prop.TryGetProperty("Value", out var valueElement))
                {
                    continue;
                }

                string propId = keyElement.GetProperty("Id").GetString() ?? string.Empty;

                switch (propId)
                {
                    case "TestResult.Outcome":
                        result.Outcome = (TestOutcome)GetIntValue(valueElement);
                        break;
                    case "TestResult.ErrorMessage":
                        result.ErrorMessage = GetStringValue(valueElement);
                        break;
                    case "TestResult.ErrorStackTrace":
                        result.ErrorStackTrace = GetStringValue(valueElement);
                        break;
                    case "TestResult.DisplayName":
                        result.DisplayName = GetStringValue(valueElement);
                        break;
                    case "TestResult.Duration":
                        var durationStr = GetStringValue(valueElement);
                        if (durationStr != null && TimeSpan.TryParse(durationStr, CultureInfo.InvariantCulture, out var duration))
                        {
                            result.Duration = duration;
                        }
                        break;
                    case "TestResult.StartTime":
                        var startStr = GetStringValue(valueElement);
                        if (startStr != null && DateTimeOffset.TryParse(startStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
                        {
                            result.StartTime = startTime;
                        }
                        break;
                    case "TestResult.EndTime":
                        var endStr = GetStringValue(valueElement);
                        if (endStr != null && DateTimeOffset.TryParse(endStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime))
                        {
                            result.EndTime = endTime;
                        }
                        break;
                    case "TestResult.ComputerName":
                        result.ComputerName = GetStringValue(valueElement) ?? string.Empty;
                        break;
                }
            }
        }

        return result;
    }

    private static string? GetStringValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            _ => element.GetRawText().Trim('"'),
        };
    }

    private static int GetIntValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String => int.TryParse(element.GetString(), out var v) ? v : 0,
            _ => 0,
        };
    }

    private static Type GetTypeFromName(string typeName)
    {
        return typeName switch
        {
            "System.String" => typeof(string),
            "System.Int32" => typeof(int),
            "System.Boolean" => typeof(bool),
            "System.Guid" => typeof(Guid),
            "System.Uri" => typeof(Uri),
            "System.TimeSpan" => typeof(TimeSpan),
            "System.DateTimeOffset" => typeof(DateTimeOffset),
            _ => typeof(string),
        };
    }
}
