// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestPlatformDataCollectionLoggerTests
    {
        private readonly TestPlatformDataCollectionLogger logger;
        private readonly Mock<IMessageSink> messageSink;
        private readonly DataCollectorConfig dataCollectorConfig;
        private readonly DataCollectionContext context;

        public TestPlatformDataCollectionLoggerTests()
        {
            messageSink = new Mock<IMessageSink>();
            dataCollectorConfig = new DataCollectorConfig(typeof(CustomDataCollector));
            logger = new TestPlatformDataCollectionLogger(messageSink.Object, dataCollectorConfig);

            var guid = Guid.NewGuid();
            var sessionId = new SessionId(guid);
            context = new DataCollectionContext(sessionId);
        }

        [TestMethod]
        public void LogErrorShouldThrowExceptionIfContextIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => logger.LogError(null, string.Empty));

            Assert.ThrowsException<ArgumentNullException>(() => logger.LogError(null, new Exception()));

            Assert.ThrowsException<ArgumentNullException>(() => logger.LogError(null, string.Empty, new Exception()));
        }

        [TestMethod]
        public void LogErrorShouldThrowExceptionIfTextIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => logger.LogError(context, (string)null));

            Assert.ThrowsException<ArgumentNullException>(() => logger.LogError(context, null, new Exception()));
        }

        [TestMethod]
        public void LogErrorShouldThrowExceptionIfExceptionIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => logger.LogError(context, (Exception)null));

            Assert.ThrowsException<ArgumentNullException>(() => logger.LogError(context, string.Empty, (Exception)null));
        }

        [TestMethod]
        public void LogErrorShouldSendMessageToMessageSink()
        {
            var text = "customtext";
            logger.LogError(context, text);

            messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once());

            logger.LogError(context, new Exception(text));
            messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Exactly(2));

            logger.LogError(context, text, new Exception(text));
            messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Exactly(3));
        }

        [TestMethod]
        public void LogWarningShouldSendMessageToMessageSink()
        {
            var text = "customtext";
            logger.LogWarning(context, text);

            messageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once());
        }
    }
}
