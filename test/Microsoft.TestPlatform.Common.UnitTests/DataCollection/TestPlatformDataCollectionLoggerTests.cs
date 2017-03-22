// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestPlatformDataCollectionLoggerTests
    {
        private TestPlatformDataCollectionLogger logger;
        private Mock<IMessageSink> messageSink;
        private DataCollectorConfig dataCollectorConfig;
        private DataCollectionContext context;

        public TestPlatformDataCollectionLoggerTests()
        {
            this.messageSink = new Mock<IMessageSink>();
            this.dataCollectorConfig = new DataCollectorConfig(typeof(CustomDataCollector));
            this.logger = new TestPlatformDataCollectionLogger(this.messageSink.Object, this.dataCollectorConfig);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            this.context = new DataCollectionContext(sessionId);
        }

        [TestMethod]
        public void LogErrorShouldThrowExceptionIfContextIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.logger.LogError(null, string.Empty);
            });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.logger.LogError(null, new Exception());
            });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.logger.LogError(null, string.Empty, new Exception());
            });
        }

        [TestMethod]
        public void LogErrorShouldThrowExceptionIfTextIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.logger.LogError(this.context, (string)null);
            });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.logger.LogError(this.context, null, new Exception());
            });
        }

        [TestMethod]
        public void LogErrorShouldThrowExceptionIfExceptionIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.logger.LogError(this.context, (Exception)null);
            });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.logger.LogError(this.context, string.Empty, (Exception)null);
            });
        }

        [TestMethod]
        public void LogErrorShouldSendMessageToMessageSink()
        {
            var text = "customtext";
            this.logger.LogError(this.context, text);

            this.messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once());

            this.logger.LogError(this.context, new Exception(text));
            this.messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Exactly(2));

            this.logger.LogError(this.context, text, new Exception(text));
            this.messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Exactly(3));
        }

        [TestMethod]
        public void LogWarningShouldSendMessageToMessageSink()
        {
            var text = "customtext";
            this.logger.LogWarning(this.context, text);

            this.messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once());
        }
    }
}
