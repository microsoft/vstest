// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace vstest.console.UnitTests.Internal;

/// <summary>
/// Round-trip tests verifying that MSBuildLogger's encoder and VSTestTask2's decoder stay in sync.
/// </summary>
[TestClass]
public class MSBuildLoggerEncoderTests
{
    // Mirrors VSTestTask2.TryGetMessage decode logic to keep the round-trip self-contained.
    // WARNING: This must stay in sync with the actual decode logic in VSTestTask2.TryGetMessage.
    // If VSTestTask2's decoder changes (e.g., different control chars), update this helper too,
    // otherwise these round-trip tests will pass while real round-trips break.
    private static string?[] Decode(string encoded)
    {
        var parts = encoded.Split(new[] { "||||" }, StringSplitOptions.None);
        // parts[0] is empty (before first ||||), parts[1] is the event name
        return parts.Skip(2).Select(p => p?.Replace("\x02", "\r").Replace("\x03", "\n")).ToArray();
    }

    [TestMethod]
    public void FormatMessage_TildeCharsInMessage_SurviveRoundTrip()
    {
        // Regression test for #15268 — 5 tildes must not be corrupted by the old ~~~~ → ____ sanitization
        var rawMessage = new string('~', 5);

        var encoded = MSBuildLogger.FormatMessage("test-failed", rawMessage, "TestFile.cs", "42");
        var decoded = Decode(encoded);

        Assert.AreEqual(rawMessage, decoded[0]);
    }

    [TestMethod]
    public void FormatMessage_ExclamationCharsInMessage_SurviveRoundTrip()
    {
        // Regression test for #15268 — 4 exclamation marks must not be corrupted by the old !!!! → ____ sanitization
        var rawMessage = new string('!', 4);

        var encoded = MSBuildLogger.FormatMessage("test-failed", rawMessage, "TestFile.cs", "42");
        var decoded = Decode(encoded);

        Assert.AreEqual(rawMessage, decoded[0]);
    }

    [TestMethod]
    public void FormatMessage_NewlinesInMessage_SurviveRoundTrip()
    {
        // Regression test for #15268 — \r\n must survive encode/decode via \x02/\x03 control chars
        var rawMessage = "Assert failed.\r\n  at MyTest()";

        var encoded = MSBuildLogger.FormatMessage("test-failed", rawMessage, "TestFile.cs", "42");
        var decoded = Decode(encoded);

        Assert.AreEqual(rawMessage, decoded[0]);
    }

    [TestMethod]
    public void FormatMessage_MultipleFields_AllFieldsSurviveRoundTrip()
    {
        // Verify that all fields (message, file, line) are preserved intact across encode/decode
        var rawMessage = "Assert failed.\r\n  at MyTest()";
        var rawFile = "TestFile.cs";
        var rawLine = "42";

        var encoded = MSBuildLogger.FormatMessage("test-failed", rawMessage, rawFile, rawLine);
        var decoded = Decode(encoded);

        Assert.HasCount(3, decoded);
        Assert.AreEqual(rawMessage, decoded[0]);
        Assert.AreEqual(rawFile, decoded[1]);
        Assert.AreEqual(rawLine, decoded[2]);
    }
}
