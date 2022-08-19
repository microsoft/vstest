﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using vstest.console.Internal;

namespace vstest.console.UnitTests.Internal;

[TestClass]
public class FilePatternParserTests
{
    private readonly FilePatternParser _filePatternParser;
    private readonly Mock<Matcher> _mockMatcherHelper;
    private readonly Mock<IFileHelper> _mockFileHelper;

    public FilePatternParserTests()
    {
        _mockMatcherHelper = new Mock<Matcher>();
        _mockFileHelper = new Mock<IFileHelper>();
        _filePatternParser = new FilePatternParser(_mockMatcherHelper.Object, _mockFileHelper.Object);
    }

    [TestMethod]
    public void FilePatternParserShouldCorrectlySplitPatternAndDirectory()
    {
        var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
        _mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
        _filePatternParser.GetMatchingFiles(TranslatePath(@"C:\Users\vanidhi\Desktop\a\c\*bc.dll"));

        // Assert
        _mockMatcherHelper.Verify(x => x.AddInclude(TranslatePath(@"*bc.dll")));
        _mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(TranslatePath(@"C:\Users\vanidhi\Desktop\a\c")))));
    }

    [TestMethod]
    public void FilePatternParserShouldCorrectlySplitWithArbitraryDirectoryDepth()
    {
        var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
        _mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
        _filePatternParser.GetMatchingFiles(TranslatePath(@"C:\Users\vanidhi\**\c\*bc.txt"));

        // Assert
        _mockMatcherHelper.Verify(x => x.AddInclude(TranslatePath(@"**\c\*bc.txt")));
        _mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(TranslatePath(@"C:\Users\vanidhi")))));
    }

    [TestMethod]
    public void FilePatternParserShouldCorrectlySplitWithWildCardInMultipleDirectory()
    {
        var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
        _mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
        _filePatternParser.GetMatchingFiles(TranslatePath(@"E:\path\to\project\tests\**.Tests\**\*.Tests.dll"));

        // Assert
        _mockMatcherHelper.Verify(x => x.AddInclude(TranslatePath(@"**.Tests\**\*.Tests.dll")));
        _mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(TranslatePath(@"E:\path\to\project\tests")))));
    }

    [TestMethod]
    public void FilePatternParserShouldCorrectlySplitWithMultipleWildCardInPattern()
    {
        var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
        _mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
        _filePatternParser.GetMatchingFiles(TranslatePath(@"E:\path\to\project\tests\Tests*.Blame*.dll"));

        // Assert
        _mockMatcherHelper.Verify(x => x.AddInclude(TranslatePath(@"Tests*.Blame*.dll")));
        _mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(TranslatePath(@"E:\path\to\project\tests")))));
    }

    [TestMethod]
    public void FilePatternParserShouldCorrectlySplitWithMultipleWildCardInMultipleDirectory()
    {
        var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
        _mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
        _filePatternParser.GetMatchingFiles(TranslatePath(@"E:\path\to\project\*tests\Tests*.Blame*.dll"));

        // Assert
        _mockMatcherHelper.Verify(x => x.AddInclude(TranslatePath(@"*tests\Tests*.Blame*.dll")));
        _mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(TranslatePath(@"E:\path\to\project")))));
    }

    [TestMethod]
    public void FilePatternParserShouldCheckIfFileExistsIfFullPathGiven()
    {
        var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
        _mockFileHelper.Setup(x => x.Exists(TranslatePath(@"E:\path\to\project\tests\Blame.Tests\\abc.Tests.dll"))).Returns(true);
        _mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
        var matchingFiles = _filePatternParser.GetMatchingFiles(TranslatePath(@"E:\path\to\project\tests\Blame.Tests\\abc.Tests.dll"));

        // Assert
        _mockFileHelper.Verify(x => x.Exists(TranslatePath(@"E:\path\to\project\tests\Blame.Tests\\abc.Tests.dll")));
        Assert.IsTrue(matchingFiles.Contains(TranslatePath(@"E:\path\to\project\tests\Blame.Tests\\abc.Tests.dll")));
    }

    [TestMethod]
    public void FilePatternParserShouldThrowCommandLineExceptionIfFileDoesNotExist()
    {
        var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
        _mockFileHelper.Setup(x => x.Exists(TranslatePath(@"E:\path\to\project\tests\Blame.Tests\\abc.Tests.dll"))).Returns(false);
        _mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);

        Assert.ThrowsException<TestSourceException>(() => _filePatternParser.GetMatchingFiles(TranslatePath(@"E:\path\to\project\tests\Blame.Tests\\abc.Tests.dll")));
    }

    private static string TranslatePath(string path)
    {
        // RuntimeInformation has conflict when used
        return Environment.OSVersion.Platform.ToString().StartsWith("Win")
            ? path
            : Regex.Replace(path.Replace("\\", "/"), @"(\w)\:/", @"/mnt/$1/");
    }
}
