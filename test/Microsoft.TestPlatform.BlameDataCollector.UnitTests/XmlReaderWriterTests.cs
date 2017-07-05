// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.TestPlatform.BlameDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class XmlReaderWriterTests
    {
        private XmlReaderWriter xmlReaderWriter;
        private Mock<IFileHelper> mockFileHelper;

        public XmlReaderWriterTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.xmlReaderWriter = new XmlReaderWriter(this.mockFileHelper.Object);
        }            
        
        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsNull()
        {
            TestCase testcase = new TestCase
            {
                Id = Guid.NewGuid(),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
            List<TestCase> testSequence = new List<TestCase>();
            testSequence.Add(testcase);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.WriteTestSequence(testSequence, null);
            });
        }

        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsEmpty()
        {
            TestCase testcase = new TestCase
            {
                Id = Guid.NewGuid(),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
            List<TestCase> testSequence = new List<TestCase>();
            testSequence.Add(testcase);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.WriteTestSequence(testSequence, String.Empty);
            });
        }

        [TestMethod]
        public void ReadTestSequenceShouldThrowExceptionIfFilePathIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.ReadTestSequence(null);
            });
        }

        [TestMethod]
        public void ReadTestSequenceShouldThrowExceptionIfFilePathIsEmpty()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.ReadTestSequence(String.Empty);
            });
        }

        [TestMethod]
        public void ReadTestSequenceShouldThrowExceptionIfFileNotFound()
        {
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(false);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.ReadTestSequence(String.Empty);
            });
        }
    }
}
