// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Publisher
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class MetricsPublisherTests
    {
        [TestMethod]
        public void RemoveInvalidCharactersFromKeysShouldReturnEmptyDictionaryIfMetricsIsNull()
        {
            var publishMetrics = new MetricsPublisher();

            // Act.
            var result = publishMetrics.RemoveInvalidCharactersFromProperties(null);

            // Assert.
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void RemoveInvalidCharactersFromPropertiesShouldEmptyMetricIfMetricsIsEmpty()
        {
            var publishMetrics = new MetricsPublisher();
            var dummyDictionary = new Dictionary<string, object>();

            // Act.
            var result = publishMetrics.RemoveInvalidCharactersFromProperties(dummyDictionary);

            // Assert.
            Assert.AreEqual(dummyDictionary.Count, result.Count);
        }

        [TestMethod]
        public void RemoveInvalidCharactersFromPropertiesShouldValidMetrics()
        {
            var publishMetrics = new MetricsPublisher();
            var dummyDictionary = new Dictionary<string, object>();
            dummyDictionary.Add("DummyMessage://", "DummyValue");
            dummyDictionary.Add("Dummy2", "DummyValue2");

            // Act.
            var result = publishMetrics.RemoveInvalidCharactersFromProperties(dummyDictionary);

            // Assert.
            Assert.AreEqual(dummyDictionary.Count, result.Count);
            Assert.IsTrue(result.ContainsKey("DummyMessage//"));
        }

        [TestMethod]
        public void LogToFileShouldCreateDirectoryIfNotExists()
        {
            var publishMetrics = new MetricsPublisher();
            var dummyDictionary = new Dictionary<string, object>();
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(false);
            dummyDictionary.Add("DummyMessage://", "DummyValue");
            dummyDictionary.Add("Dummy2", "DummyValue2");

            // Act.
            publishMetrics.LogToFile("dummyevent", dummyDictionary, mockFileHelper.Object);

            // Verify.
            mockFileHelper.Verify(fh => fh.CreateDirectory(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void LogToFileShouldWriteAllText()
        {
            var publishMetrics = new MetricsPublisher();
            var dummyDictionary = new Dictionary<string, object>();
            var mockFileHelper = new Mock<IFileHelper>();
            dummyDictionary.Add("DummyMessage://", "DummyValue");
            dummyDictionary.Add("Dummy2", "DummyValue2");

            // Act.
            publishMetrics.LogToFile("dummyevent", dummyDictionary, mockFileHelper.Object);

            // Verify.
            mockFileHelper.Verify(fh => fh.WriteAllTextToFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
