// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Extensions;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    public void AddDoubleQuoteShouldWrapPlainValueInQuotes()
    {
        Assert.AreEqual("\"value\"", "value".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldWrapValueContainingSpaces()
    {
        Assert.AreEqual("\"C:\\Users\\Jane Doe\\project\"", @"C:\Users\Jane Doe\project".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldEscapeEmbeddedDoubleQuotes()
    {
        Assert.AreEqual("\"va\\\"lue\"", "va\"lue".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldDoubleTrailingBackslashesBeforeClosingQuote()
    {
        // A path ending in a single backslash (e.g. a drive root "D:\") must have its trailing
        // backslash doubled so the closing quote isn't parsed as an escaped quote by
        // CommandLineToArgvW-style command line parsers.
        Assert.AreEqual("\"D:\\\\\"", @"D:\".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldDoubleMultipleTrailingBackslashes()
    {
        Assert.AreEqual("\"value\\\\\\\\\"", @"value\\".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldNotAlterBackslashesNotAtTheEnd()
    {
        Assert.AreEqual("\"C:\\temp\\file.txt\"", @"C:\temp\file.txt".AddDoubleQuote());
    }
}
