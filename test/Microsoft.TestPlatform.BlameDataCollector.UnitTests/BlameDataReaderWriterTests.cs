// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.BlameDataCollector.UnitTests
{
    using Microsoft.TestPlatform.BlameDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;

    [TestClass]
    public class BlameDataReaderWriterTests
    {
        private string filePath;
        private Mock<IBlameFileManager> mockBlamefileManager;
        private List<object> TestSequence;
        private BlameDataReaderWriter blameDataReaderWriter;

        public BlameDataReaderWriterTests()
        {

            this.filePath = Path.Combine(AppContext.BaseDirectory, "TestSequence.xml");
            this.mockBlamefileManager = new Mock<IBlameFileManager>();
            this.TestSequence = new List<object>();
        }

        [TestMethod]
        public void WriteTestsToFileShouldThrowExceptionIfFilePathIsNull()
        {
            this.blameDataReaderWriter = new BlameDataReaderWriter(mockBlamefileManager.Object);
            TestCase testcase = new TestCase
            {
                Id = Guid.NewGuid(),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
            TestSequence.Add(testcase);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameDataReaderWriter.WriteTestsToFile(this.TestSequence, null);
            });
        }

        [TestMethod]
        public void WriteTestsToFileShouldThrowExceptionIfFilePathIsEmpty()
        {
            this.blameDataReaderWriter = new BlameDataReaderWriter(mockBlamefileManager.Object);
            TestCase testcase = new TestCase
            {
                Id = Guid.NewGuid(),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
            TestSequence.Add(testcase);

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameDataReaderWriter.WriteTestsToFile(this.TestSequence, String.Empty);
            });
        }
        [TestMethod]
        public void WriteTestsToFileShouldThrowExceptionIfTestSequenceIsNull()
        {
            this.blameDataReaderWriter = new BlameDataReaderWriter(mockBlamefileManager.Object);
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameDataReaderWriter.WriteTestsToFile(null, this.filePath);
            });
        }

        [TestMethod]
        public void InitializeBlameDataReaderWriterShouldThrowExceptionIfBlameFileManagerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new BlameDataReaderWriter(null);
            });
        }

        [TestMethod]
        public void WriteTestsToFileShouldAddTestsInFormat()
        {
            // Initialize blameDataWriter
            TestCase testcase = new TestCase
            {
                Id = Guid.NewGuid(),
                FullyQualifiedName = "TestProject.UnitTest.TestMethod",
                Source = "abc.dll"
            };
            TestSequence.Add(testcase);
            this.blameDataReaderWriter = new BlameDataReaderWriter(this.mockBlamefileManager.Object);

            // Call WriteTestsToFile method
            this.blameDataReaderWriter.WriteTestsToFile(this.TestSequence, this.filePath);

            // Verify if tests are added
            this.mockBlamefileManager.Verify(x => x.AddTestsToFormat(this.TestSequence,this.filePath), Times.Once);

        }
    }
}
