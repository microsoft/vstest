// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.TestRunAttachmentsProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TestRunAttachmentsProcessingManagerTests
    {
        private const string uri1 = "datacollector://microsoft/some1/1.0";
        private const string uri2 = "datacollector://microsoft/some2/2.0";
        private const string uri3 = "datacollector://microsoft/some3/2.0";

        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IMetricsCollection> mockMetricsCollection;
        private readonly Mock<ITestPlatformEventSource> mockEventSource;
        private readonly Mock<IDataCollectorAttachmentProcessor> mockAttachmentHandler1;
        private readonly Mock<IDataCollectorAttachmentProcessor> mockAttachmentHandler2;
        private readonly Mock<IDataCollectorAttachmentsProcessorsFactory> mockDataCollectorAttachmentsProcessorsFactory;
        private readonly Mock<ITestRunAttachmentsProcessingEventsHandler> mockEventsHandler;
        private readonly TestRunAttachmentsProcessingManager manager;
        private readonly CancellationTokenSource cancellationTokenSource;

        public TestRunAttachmentsProcessingManagerTests()
        {
            mockRequestData = new Mock<IRequestData>();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(r => r.MetricsCollection).Returns(mockMetricsCollection.Object);

            mockEventSource = new Mock<ITestPlatformEventSource>();
            mockAttachmentHandler1 = new Mock<IDataCollectorAttachmentProcessor>();
            mockAttachmentHandler1.Setup(x => x.SupportsIncrementalProcessing).Returns(true);
            mockAttachmentHandler2 = new Mock<IDataCollectorAttachmentProcessor>();
            mockAttachmentHandler2.Setup(x => x.SupportsIncrementalProcessing).Returns(true);
            mockEventsHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();
            mockDataCollectorAttachmentsProcessorsFactory = new Mock<IDataCollectorAttachmentsProcessorsFactory>();

            mockAttachmentHandler1.Setup(h => h.GetExtensionUris()).Returns(new[] { new Uri(uri1) });
            mockAttachmentHandler2.Setup(h => h.GetExtensionUris()).Returns(new[] { new Uri(uri2) });
            mockDataCollectorAttachmentsProcessorsFactory.Setup(p => p.Create(It.IsAny<InvokedDataCollector[]>(), It.IsAny<IMessageLogger>()))
            .Returns(new DataCollectorAttachmentProcessor[]
            {
               new DataCollectorAttachmentProcessor( "friendlyNameA", mockAttachmentHandler1.Object ),
               new DataCollectorAttachmentProcessor( "friendlyNameB"  ,mockAttachmentHandler2.Object )
            });

            manager = new TestRunAttachmentsProcessingManager(mockEventSource.Object, mockDataCollectorAttachmentsProcessorsFactory.Object);

            cancellationTokenSource = new CancellationTokenSource();
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnInitialAttachmentsThroughEventsHandler_IfNoAttachmentsOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>();

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            VerifyCompleteEvent(false, false);
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingComplete(It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => !a.IsCanceled), It.Is<ICollection<AttachmentSet>>(c => c.Count == 0)));
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Never);
            mockEventSource.Verify(s => s.TestRunAttachmentsProcessingStart(0));
            mockEventSource.Verify(s => s.TestRunAttachmentsProcessingStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 0, outputCount: 0);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnNoAttachments_IfNoAttachmentsOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>();

            // act
            var result = await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(0, result.Count);
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 0, outputCount: 0);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturn1NotProcessedAttachmentThroughEventsHandler_If1NotRelatedAttachmentOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri3), "uri3_input")
            };

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            VerifyCompleteEvent(false, false, inputAttachments[0]);
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Never);
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturn1NotProcessedAttachment_If1NotRelatedAttachmentOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri3), "uri3_input")
            };

            // act
            var result = await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(inputAttachments[0]));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturn1ProcessedAttachmentThroughEventsHandler_IfRelatedAttachmentOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            List<AttachmentSet> outputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_output")
            };

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(outputAttachments);

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            VerifyCompleteEvent(false, false, outputAttachments[0]);
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Never);
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturn1ProcessedAttachment_IfRelatedAttachmentOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            List<AttachmentSet> outputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_output")
            };

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(outputAttachments);

            // act
            var result = await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(outputAttachments[0]));
            mockEventSource.Verify(s => s.TestRunAttachmentsProcessingStart(1));
            mockEventSource.Verify(s => s.TestRunAttachmentsProcessingStop(1));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnInitialAttachmentsThroughEventsHandler_IfRelatedAttachmentOnInputButHandlerThrowsException()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            var exceptionToThrow = new Exception("exception message");
            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).Throws(exceptionToThrow);

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            VerifyCompleteEvent(false, false, inputAttachments[0]);
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Never);
            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Error, exceptionToThrow.ToString()), Times.Once);
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Once);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnInitialAttachments_IfRelatedAttachmentOnInputButHandlerThrowsException()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).Throws(new Exception("exception message"));

            // act
            var result = await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(inputAttachments[0]));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Once);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnInitialAttachmentsThroughEventsHandler_IfOperationIsCancelled()
        {
            // arrange
            cancellationTokenSource.Cancel();
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            VerifyCompleteEvent(true, false, inputAttachments[0]);
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Never);
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1, status: "Canceled");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnInitialAttachments_IfOperationIsCancelled()
        {
            // arrange
            cancellationTokenSource.Cancel();
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            // act
            var result = await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(inputAttachments[0]));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1, status: "Canceled");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnProcessedAttachmentsThroughEventsHandler_IfRelatedAttachmentsOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input1"),
                new AttachmentSet(new Uri(uri1), "uri1_input2"),
                new AttachmentSet(new Uri(uri2), "uri2_input1"),
                new AttachmentSet(new Uri(uri2), "uri2_input2"),
                new AttachmentSet(new Uri(uri3), "uri3_input1"),
            };

            List<AttachmentSet> outputAttachmentsForHandler1 = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_output")
            };

            List<AttachmentSet> outputAttachmentsForHandler2 = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri2), "uri2_output")
            };

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(null, It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(outputAttachmentsForHandler1);
            mockAttachmentHandler2.Setup(h => h.ProcessAttachmentSetsAsync(null, It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(outputAttachmentsForHandler2);

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            VerifyCompleteEvent(false, false, inputAttachments[4], outputAttachmentsForHandler1.First(), outputAttachmentsForHandler2.First());
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Never);
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[0]) && c.Contains(inputAttachments[1])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[2]) && c.Contains(inputAttachments[3])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));

            VerifyMetrics(inputCount: 5, outputCount: 3);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnProcessedAttachments_IfRelatedAttachmentsOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input1"),
                new AttachmentSet(new Uri(uri1), "uri1_input2"),
                new AttachmentSet(new Uri(uri2), "uri2_input1"),
                new AttachmentSet(new Uri(uri2), "uri2_input2"),
                new AttachmentSet(new Uri(uri3), "uri3_input1"),
            };

            List<AttachmentSet> outputAttachmentsForHandler1 = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_output")
            };

            List<AttachmentSet> outputAttachmentsForHandler2 = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri2), "uri2_output")
            };

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(outputAttachmentsForHandler1);
            mockAttachmentHandler2.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(outputAttachmentsForHandler2);

            // act
            var result = await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(inputAttachments[4]));
            Assert.IsTrue(result.Contains(outputAttachmentsForHandler1[0]));
            Assert.IsTrue(result.Contains(outputAttachmentsForHandler2[0]));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[0]) && c.Contains(inputAttachments[1])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[2]) && c.Contains(inputAttachments[3])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));

            VerifyMetrics(inputCount: 5, outputCount: 3);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnInitialAttachmentsThroughEventsHandler_IfOperationCancelled()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            ICollection<AttachmentSet> outputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_output")
            };

            var innerTaskCompletionSource = new TaskCompletionSource<object>();

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
                .Returns((string configElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                try
                {
                    for (int i = 0; i < 100; ++i)
                    {
                        Task.Delay(100).Wait();
                        Console.WriteLine($"Iteration: {i}");
                        logger.SendMessage(TestMessageLevel.Informational, $"Iteration: {i}");

                        cancellation.ThrowIfCancellationRequested();
                        progress.Report(i + 1);

                        if (i == 3)
                        {
                            cancellationTokenSource.Cancel();
                            Task.Delay(500).Wait();
                        }
                    }
                }
                finally
                {
                    innerTaskCompletionSource.TrySetResult(null);
                }

                return Task.FromResult(outputAttachments);
            });

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);
            Console.WriteLine("Attachments processing done");
            await innerTaskCompletionSource.Task;

            // assert
            VerifyCompleteEvent(true, false, inputAttachments[0]);
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Exactly(4));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgs(a, 1))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgs(a, 2))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgs(a, 3))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgs(a, 4))));
            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Informational, "Attachments processing was cancelled."));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1, status: "Canceled");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnInitialAttachments_IfOperationCancelled()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            ICollection<AttachmentSet> outputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_output")
            };

            var innerTaskCompletionSource = new TaskCompletionSource<object>();

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
                .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> p, IMessageLogger logger, CancellationToken cancellation) =>
            {
                try
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        Task.Delay(100).Wait();
                        Console.WriteLine($"Iteration: {i}");
                        logger.SendMessage(TestMessageLevel.Informational, $"Iteration: {i}");

                        cancellation.ThrowIfCancellationRequested();

                        if (i == 3)
                        {
                            cancellationTokenSource.Cancel();
                            Task.Delay(500).Wait();
                        }
                    }
                }
                finally
                {
                    innerTaskCompletionSource.TrySetResult(null);
                }

                return Task.FromResult(outputAttachments);
            });

            // act
            var result = await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], cancellationTokenSource.Token);
            Console.WriteLine("Attachments processing done");
            await innerTaskCompletionSource.Task;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(inputAttachments[0]));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            VerifyMetrics(inputCount: 1, outputCount: 1, status: "Canceled");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldReturnProperlySendProgressEvents_IfHandlersPropagesEvents()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input"),
                new AttachmentSet(new Uri(uri2), "uri2_input")
            };

            ICollection<AttachmentSet> outputAttachments1 = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_output")
            };

            ICollection<AttachmentSet> outputAttachments2 = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri2), "uri2_output")
            };

            var innerTaskCompletionSource = new TaskCompletionSource<object>();

            int counter = 0;
            mockEventsHandler.Setup(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>())).Callback(() =>
            {
                counter++;
                if (counter == 6)
                {
                    innerTaskCompletionSource.TrySetResult(null);
                }
            });

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(null, It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
                .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                progress.Report(25);
                progress.Report(50);
                progress.Report(75);
                logger.SendMessage(TestMessageLevel.Error, "error");
                progress.Report(100);
                return Task.FromResult(outputAttachments1);
            });

            mockAttachmentHandler2.Setup(h => h.ProcessAttachmentSetsAsync(null, It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
                .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                progress.Report(50);
                logger.SendMessage(TestMessageLevel.Informational, "info");
                progress.Report(100);
                return Task.FromResult(outputAttachments2);
            });

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, CancellationToken.None);

            // assert
            await innerTaskCompletionSource.Task;
            VerifyCompleteEvent(false, false, outputAttachments1.First(), outputAttachments2.First());
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>()), Times.Exactly(6));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgsForTwoHandlers(a, 1, 25, uri1))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgsForTwoHandlers(a, 1, 50, uri1))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgsForTwoHandlers(a, 1, 75, uri1))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgsForTwoHandlers(a, 1, 100, uri1))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgsForTwoHandlers(a, 2, 50, uri2))));
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => VerifyProgressArgsForTwoHandlers(a, 2, 100, uri2))));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUris());
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), CancellationToken.None));
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[1])), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), CancellationToken.None));

            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Informational, "info"));
            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Error, "error"));

            VerifyMetrics(inputCount: 2, outputCount: 2);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldNotFailIfRunsettingsIsNull()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input"),
                new AttachmentSet(new Uri(uri2), "uri2_input")
            };
            ICollection<AttachmentSet> outputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri2), "uri2_output")
            };
            mockAttachmentHandler2.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
            .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                // assert
                Assert.IsNull(configurationElement);
                return Task.FromResult(outputAttachments);
            });

            // act
            await manager.ProcessTestRunAttachmentsAsync(null, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ProcessTestRunAttachmentsAsync_ShouldFlowCorrectDataCollectorConfiguration(bool withConfig)
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input"),
                new AttachmentSet(new Uri(uri2), "uri2_input")
            };

            List<InvokedDataCollector> invokedDataCollectors = new List<InvokedDataCollector>
            {
                new InvokedDataCollector(new Uri(uri1),withConfig ? "friendlyNameA" : "friendlyNameB", typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false)
            };

            string runSettingsXml =
    $@"
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName=""{(withConfig ? "friendlyNameA" : "friendlyNameB")}"">
        <Configuration>
          <ConfigSample>Value</ConfigSample>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
