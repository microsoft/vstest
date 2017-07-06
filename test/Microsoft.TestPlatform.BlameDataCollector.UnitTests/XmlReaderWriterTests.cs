// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

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
        private Mock<Stream> mockStream;


        public XmlReaderWriterTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.xmlReaderWriter = new XmlReaderWriter(this.mockFileHelper.Object);
            this.mockStream = new Mock<Stream>();

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
        public void ReadTestSequenceShouldThrowExceptionIfFileNotFound()
        {
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(false);

            Assert.ThrowsException<FileNotFoundException>(() =>
            {
                this.xmlReaderWriter.ReadTestSequence(String.Empty);
            });
        }

        [TestMethod]
        public void ReadTestSequenceShouldReadFileStream()
        {
            // Setup
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(m => m.GetStream("path.xml", FileMode.Open, FileAccess.ReadWrite)).Returns(this.mockStream.Object);

            // Call to Read Test Sequence
            this.xmlReaderWriter.ReadTestSequence("path.xml");

            // Verify Call to fileHelper
            this.mockFileHelper.Verify(x => x.GetStream("path.xml", FileMode.Open, FileAccess.ReadWrite));

            // Verify Call to stream read
            this.mockStream.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));

        }

        [TestMethod]
        public void WriteTestSequenceShouldWriteFileStream()
        {
            List<TestCase> testCaseList = new List<TestCase>();

            // Setup
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(m => m.GetStream("path.xml", FileMode.Create, FileAccess.ReadWrite)).Returns(this.mockStream.Object);
            this.mockStream.Setup(x => x.CanWrite).Returns(true);
            this.mockStream.Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));

            this.xmlReaderWriter.WriteTestSequence(testCaseList, "path");

            // Verify Call to fileHelper
            this.mockFileHelper.Verify(x => x.GetStream("path.xml", FileMode.Create, FileAccess.ReadWrite));

            // Verify Call to stream write
            this.mockStream.Verify(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
        }
    }
}
