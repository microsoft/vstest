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
        Assert.AreEqual("\"C:\\Users\\Jane Doe\\file.txt\"", @"C:\Users\Jane Doe\file.txt".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldEscapeEmbeddedDoubleQuotes()
    {
        Assert.AreEqual("\"va\\\"lue\"", "va\"lue".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldDoubleSingleTrailingBackslash()
    {
        Assert.AreEqual("\"D:\\\\\"", @"D:\".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldDoubleMultipleTrailingBackslashes()
    {
        Assert.AreEqual("\"C:\\path\\\\\\\\\"", @"C:\path\\".AddDoubleQuote());
    }

    [TestMethod]
    public void AddDoubleQuoteShouldNotAffectBackslashesNotAtTheEnd()
    {
        Assert.AreEqual("\"C:\\path\\to\\file\"", @"C:\path\to\file".AddDoubleQuote());
    }
}
