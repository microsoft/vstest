// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETCOREAPP
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
#endif

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests;

/// <summary>
/// Regression tests for https://github.com/microsoft/vstest/issues/16512: a .NET Framework testhost
/// escapes every surrogate code unit on its own, so a display name truncated in the middle of an
/// astral character arrives as a lone <c>\uD83D</c> escape and used to abort the whole run.
/// </summary>
[TestClass]
public class JsonDataSerializerSurrogateRegressionTests
{
#if NETCOREAPP
    // System.Text.Json cannot represent the unpaired code unit, it is replaced with U+FFFD.
    private const string ExpectedDisplayName = "a\uFFFD";
#else
    // Jsonite reads the escape as-is and keeps the unpaired code unit.
    private const string ExpectedDisplayName = "a\uD83D";
#endif

    private readonly JsonDataSerializer _jsonDataSerializer = JsonDataSerializer.Instance;

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(6)]
    public void DeserializePayloadShouldNotFailOnUnpairedSurrogateEscape(int version)
    {
        var rawMessage = CreateTestCaseMessageWithDisplayName(@"a\uD83D", version);

        var message = _jsonDataSerializer.DeserializeMessage(rawMessage);
        var testCases = _jsonDataSerializer.DeserializePayload<TestCase[]>(message)!;

        Assert.HasCount(1, testCases);
        Assert.AreEqual(ExpectedDisplayName, testCases[0].DisplayName);
        Assert.AreEqual("x.y.z", testCases[0].FullyQualifiedName);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(6)]
    public void DeserializePayloadShouldKeepCompleteSurrogatePairs(int version)
    {
        var rawMessage = CreateTestCaseMessageWithDisplayName(@"a\uD83D\uDE00", version);

        var message = _jsonDataSerializer.DeserializeMessage(rawMessage);
        var testCases = _jsonDataSerializer.DeserializePayload<TestCase[]>(message)!;

        Assert.AreEqual("a\uD83D\uDE00", testCases[0].DisplayName);
    }

    /// <summary>
    /// Builds the message the way an older .NET Framework testhost sends it, by serializing a
    /// placeholder name and swapping it for the raw escape sequence afterwards.
    /// </summary>
    private string CreateTestCaseMessageWithDisplayName(string escapedDisplayName, int version)
    {
        var testCase = new TestCase("x.y.z", new System.Uri("uri://dummy"), "x.dll")
        {
            DisplayName = "PLACEHOLDER",
        };

        var rawMessage = _jsonDataSerializer.SerializePayload(MessageType.TestCasesFound, new[] { testCase }, version);
        Assert.Contains("PLACEHOLDER", rawMessage, "Test setup is broken, the display name was not serialized.");

        return rawMessage.Replace("PLACEHOLDER", escapedDisplayName);
    }

    [TestMethod]
    public void DeserializeShouldNotFailOnUnpairedSurrogateEscape()
    {
        var testCase = new TestCase("x.y.z", new System.Uri("uri://dummy"), "x.dll")
        {
            DisplayName = "PLACEHOLDER",
        };
        var json = _jsonDataSerializer.Serialize(testCase, 2).Replace("PLACEHOLDER", @"a\uD83D");

        var deserialized = _jsonDataSerializer.Deserialize<TestCase>(json, 2)!;

        Assert.AreEqual(ExpectedDisplayName, deserialized.DisplayName);
    }

    [TestMethod]
    public void DeserializeMessageShouldNotFailWhenTheHeaderIsParsedFromJson()
    {
        // Payload comes first, which defeats the string based fast header parse and forces the
        // full JSON parse in ParseHeaderFromJson.
        var rawMessage = @"{""Payload"":null,""Version"":6,""MessageType"":""TestDiscovery.TestFound_a\uD83D""}";

        var message = _jsonDataSerializer.DeserializeMessage(rawMessage);

        Assert.AreEqual(6, message.Version);
        Assert.AreEqual("TestDiscovery.TestFound_" + ExpectedDisplayName, message.MessageType);
    }

