// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
        private List<Guid> testCaseList;
        private Dictionary<Guid, BlameTestObject> testObjectDictionary;
        private BlameTestObject blameTestObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriterTests"/> class.
        /// </summary>
        public XmlReaderWriterTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.xmlReaderWriter = new TestableXmlReaderWriter(this.mockFileHelper.Object);
            this.mockStream = new Mock<Stream>();
            this.testCaseList = new List<Guid>();
            this.testObjectDictionary = new Dictionary<Guid, BlameTestObject>();
            var testcase = new TestCase
            {
                ExecutorUri = new Uri("test:/abc"),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
            this.blameTestObject = new BlameTestObject(testcase);
        }

        /// <summary>
        /// The write test sequence should throw exception if file path is null.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsNull()
        {
            this.testCaseList.Add(this.blameTestObject.Id);
            this.testObjectDictionary.Add(this.blameTestObject.Id, this.blameTestObject);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.WriteTestSequence(this.testCaseList, this.testObjectDictionary, null);
            });
        }

        /// <summary>
        /// The write test sequence should throw exception if file path is empty.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsEmpty()
        {
            this.testCaseList.Add(this.blameTestObject.Id);
            this.testObjectDictionary.Add(this.blameTestObject.Id, this.blameTestObject);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.xmlReaderWriter.WriteTestSequence(this.testCaseList, this.testObjectDictionary, string.Empty);
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

            this.xmlReaderWriter.WriteTestSequence(this.testCaseList, this.testObjectDictionary, "path");

            // Verify Call to fileHelper
            this.mockFileHelper.Verify(x => x.GetStream("path.xml", FileMode.Create, FileAccess.ReadWrite));

            // Verify Call to stream write
            this.mockStream.Verify(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
        }

        /// <summary>
        /// Verify Write and Read test sequence to check file contents if test completed is false.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldWriteCorrectFileContentsIfTestCompletedIsFalse()
        {
            var xmlReaderWriter = new XmlReaderWriter();
            var testObject = new BlameTestObject(new TestCase("Abc.UnitTest1", new Uri("test:/abc"), "Abc.dll"));
            testObject.DisplayName = "UnitTest1";
            var testSequence = new List<Guid>
            {
                testObject.Id
            };
            var testObjectDictionary = new Dictionary<Guid, BlameTestObject>
            {
                { testObject.Id, testObject }
            };

            var filePath = xmlReaderWriter.WriteTestSequence(testSequence, testObjectDictionary, Path.GetTempPath());
            var testCaseList = xmlReaderWriter.ReadTestSequence(filePath);
            File.Delete(filePath);

            Assert.AreEqual("Abc.UnitTest1", testCaseList.First().FullyQualifiedName);
            Assert.AreEqual("UnitTest1", testCaseList.First().DisplayName);
            Assert.AreEqual("Abc.dll", testCaseList.First().Source);
            Assert.IsFalse(testCaseList.First().IsCompleted);
        }

        /// <summary>
        /// Verify Write and Read test sequence to check file contents if test completed is true.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldWriteCorrectFileContentsIfTestCompletedIsTrue()
        {
            var xmlReaderWriter = new XmlReaderWriter();
            var testObject = new BlameTestObject(new TestCase("Abc.UnitTest1", new Uri("test:/abc"), "Abc.dll"));
            testObject.DisplayName = "UnitTest1";
            var testSequence = new List<Guid>
            {
                testObject.Id
            };
            var testObjectDictionary = new Dictionary<Guid, BlameTestObject>
            {
                { testObject.Id, testObject }
            };

            testObjectDictionary[testObject.Id].IsCompleted = true;
            var filePath = xmlReaderWriter.WriteTestSequence(testSequence, testObjectDictionary, Path.GetTempPath());
            var testCaseList = xmlReaderWriter.ReadTestSequence(filePath);
            File.Delete(filePath);

            Assert.AreEqual("Abc.UnitTest1", testCaseList.First().FullyQualifiedName);
            Assert.AreEqual("UnitTest1", testCaseList.First().DisplayName);
            Assert.AreEqual("Abc.dll", testCaseList.First().Source);
            Assert.IsTrue(testCaseList.First().IsCompleted);
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
