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
        private List<TestCase> TestSequence;
        private BlameDataReaderWriter blameDataReaderWriter;

        public BlameDataReaderWriterTests()
        {

            this.filePath = Path.Combine(AppContext.BaseDirectory, "TestSequence.xml");
            this.mockBlamefileManager = new Mock<IBlameFileManager>();
            this.TestSequence = new List<TestCase>();
        }

        [TestMethod]
        public void InitializeBlameDataReaderWriterShouldThrowExceptionIfFilePathIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new BlameDataReaderWriter(null, mockBlamefileManager.Object);
            });
        }

        [TestMethod]
        public void InitializeBlameDataReaderWriterShouldThrowExceptionIfFilePathIsEmpty()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new BlameDataReaderWriter(String.Empty, mockBlamefileManager.Object);
            });
        }

        [TestMethod]
        public void InitializeBlameDataReaderWriterShouldThrowExceptionIfBlameFileManagerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new BlameDataReaderWriter("filename", null);
            });
        }

        [TestMethod]
        public void WriteTestsToFileShouldAddTestsInFormat()
        {
            // Initialize blameDataWriter
            TestCase testcase = new TestCase();
            testcase.Id = Guid.NewGuid();
            testcase.FullyQualifiedName = "TestProject.UnitTest.TestMethod";
            testcase.Source = "abc.dll";
            TestSequence.Add(testcase);
            this.blameDataReaderWriter = new BlameDataReaderWriter(this.TestSequence, this.filePath, this.mockBlamefileManager.Object);

            // Call WriteTestsToFile method
            this.blameDataReaderWriter.WriteTestsToFile();

            // Verify if tests are added
            this.mockBlamefileManager.Verify(x => x.AddTestsToFormat(this.TestSequence,this.filePath), Times.Once);

        }
    }
}
