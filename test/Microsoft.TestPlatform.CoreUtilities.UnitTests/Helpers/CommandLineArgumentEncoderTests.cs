// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.CoreUtilities.UnitTests;

[TestClass]
public sealed class CommandLineArgumentEncoderTests
{
    [TestMethod]
    [DataRow("", "\"\"")]
    [DataRow("log with spaces.txt", "\"log with spaces.txt\"")]
    [DataRow(@"C:\logs\", "\"C:\\logs\\\\\"")]
    [DataRow(@"C:\", "\"C:\\\\\"")]
    [DataRow(@"log\\", "\"log\\\\\\\\\"")]
    [DataRow("log\"one\".txt", "\"log\\\"one\\\".txt\"")]
    [DataRow("log\\\"one\".txt", "\"log\\\\\\\"one\\\".txt\"")]
    [DataRow("\"log\"", "\"\\\"log\\\"\"")]
    [DataRow("log\\\\\"\\", "\"log\\\\\\\\\\\"\\\\\"")]
    public void EncodePreservesOneArgument(string argument, string expected)
    {
        Assert.AreEqual(expected, CommandLineArgumentEncoder.Encode(argument));
    }

    [TestMethod]
    public void AddDoubleQuoteStillWrapsPrequotedMultipleSources()
    {
        Assert.AreEqual("\"first.dll\" \"second.dll\"", "first.dll\" \"second.dll".AddDoubleQuote());
    }
}
