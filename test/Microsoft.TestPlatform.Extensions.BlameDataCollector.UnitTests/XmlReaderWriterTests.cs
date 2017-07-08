// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.TestPlatform.Extensions.BlameDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// The xml reader writer tests.
    /// </summary>
    [TestClass]
    public class XmlReaderWriterTests
    {
        private TestableXmlReaderWriter xmlReaderWriter;
        private Mock<IFileHelper> mockFileHelper;
        private Mock<Stream> mockStream;
        List<TestCase> testCaseList;
        private TestCase testcase;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriterTests"/> class.
        /// </summary>
        public XmlReaderWriterTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.xmlReaderWriter = new TestableXmlReaderWriter(this.mockFileHelper.Object);
            this.mockStream = new Mock<Stream>();
            this.testCaseList = new List<TestCase>();
            this.testcase = new TestCase
            {
                Id = Guid.NewGuid(),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
        }

        /// <summary>
        /// The write test sequence should throw exception if file path is null.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsNull()
        {
            this.testCaseList.Add(this.testcase);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.WriteTestSequence(this.testCaseList, null);
            });
        }

        /// <summary>
        /// The write test sequence should throw exception if file path is empty.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsEmpty()
        {
            this.testCaseList.Add(this.testcase);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.WriteTestSequence(this.testCaseList, string.Empty);
            });
        }

        /// <summary>
        /// The read test sequence should throw exception if file path is null.
        /// </summary>
        [TestMethod]
        public void ReadTestSequenceShouldThrowExceptionIfFilePathIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.ReadTestSequence(null);
            });
        }

        /// <summary>
        /// The read test sequence should throw exception if file not found.
        /// </summary>
        [TestMethod]
        public void ReadTestSequenceShouldThrowExceptionIfFileNotFound()
        {
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(false);

            Assert.ThrowsException<FileNotFoundException>(() =>
            {
                this.xmlReaderWriter.ReadTestSequence(string.Empty);
            });
        }

        /// <summary>
        /// The read test sequence should read file stream.
        /// </summary>
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

        /// <summary>
        /// The write test sequence should write file stream.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldWriteFileStream()
        {
            // Setup
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);
            this.mockFileHelper.Setup(m => m.GetStream("path.xml", FileMode.Create, FileAccess.ReadWrite)).Returns(this.mockStream.Object);
            this.mockStream.Setup(x => x.CanWrite).Returns(true);
            this.mockStream.Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));

            this.xmlReaderWriter.WriteTestSequence(this.testCaseList, "path");

            // Verify Call to fileHelper
            this.mockFileHelper.Verify(x => x.GetStream("path.xml", FileMode.Create, FileAccess.ReadWrite));

            // Verify Call to stream write
            this.mockStream.Verify(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
        }

        /// <summary>
        /// The testable xml reader writer.
        /// </summary>
        internal class TestableXmlReaderWriter : XmlReaderWriter
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableXmlReaderWriter"/> class.
            /// </summary>
            /// <param name="fileHelper">
            /// The file helper.
            /// </param>
            internal TestableXmlReaderWriter(IFileHelper fileHelper)
                : base(fileHelper)
            {
            }
        }
    }
}
