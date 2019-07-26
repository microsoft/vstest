// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Internal
{
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.Collections.Generic;
    using vstest.console.Internal;

    [TestClass]
    public class FilePatternParserTests
    {
        private FilePatternParser filePatternParser;
        private Mock<Matcher> mockMatcherHelper;

        [TestInitialize]
        public void TestInit()
        {
            this.mockMatcherHelper = new Mock<Matcher>();
            this.filePatternParser = new FilePatternParser(this.mockMatcherHelper.Object);
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitPatternAndDirectory()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            var sourceFiles = new List<string>();
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            var isValidPattern = this.filePatternParser.IsValidPattern(@"C:\Users\vanidhi\Desktop\a\c\*bc.dll", out sourceFiles);

            //Assert
            Assert.IsTrue(isValidPattern);
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"*bc.dll"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"C:\Users\vanidhi\Desktop\a\c"))));
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitWithArbitraryDirectoryDepth()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            var sourceFiles = new List<string>();
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            var isValidPattern = this.filePatternParser.IsValidPattern(@"C:\Users\vanidhi\**\c\*bc.txt", out sourceFiles);

            //Assert
            Assert.IsTrue(isValidPattern);
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"**\c\*bc.txt"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"C:\Users\vanidhi"))));
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitWithWildCardInMultipleDirectory()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            var sourceFiles = new List<string>();
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            var isValidPattern = this.filePatternParser.IsValidPattern(@"E:\path\to\project\tests\**.Tests\**\*.Tests.dll", out sourceFiles);

            //Assert
            Assert.IsTrue(isValidPattern);
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"**.Tests\**\*.Tests.dll"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"E:\path\to\project\tests"))));
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitWithMultpleWildCardInPattern()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            var sourceFiles = new List<string>();
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            var isValidPattern = this.filePatternParser.IsValidPattern(@"E:\path\to\project\tests\Tests*.Blame*.dll", out sourceFiles);

            //Assert
            Assert.IsTrue(isValidPattern);
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"Tests*.Blame*.dll"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"E:\path\to\project\tests"))));
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitWithMultpleWildCardInMultipleDirectory()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            var sourceFiles = new List<string>();
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            var isValidPattern = this.filePatternParser.IsValidPattern(@"E:\path\to\project\*tests\Tests*.Blame*.dll", out sourceFiles);

            //Assert
            Assert.IsTrue(isValidPattern);
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"*tests\Tests*.Blame*.dll"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"E:\path\to\project"))));
        }

        [TestMethod]
        public void IsValidPatternShouldReturnFalseForAbsoluteSourcePath()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            var sourceFiles = new List<string>();
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            var isValidPattern = this.filePatternParser.IsValidPattern(@"E:\path\to\project\tests\Blame.Tests\\abc.Tests.dll", out sourceFiles);

            //Assert
            Assert.IsFalse(isValidPattern);
        }
    }
}