#if !NETCOREAPP
    [TestMethod]
    // The display name is spelled as a recipe because a custom attribute blob is UTF-8 and cannot
    // carry an unpaired surrogate: 'H' becomes U+D83D, 'L' becomes U+DE00, anything else is literal.
    [DataRow("aH", @"""a\uFFFD""")]
    [DataRow("aHb", @"""a\uFFFDb""")]
    [DataRow("aLb", @"""a\uFFFDb""")]
    [DataRow("aHHb", @"""a\uFFFD\uFFFDb""")]
    [DataRow("aLHb", @"""a\uFFFD\uFFFDb""")]
    [DataRow("aHLb", @"""a\uD83D\uDE00b""")]
    [DataRow("aHL", @"""a\uD83D\uDE00""")]
    public void JsoniteShouldNotEmitUnpairedSurrogateEscapes(string recipe, string expectedJsonValue)
    {
        var testCase = new TestCase("x.y.z", new System.Uri("uri://dummy"), "x.dll")
        {
            DisplayName = BuildSurrogateString(recipe),
        };

        var rawMessage = _jsonDataSerializer.SerializePayload(MessageType.TestCasesFound, new[] { testCase }, 6);

        // The full quoted value is asserted so that a dropped or duplicated code unit is caught.
        Assert.Contains(expectedJsonValue, rawMessage);
    }

    private static string BuildSurrogateString(string recipe)
    {
        var chars = recipe.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] switch
            {
                'H' => (char)0xD83D,
                'L' => (char)0xDE00,
                _ => chars[i],
            };
        }

        return new string(chars);
    }
#endif

#if NETCOREAPP
    [TestMethod]
    [DataRow(@"{""Name"":""plain""}")]
    [DataRow(@"{""Name"":""tab\tand\""quote""}")]
    [DataRow(@"{""Name"":""\u0041\u00E9""}")]
    [DataRow(@"{""Name"":""a\uD83D\uDE00b""}")]
    [DataRow(@"{""Name"":""a\ud83d\ude00b""}")]
    // The value here is the literal text a\uD83D, the backslash itself is escaped.
    [DataRow(@"{""Name"":""a\\uD83D""}")]
    [DataRow(@"{""Name"":""ends with a backslash escape\\""}")]
    [DataRow(@"{""Name"":""truncated escape \u12""}")]
    public void ReplaceUnpairedShouldNotChangeValidJson(string json)
    {
        Assert.AreSame(json, JsonSurrogates.ReplaceUnpaired(json));
    }

    [TestMethod]
    [DataRow(@"{""Name"":""a\uD83D""}", @"{""Name"":""a\uFFFD""}")]
    [DataRow(@"{""Name"":""a\ud83d""}", @"{""Name"":""a\uFFFD""}")]
    [DataRow(@"{""Name"":""a\uDE00""}", @"{""Name"":""a\uFFFD""}")]
    [DataRow(@"{""Name"":""a\uD83D\uD83D""}", @"{""Name"":""a\uFFFD\uFFFD""}")]
    [DataRow(@"{""Name"":""a\uDE00\uD83D""}", @"{""Name"":""a\uFFFD\uFFFD""}")]
    [DataRow(@"{""Name"":""a\uD83D\uDE00\uD83D""}", @"{""Name"":""a\uD83D\uDE00\uFFFD""}")]
    [DataRow(@"{""Name"":""a\\\uD83D""}", @"{""Name"":""a\\\uFFFD""}")]
    [DataRow(@"{""Name"":""\uD83D""}", @"{""Name"":""\uFFFD""}")]
    public void ReplaceUnpairedShouldReplaceUnpairedSurrogateEscapes(string json, string expected)
    {
        Assert.AreEqual(expected, JsonSurrogates.ReplaceUnpaired(json));
    }

    [TestMethod]
    public void ReplaceUnpairedShouldHandleEscapeTruncatedByEndOfString()
    {
        // A high surrogate escape that is not followed by anything at all.
        Assert.AreEqual(@"\uFFFD", JsonSurrogates.ReplaceUnpaired(@"\uD83D"));
    }
#endif
}
