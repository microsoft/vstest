// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.CoreUtilities.UnitTests;

[TestClass]
public sealed class DiagnosticLogPathTests
{
    [TestMethod]
    [DataRow("log with spaces.txt")]
    [DataRow("log\"one\".txt")]
    [DataRow("\"lead")]
    [DataRow("trail\"")]
    [DataRow("\"\"log\"\"")]
    [DataRow("\"log\"one\"")]
    [DataRow(@"log\")]
    [DataRow(@"log\\")]
    public void RemoveLegacyQuotesLeavesFilenameCharactersUnchanged(string path)
    {
        Assert.AreEqual(path, DiagnosticLogPath.RemoveLegacyQuotes(path));
    }

    [TestMethod]
    [DataRow("\"log.txt\"", "log.txt")]
    [DataRow("\"log with spaces/\"", "log with spaces/")]
    [DataRow("\"log with spaces\\\"", "log with spaces\\")]
    public void RemoveLegacyQuotesOnlyRemovesOneWrapperOnWindows(string path, string windowsPath)
    {
        Assert.AreEqual(Path.DirectorySeparatorChar == '\\' ? windowsPath : path, DiagnosticLogPath.RemoveLegacyQuotes(path));
    }
}
