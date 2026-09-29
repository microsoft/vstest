// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests;

[TestClass]
public class ConsoleParametersTests
{
    [TestMethod]
    public void LogFilePathShouldEnsureDoubleQuote()
    {
        var moqFileHelper = new Mock<IFileHelper>();
        moqFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);

        var sut = new ConsoleParameters(moqFileHelper.Object);

        sut.LogFilePath = "c:\\users\\file location\\o.txt";

        string result = sut.LogFilePath;

        Assert.StartsWith("\"", result); 
    }

    [TestMethod]
    public void TraceLevelShouldHaveVerboseAsDefaultValue()
    {
        var consoleParameters = new ConsoleParameters(new FileHelper());
        Assert.AreEqual(TraceLevel.Verbose, consoleParameters.TraceLevel);
    }

    [TestMethod]
    [DataRow("log with spaces.txt")]
    [DataRow(@"log with spaces\")]
    [DataRow(@"log with spaces\\")]
    [DataRow("log with spaces/")]
    public void LogFilePathKeepsRawPathSeparateFromCompatibilityWrapper(string path)
    {
        var fileHelper = new Mock<IFileHelper>();
        fileHelper.Setup(helper => helper.DirectoryExists(It.IsAny<string>())).Returns(true);
        var parameters = new ConsoleParameters(fileHelper.Object) { LogFilePath = path };

        Assert.AreEqual(path, parameters.UnquotedLogFilePath);
        Assert.AreEqual("\"" + path + "\"", parameters.LogFilePath);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Linux | OperatingSystems.OSX)]
    [DataRow("\"\"")]
    [DataRow("\"log\"")]
    [DataRow("log\\\"one\".txt")]
    public void LogFilePathPreservesLiteralUnixQuotes(string path)
    {
        var parameters = new ConsoleParameters { LogFilePath = path };

        Assert.AreEqual(path, parameters.UnquotedLogFilePath);
        Assert.AreEqual("\"" + path + "\"", parameters.LogFilePath);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void LogFilePathRejectsEmptyLegacyQuotedWindowsPath()
    {
        var parameters = new ConsoleParameters();

        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => parameters.LogFilePath = "\"\"");
        Assert.AreEqual(nameof(ConsoleParameters.LogFilePath), exception.ParamName);
        Assert.IsNull(parameters.LogFilePath);
        Assert.IsNull(parameters.UnquotedLogFilePath);
    }
}