";

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
                .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
                {
                    // assert
                    if (withConfig)
                    {
                        Assert.IsNotNull(configurationElement);
                        Assert.AreEqual("<ConfigSample>Value</ConfigSample>", configurationElement.InnerXml);
                    }
                    else
                    {
                        Assert.IsNull(configurationElement);
                    }

                    ICollection<AttachmentSet> outputAttachments = new List<AttachmentSet>
                    {
                    new AttachmentSet(new Uri(uri2), "uri2_output")
                    };
                    return Task.FromResult(outputAttachments);
                });

            // act
            await manager.ProcessTestRunAttachmentsAsync(runSettingsXml, mockRequestData.Object, inputAttachments, invokedDataCollectors, mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldNotConsumeAttachmentsIfProcessorFails()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input_1")
            };
            inputAttachments[0].Attachments.Add(UriDataAttachment.CreateFrom("file1", "Sample1"));
            inputAttachments[0].Attachments.Add(UriDataAttachment.CreateFrom("file2", "Sample2"));
            inputAttachments[0].Attachments.Add(UriDataAttachment.CreateFrom("file3", "Sample3"));


            mockAttachmentHandler1.Setup(h => h.GetExtensionUris()).Returns(new[] { new Uri(uri1) });
            mockAttachmentHandler2.Setup(h => h.GetExtensionUris()).Returns(new[] { new Uri(uri1) });

            bool firstProcessorFailed = false;

            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
            .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                try
                {
                    throw new Exception("Processor exception");
                }
                catch
                {
                    firstProcessorFailed = true;
                    throw;
                }
            });

            ICollection<AttachmentSet> output = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input_1")
            };
            output.Single().Attachments.Add(UriDataAttachment.CreateFrom("file4", "Merged"));

            mockAttachmentHandler2.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
            .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                // assert
                Assert.IsTrue(firstProcessorFailed);
                Assert.AreEqual(1, i1.Count);
                Assert.AreEqual(3, i1.Single().Attachments.Count);
                for (int i = 0; i < i1.Single().Attachments.Count; i++)
                {
                    Assert.AreEqual(inputAttachments.Single().Attachments[i], i1.Single().Attachments[i]);
                }

                return Task.FromResult(output);
            });

            // act
            await manager.ProcessTestRunAttachmentsAsync(null, mockRequestData.Object, inputAttachments, new List<InvokedDataCollector>(), mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Once());
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Once());
            VerifyCompleteEvent(false, false, output.ToArray());
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldNotConsumeAttachmentsIfAllProcessorsFail()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input_1"),
            };
            inputAttachments[0].Attachments.Add(UriDataAttachment.CreateFrom("file1", "Sample1"));
            inputAttachments[0].Attachments.Add(UriDataAttachment.CreateFrom("file2", "Sample2"));
            inputAttachments[0].Attachments.Add(UriDataAttachment.CreateFrom("file3", "Sample3"));


            mockAttachmentHandler1.Setup(h => h.GetExtensionUris()).Returns(new[] { new Uri(uri1) });
            mockAttachmentHandler2.Setup(h => h.GetExtensionUris()).Returns(new[] { new Uri(uri1) });

            bool firstProcessorFailed = false;
            mockAttachmentHandler1.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
            .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                try
                {
                    throw new Exception("Processor exception");
                }
                catch
                {
                    firstProcessorFailed = true;
                    throw;
                }
            });

            bool secondProcessorFailed = false;
            mockAttachmentHandler2.Setup(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()))
            .Returns((XmlElement configurationElement, ICollection<AttachmentSet> i1, IProgress<int> progress, IMessageLogger logger, CancellationToken cancellation) =>
            {
                try
                {
                    // assert
                    Assert.IsTrue(firstProcessorFailed);
                    Assert.AreEqual(1, i1.Count);
                    Assert.AreEqual(3, i1.Single().Attachments.Count);
                    for (int i = 0; i < i1.Single().Attachments.Count; i++)
                    {
                        Assert.AreEqual(inputAttachments.Single().Attachments[i], i1.Single().Attachments[i]);
                    }
                    throw new Exception("Processor exception");
                }
                catch
                {
                    secondProcessorFailed = true;
                    throw;
                }
            });

            // act
            await manager.ProcessTestRunAttachmentsAsync(null, mockRequestData.Object, inputAttachments, new List<InvokedDataCollector>(), mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            Assert.IsTrue(firstProcessorFailed && secondProcessorFailed);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Once());
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Once());
            VerifyCompleteEvent(false, false, inputAttachments.ToArray());
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsync_ShouldSkipAttachmentProcessorIfDoesNotSupportIncrementalProcessing()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input"),
                new AttachmentSet(new Uri(uri2), "uri2_input")
            };
            mockAttachmentHandler1.Setup(x => x.SupportsIncrementalProcessing).Returns(false);

            // act
            await manager.ProcessTestRunAttachmentsAsync(Constants.EmptyRunSettings, mockRequestData.Object, inputAttachments, new InvokedDataCollector[0], mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockAttachmentHandler1.Verify(h => h.GetExtensionUris(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Never);

            mockAttachmentHandler2.Verify(h => h.GetExtensionUris(), Times.Once);
            mockAttachmentHandler2.Verify(h => h.ProcessAttachmentSetsAsync(It.IsAny<XmlElement>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<IProgress<int>>(), It.IsAny<IMessageLogger>(), It.IsAny<CancellationToken>()), Times.Once);

            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        private void VerifyMetrics(int inputCount, int outputCount, string status = "Completed")
        {
            mockEventSource.Verify(s => s.TestRunAttachmentsProcessingStart(inputCount));
            mockEventSource.Verify(s => s.TestRunAttachmentsProcessingStop(outputCount));

            mockMetricsCollection.Verify(m => m.Add(TelemetryDataConstants.NumberOfAttachmentsSentForProcessing, inputCount));
            mockMetricsCollection.Verify(m => m.Add(TelemetryDataConstants.NumberOfAttachmentsAfterProcessing, outputCount));
            mockMetricsCollection.Verify(m => m.Add(TelemetryDataConstants.AttachmentsProcessingState, status));
            mockMetricsCollection.Verify(m => m.Add(TelemetryDataConstants.TimeTakenInSecForAttachmentsProcessing, It.IsAny<double>()));
        }

        private void VerifyCompleteEvent(bool isCanceled, bool containsError, params AttachmentSet[] expectedSets)
        {
            mockEventsHandler.Verify(h => h.HandleTestRunAttachmentsProcessingComplete(
                It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => a.IsCanceled == isCanceled && (a.Error != null) == containsError),
                It.Is<ICollection<AttachmentSet>>(c => c.Count == expectedSets.Length && expectedSets.All(e => c.Contains(e)))));
        }

        private bool VerifyProgressArgs(TestRunAttachmentsProcessingProgressEventArgs args, int progress)
        {
            Assert.AreEqual(1, args.CurrentAttachmentProcessorIndex);
            Assert.AreEqual(2, args.AttachmentProcessorsCount);
            Assert.AreEqual(1, args.CurrentAttachmentProcessorUris.Count);
            Assert.AreEqual(uri1, args.CurrentAttachmentProcessorUris.First().AbsoluteUri);
            return progress == args.CurrentAttachmentProcessorProgress;
        }

        private bool VerifyProgressArgsForTwoHandlers(TestRunAttachmentsProcessingProgressEventArgs args, long handlerIndex, long progress, string uri)
        {
            return progress == args.CurrentAttachmentProcessorProgress &&
                args.CurrentAttachmentProcessorIndex == handlerIndex &&
                args.CurrentAttachmentProcessorUris.First().AbsoluteUri == uri &&
                args.AttachmentProcessorsCount == 2;
        }
    }
}
