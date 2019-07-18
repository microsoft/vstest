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
    using vstest.console.Internal.Interfaces;

    [TestClass]
    public class FilePatternParserTests
    {
        private FilePatternParser filePatternParser;
        private Mock<IMatcherHelper> mockMatcherHelper;

        [TestInitialize]
        public void TestInit()
        {
            this.mockMatcherHelper = new Mock<IMatcherHelper>();
            this.filePatternParser = new FilePatternParser(this.mockMatcherHelper.Object);
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitPatternAndDirectory()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            this.filePatternParser.GetMatchingFiles(@"C:\Users\vanidhi\Desktop\a\c\*bc.dll");

            //Assert
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"*bc.dll"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"C:\Users\vanidhi\Desktop\a\c"))));
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitWithArbitraryDirectoryDepth()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            this.filePatternParser.GetMatchingFiles(@"C:\Users\vanidhi\**\c\*bc.txt");

            //Assert
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"**\c\*bc.txt"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"C:\Users\vanidhi"))));
        }

        [TestMethod]
        public void FilePatternParserShouldCorrectlySplitWithWildCardInMultipleDirectory()
        {
            var patternMatchingResult = new PatternMatchingResult(new List<FilePatternMatch>());
            this.mockMatcherHelper.Setup(x => x.Execute(It.IsAny<DirectoryInfoWrapper>())).Returns(patternMatchingResult);
            this.filePatternParser.GetMatchingFiles(@"E:\path\to\project\tests\**.Tests\**\*.Tests.dll");

            //Assert
            this.mockMatcherHelper.Verify(x => x.AddInclude(@"**.Tests\**\*.Tests.dll"));
            this.mockMatcherHelper.Verify(x => x.Execute(It.Is<DirectoryInfoWrapper>(y => y.FullName.Equals(@"E:\path\to\project\tests"))));
        }
    }
}
