// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class ParallelDataCollectionEventsHandlerTests
    {
        private const string uri1 = "datacollector://microsoft/some1/1.0";
        private const string uri2 = "datacollector://microsoft/some2/2.0";
        private const string uri3 = "datacollector://microsoft/some3/2.0";

        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IProxyExecutionManager> mockProxyExecutionManager;
        private readonly Mock<ITestRunEventsHandler> mockTestRunEventsHandler;
        private readonly Mock<IParallelProxyExecutionManager> mockParallelProxyExecutionManager;
        private readonly Mock<ITestRunAttachmentsProcessingManager> mockTestRunAttachmentsProcessingManager;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ParallelDataCollectionEventsHandler parallelDataCollectionEventsHandler;

        public ParallelDataCollectionEventsHandlerTests()
        {
            mockRequestData = new Mock<IRequestData>();
            mockProxyExecutionManager = new Mock<IProxyExecutionManager>();
            mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            mockParallelProxyExecutionManager = new Mock<IParallelProxyExecutionManager>();
            mockTestRunAttachmentsProcessingManager = new Mock<ITestRunAttachmentsProcessingManager>();
            cancellationTokenSource = new CancellationTokenSource();
            parallelDataCollectionEventsHandler = new ParallelDataCollectionEventsHandler(mockRequestData.Object, mockProxyExecutionManager.Object, mockTestRunEventsHandler.Object,
                mockParallelProxyExecutionManager.Object, new ParallelRunDataAggregator(), mockTestRunAttachmentsProcessingManager.Object, cancellationTokenSource.Token);

            mockParallelProxyExecutionManager.Setup(m => m.HandlePartialRunComplete(It.IsAny<IProxyExecutionManager>(), It.IsAny<TestRunCompleteEventArgs>(), It.IsAny<TestRunChangedEventArgs>(), It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<ICollection<string>>())).Returns(true);
        }

        [TestMethod]
        public void HandleTestRunComplete_ShouldCallProcessTestRunAttachmentsAsyncWithAttachmentsAndUseResults()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input1"),
                new AttachmentSet(new Uri(uri2), "uri2_input1"),
                new AttachmentSet(new Uri(uri3), "uri3_input1")
            };

            Collection<AttachmentSet> outputAttachments = new Collection<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input1")
            };

            mockTestRunAttachmentsProcessingManager.Setup(f => f.ProcessTestRunAttachmentsAsync(mockRequestData.Object, It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(outputAttachments));

            // act
            parallelDataCollectionEventsHandler.HandleTestRunComplete(new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromSeconds(1)), null, inputAttachments, null);

            // assert
            mockTestRunEventsHandler.Verify(h => h.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), It.IsAny<TestRunChangedEventArgs>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 1 && c.Contains(outputAttachments[0])), It.IsAny<ICollection<string>>()));
            mockTestRunAttachmentsProcessingManager.Verify(f => f.ProcessTestRunAttachmentsAsync(mockRequestData.Object, It.Is<ICollection<AttachmentSet>>(a => a.Count == 3), cancellationTokenSource.Token));
        }

        [TestMethod]
        public void HandleTestRunComplete_ShouldCallProcessTestRunAttachmentsAsyncWithAttachmentsAndNotUserResults_IfManagerReturnsNull()
        {
            // arrange
            List<AttachmentSet> inputAttachments = new List<AttachmentSet>
            {
                new AttachmentSet(new Uri(uri1), "uri1_input1"),
                new AttachmentSet(new Uri(uri2), "uri2_input1"),
                new AttachmentSet(new Uri(uri3), "uri3_input1")
            };

            mockTestRunAttachmentsProcessingManager.Setup(f => f.ProcessTestRunAttachmentsAsync(mockRequestData.Object, It.IsAny<ICollection<AttachmentSet>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult((Collection<AttachmentSet>)null));

            // act
            parallelDataCollectionEventsHandler.HandleTestRunComplete(new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromSeconds(1)), null, inputAttachments, null);

            // assert
            mockTestRunEventsHandler.Verify(h => h.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), It.IsAny<TestRunChangedEventArgs>(), It.Is<ICollection<AttachmentSet>>(c => c.Count == 3), It.IsAny<ICollection<string>>()));
            mockTestRunAttachmentsProcessingManager.Verify(f => f.ProcessTestRunAttachmentsAsync(mockRequestData.Object, It.Is<ICollection<AttachmentSet>>(a => a.Count == 3), cancellationTokenSource.Token));
        }
    }
}
