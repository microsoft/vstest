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
        private readonly TestableXmlReaderWriter xmlReaderWriter;
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly Mock<Stream> mockStream;
        private readonly List<Guid> testCaseList;
        private readonly Dictionary<Guid, BlameTestObject> testObjectDictionary;
        private readonly BlameTestObject blameTestObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriterTests"/> class.
        /// </summary>
        public XmlReaderWriterTests()
        {
            mockFileHelper = new Mock<IFileHelper>();
            xmlReaderWriter = new TestableXmlReaderWriter(mockFileHelper.Object);
            mockStream = new Mock<Stream>();
            testCaseList = new List<Guid>();
            testObjectDictionary = new Dictionary<Guid, BlameTestObject>();
            var testcase = new TestCase
            {
                ExecutorUri = new Uri("test:/abc"),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
            blameTestObject = new BlameTestObject(testcase);
        }

        /// <summary>
        /// The write test sequence should throw exception if file path is null.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsNull()
        {
            testCaseList.Add(blameTestObject.Id);
            testObjectDictionary.Add(blameTestObject.Id, blameTestObject);

            Assert.ThrowsException<ArgumentNullException>(() => xmlReaderWriter.WriteTestSequence(testCaseList, testObjectDictionary, null));
        }

        /// <summary>
        /// The write test sequence should throw exception if file path is empty.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldThrowExceptionIfFilePathIsEmpty()
        {
            testCaseList.Add(blameTestObject.Id);
            testObjectDictionary.Add(blameTestObject.Id, blameTestObject);

            Assert.ThrowsException<ArgumentNullException>(() => xmlReaderWriter.WriteTestSequence(testCaseList, testObjectDictionary, string.Empty));
        }

        /// <summary>
        /// The read test sequence should throw exception if file path is null.
        /// </summary>
        [TestMethod]
        public void ReadTestSequenceShouldThrowExceptionIfFilePathIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => xmlReaderWriter.ReadTestSequence(null));
        }

        /// <summary>
        /// The read test sequence should throw exception if file not found.
        /// </summary>
        [TestMethod]
        public void ReadTestSequenceShouldThrowExceptionIfFileNotFound()
        {
            mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(false);

            Assert.ThrowsException<FileNotFoundException>(() => xmlReaderWriter.ReadTestSequence(string.Empty));
        }

        /// <summary>
        /// The read test sequence should read file stream.
        /// </summary>
        [TestMethod]
        public void ReadTestSequenceShouldReadFileStream()
        {
            // Setup
            mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(m => m.GetStream("path.xml", FileMode.Open, FileAccess.ReadWrite)).Returns(mockStream.Object);

            // Call to Read Test Sequence
            xmlReaderWriter.ReadTestSequence("path.xml");

            // Verify Call to fileHelper
            mockFileHelper.Verify(x => x.GetStream("path.xml", FileMode.Open, FileAccess.ReadWrite));

            // Verify Call to stream read
            mockStream.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
        }

        /// <summary>
        /// The write test sequence should write file stream.
        /// </summary>
        [TestMethod]
        public void WriteTestSequenceShouldWriteFileStream()
        {
            // Setup
            mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);
            mockFileHelper.Setup(m => m.GetStream("path.xml", FileMode.Create, FileAccess.ReadWrite)).Returns(mockStream.Object);
            mockStream.Setup(x => x.CanWrite).Returns(true);
            mockStream.Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));

            xmlReaderWriter.WriteTestSequence(testCaseList, testObjectDictionary, "path");

            // Verify Call to fileHelper
            mockFileHelper.Verify(x => x.GetStream("path.xml", FileMode.Create, FileAccess.ReadWrite));

            // Verify Call to stream write
            mockStream.Verify(x => x.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
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
