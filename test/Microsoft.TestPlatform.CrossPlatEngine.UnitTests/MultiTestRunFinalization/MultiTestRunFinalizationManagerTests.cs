// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.MultiTestRunFinalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.MultiTestRunFinalization;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;    

    [TestClass]
    public class MultiTestRunFinalizationManagerTests
    {
        private const string uri1 = "datacollector://microsoft/some1/1.0";
        private const string uri2 = "datacollector://microsoft/some2/2.0";
        private const string uri3 = "datacollector://microsoft/some3/2.0";

        private readonly Mock<ITestPlatformEventSource> mockEventSource;
        private readonly Mock<IDataCollectorAttachments> mockAttachmentHandler1;
        private readonly Mock<IDataCollectorAttachments> mockAttachmentHandler2;
        private readonly Mock<IMultiTestRunFinalizationEventsHandler> mockEventsHandler;
        private readonly MultiTestRunFinalizationManager manager;
        private readonly CancellationTokenSource cancellationTokenSource;

        public MultiTestRunFinalizationManagerTests()
        {
            mockEventSource = new Mock<ITestPlatformEventSource>();
            mockAttachmentHandler1 = new Mock<IDataCollectorAttachments>();
            mockAttachmentHandler2 = new Mock<IDataCollectorAttachments>();
            mockEventsHandler = new Mock<IMultiTestRunFinalizationEventsHandler>();

            mockAttachmentHandler1.Setup(h => h.GetExtensionUri()).Returns(new Uri(uri1));
            mockAttachmentHandler2.Setup(h => h.GetExtensionUri()).Returns(new Uri(uri2));

            manager = new MultiTestRunFinalizationManager(mockEventSource.Object, mockAttachmentHandler1.Object, mockAttachmentHandler2.Object);

            cancellationTokenSource = new CancellationTokenSource();
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNoAttachmentsThroughEventsHandler_IfNoAttachmentsOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>();

            // act
            await manager.FinalizeMultiTestRunAsync(inputAttachments, mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockEventsHandler.Verify(h => h.HandleMultiTestRunFinalizationComplete(It.Is<ICollection<AttachmentSet>>(c => c.Count == 0)));
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(0));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNoAttachments_IfNoAttachmentsOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>();

            // act
            var result = await manager.FinalizeMultiTestRunAsync(inputAttachments, cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(0, result.Count);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(0));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturn1NotProcessedAttachmentThroughEventsHandler_If1NotRelatedAttachmentOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri3), "uri3_input")
            };

            // act
            await manager.FinalizeMultiTestRunAsync(inputAttachments, mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockEventsHandler.Verify(h => h.HandleMultiTestRunFinalizationComplete(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0]))));
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(1));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturn1NotProcessedAttachment_If1NotRelatedAttachmentOnInput()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri3), "uri3_input")
            };

            // act
            var result = await manager.FinalizeMultiTestRunAsync(inputAttachments, cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(inputAttachments[0]));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(1));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturn1ProcessedAttachmentThroughEventsHandler_IfRelatedAttachmentOnInput()
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

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(outputAttachments);

            // act
            await manager.FinalizeMultiTestRunAsync(inputAttachments, mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockEventsHandler.Verify(h => h.HandleMultiTestRunFinalizationComplete(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(outputAttachments[0]))));
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(1));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturn1ProcessedAttachment_IfRelatedAttachmentOnInput()
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

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(outputAttachments);

            // act
            var result = await manager.FinalizeMultiTestRunAsync(inputAttachments, cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(outputAttachments[0]));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(1));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNullThroughEventsHandler_IfRelatedAttachmentOnInputButHandlerThrowsException()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Throws(new Exception("exception message"));

            // act
            await manager.FinalizeMultiTestRunAsync(inputAttachments, mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockEventsHandler.Verify(h => h.HandleMultiTestRunFinalizationComplete(null));
            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Error, "exception message"), Times.Once);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNull_IfRelatedAttachmentOnInputButHandlerThrowsException()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Throws(new Exception("exception message"));

            // act
            var result = await manager.FinalizeMultiTestRunAsync(inputAttachments, cancellationTokenSource.Token);

            // assert
            Assert.IsNull(result);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNullThroughEventsHandler_IfOperationIsCancelled()
        {
            // arrange
            cancellationTokenSource.Cancel();
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            // act
            await manager.FinalizeMultiTestRunAsync(inputAttachments, mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockEventsHandler.Verify(h => h.HandleMultiTestRunFinalizationComplete(null));
            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Error, "The operation was canceled."), Times.Once);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNull_IfOperationIsCancelled()
        {
            // arrange
            cancellationTokenSource.Cancel();
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input")
            };

            // act
            var result = await manager.FinalizeMultiTestRunAsync(inputAttachments, cancellationTokenSource.Token);

            // assert
            Assert.IsNull(result);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnProcessedAttachmentsThroughEventsHandler_IfRelatedAttachmentsOnInput()
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

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(outputAttachmentsForHandler1);
            mockAttachmentHandler2.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(outputAttachmentsForHandler2);

            // act
            await manager.FinalizeMultiTestRunAsync(inputAttachments, mockEventsHandler.Object, cancellationTokenSource.Token);

            // assert
            mockEventsHandler.Verify(h => h.HandleMultiTestRunFinalizationComplete(It.Is<ICollection<AttachmentSet>>(
                c => c.Count == 3 &&
                c.Contains(inputAttachments[4]) &&
                c.Contains(outputAttachmentsForHandler1.First()) &&
                c.Contains(outputAttachmentsForHandler2.First()))));
            mockEventsHandler.Verify(h => h.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(5));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(3));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[0]) && c.Contains(inputAttachments[1])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[2]) && c.Contains(inputAttachments[3])), cancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnProcessedAttachments_IfRelatedAttachmentsOnInput()
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

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(outputAttachmentsForHandler1);
            mockAttachmentHandler2.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(outputAttachmentsForHandler2);

            // act
            var result = await manager.FinalizeMultiTestRunAsync(inputAttachments, cancellationTokenSource.Token);

            // assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(inputAttachments[4]));
            Assert.IsTrue(result.Contains(outputAttachmentsForHandler1[0]));
            Assert.IsTrue(result.Contains(outputAttachmentsForHandler2[0]));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(5));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(3));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[0]) && c.Contains(inputAttachments[1])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 2 && c.Contains(inputAttachments[2]) && c.Contains(inputAttachments[3])), cancellationTokenSource.Token));
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNullThroughEventsHandler_IfOperationCancelled()
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

            var innerTaskCompletionSource = new TaskCompletionSource<object>();

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns((ICollection<AttachmentSet> i1, CancellationToken cancellation) =>
            {
                try
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        Task.Delay(100).Wait();
                        Console.WriteLine($"Iteration: {i}");

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
                
                return outputAttachments;
            });

            // act
            await manager.FinalizeMultiTestRunAsync(inputAttachments, mockEventsHandler.Object, cancellationTokenSource.Token);
            Console.WriteLine("Finalization done");
            await innerTaskCompletionSource.Task;

            // assert
            mockEventsHandler.Verify(h => h.HandleMultiTestRunFinalizationComplete(null));
            mockEventsHandler.Verify(h => h.HandleLogMessage(TestMessageLevel.Informational, "Finalization was cancelled."));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task FinalizeMultiTestRunAsync_ShouldReturnNull_IfOperationCancelled()
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

            var innerTaskCompletionSource = new TaskCompletionSource<object>();

            mockAttachmentHandler1.Setup(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns((ICollection<AttachmentSet> i1, CancellationToken cancellation) =>
            {
                for (int i = 0; i < 1000; ++i)
                {
                    Task.Delay(100).Wait();
                    Console.WriteLine($"Iteration: {i}");

                    cancellation.ThrowIfCancellationRequested();

                    if (i == 3)
                    {                        
                        cancellationTokenSource.Cancel();
                        Task.Delay(500).Wait();
                    }
                }

                innerTaskCompletionSource.TrySetResult(null);
                return outputAttachments;
            });

            // act
            var result = await manager.FinalizeMultiTestRunAsync(inputAttachments, cancellationTokenSource.Token);
            Console.WriteLine("Finalization done");
            await innerTaskCompletionSource.Task;

            // assert
            Assert.IsNull(result);
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStart(1));
            mockEventSource.Verify(s => s.MultiTestRunFinalizationStop(0));
            mockAttachmentHandler1.Verify(h => h.GetExtensionUri());
            mockAttachmentHandler2.Verify(h => h.GetExtensionUri(), Times.Never);
            mockAttachmentHandler1.Verify(h => h.HandleDataCollectionAttachmentSets(It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(inputAttachments[0])), cancellationTokenSource.Token));
            mockAttachmentHandler2.Verify(h => h.HandleDataCollectionAttachmentSets(It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
