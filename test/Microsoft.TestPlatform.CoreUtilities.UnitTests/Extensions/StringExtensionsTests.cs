// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Extensions;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    [DataRow("", "\"\"")]
    [DataRow("plain", "\"plain\"")]
    [DataRow("with spaces", "\"with spaces\"")]
    [DataRow("with\ttab", "\"with\ttab\"")]
    [DataRow("say \"hello\"", "\"say \\\"hello\\\"\"")]
    [DataRow("\"", "\"\\\"\"")]
    [DataRow("\"\"", "\"\\\"\\\"\"")]
    [DataRow(@"C:\temp\file.dll", "\"C:\\temp\\file.dll\"")]
    [DataRow(@"\\server\share\file.dll", "\"\\\\server\\share\\file.dll\"")]
    [DataRow(@"D:\", "\"D:\\\\\"")]
    [DataRow(@"C:\path with spaces\", "\"C:\\path with spaces\\\\\"")]
    [DataRow(@"path\\", "\"path\\\\\\\\\"")]
    [DataRow("\\\"quoted", "\"\\\\\\\"quoted\"")]
    [DataRow("\\\\\"quoted", "\"\\\\\\\\\\\"quoted\"")]
    [DataRow("a\\\"b\\", "\"a\\\\\\\"b\\\\\"")]
    public void AddDoubleQuoteShouldEscapeCommandLineArgument(string value, string expected)
    {
        var quoted = value.AddDoubleQuote();

        Assert.AreEqual(expected, quoted);
    }
}
