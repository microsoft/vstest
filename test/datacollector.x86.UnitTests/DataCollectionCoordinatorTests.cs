// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.x86;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectionCoordinatorTests
    {
        private DummyDataCollectionManager dummyDataCollectionManagerV1, dummyDataCollectionManagerV2;
        private DataCollectionCoordinator dataCollectionCoordinator;

        [TestInitialize]
        public void Initialize()
        {
            this.dummyDataCollectionManagerV1 = new DummyDataCollectionManager();
            this.dummyDataCollectionManagerV2 = new DummyDataCollectionManager();
            this.dataCollectionCoordinator = new DataCollectionCoordinator(new[] { dummyDataCollectionManagerV1, dummyDataCollectionManagerV2 });
        }

        [TestMethod]
        public void BeforeTestRunStartShouldReturnBeforeTestRunStartResult()
        {
            var envVars = new Dictionary<string, string>();
            envVars.Add("key", "value");
            this.dummyDataCollectionManagerV1.envVariables = envVars;
            this.dummyDataCollectionManagerV2.envVariables = new Dictionary<string, string>();

            var result = this.dataCollectionCoordinator.BeforeTestRunStart(settingsXml: string.Empty, resetDataCollectors: true, isRunStartingNow: true);

            Assert.IsTrue(this.dummyDataCollectionManagerV1.isLoadCollectorsInvoked);
            Assert.IsTrue(this.dummyDataCollectionManagerV2.isLoadCollectorsInvoked);
            Assert.IsTrue(this.dummyDataCollectionManagerV1.isSessionStartedInvoked);
            Assert.IsTrue(this.dummyDataCollectionManagerV2.isSessionStartedInvoked);
            Assert.AreEqual(1, result.EnvironmentVariables.Count);
            Assert.AreEqual(envVars.Keys.First(), result.EnvironmentVariables.Keys.First());
            Assert.AreEqual(envVars.Values.First(), result.EnvironmentVariables.Values.First());
        }

        [TestMethod]
        public void BeforeTestRunStartShouldLoadTwoDataCollectorsInParallel()
        {
            var envVars = new Dictionary<string, string>();
            envVars.Add("key", "value");
            this.dummyDataCollectionManagerV1.envVariables = envVars;
            this.dummyDataCollectionManagerV2.envVariables = new Dictionary<string, string>();

            var result = this.dataCollectionCoordinator.BeforeTestRunStart(settingsXml: string.Empty, resetDataCollectors: true, isRunStartingNow: true);

            // Verify the two collectors are invoked in parallel
            Assert.IsTrue(this.dummyDataCollectionManagerV1.ThreadId > 0);
            Assert.IsTrue(this.dummyDataCollectionManagerV2.ThreadId > 0);
            Assert.AreNotEqual(this.dummyDataCollectionManagerV1.ThreadId, this.dummyDataCollectionManagerV2.ThreadId);
        }

        [TestMethod]
        public void BeforeTestRunStartShouldReturnNullIfNoDataCollectorManagersAreProvided()
        {
            this.dataCollectionCoordinator = new DataCollectionCoordinator(null);

            var result = this.dataCollectionCoordinator.BeforeTestRunStart(settingsXml: string.Empty, resetDataCollectors: true, isRunStartingNow: true);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void BeforeTestRunStartShouldThrowExceptionIfExceptionIsThrownByDataCollectionManager()
        {
            this.dummyDataCollectionManagerV1.loadDataCollectorsThrowException = true;

            Assert.ThrowsException<AggregateException>(
                () =>
                {
                    var result = this.dataCollectionCoordinator.BeforeTestRunStart(settingsXml: string.Empty, resetDataCollectors: true, isRunStartingNow: true);
                });
        }

        [TestMethod]
        public void AfterTestRunEndShouldReturnAttachments()
        {
            Collection<AttachmentSet> attachments1 = new Collection<AttachmentSet>();
            AttachmentSet attachmentset1 = new AttachmentSet(new Uri("DataCollection://Attachment/v1"), "AttachmentV1");
            attachmentset1.Attachments.Add(new UriDataAttachment(new Uri("DataCollection://Attachment/v11"), "AttachmentV1-Attachment1"));
            attachments1.Add(attachmentset1);

            this.dummyDataCollectionManagerV1.attachments = attachments1;
            this.dummyDataCollectionManagerV2.attachments = attachments1;

            var result = this.dataCollectionCoordinator.AfterTestRunEnd(isCancelled: false);

            Assert.IsNotNull(result);
            Assert.IsTrue(this.dummyDataCollectionManagerV1.isSessionEndedInvoked);
            Assert.IsTrue(this.dummyDataCollectionManagerV2.isSessionEndedInvoked);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public void AfterTestRunEndShouldGetAttachmentsFromDataCollectorManagersInParallel()
        {
            Collection<AttachmentSet> attachments1 = new Collection<AttachmentSet>();
            AttachmentSet attachmentset1 = new AttachmentSet(new Uri("DataCollection://Attachment/v1"), "AttachmentV1");
            attachmentset1.Attachments.Add(new UriDataAttachment(new Uri("DataCollection://Attachment/v11"), "AttachmentV1-Attachment1"));
            attachments1.Add(attachmentset1);

            this.dummyDataCollectionManagerV1.attachments = attachments1;
            this.dummyDataCollectionManagerV2.attachments = attachments1;

            var result = this.dataCollectionCoordinator.AfterTestRunEnd(isCancelled: false);

            // Verify the two collectors are invoked in parallel
            Assert.IsTrue(this.dummyDataCollectionManagerV1.ThreadId > 0);
            Assert.IsTrue(this.dummyDataCollectionManagerV2.ThreadId > 0);
            Assert.AreNotEqual(this.dummyDataCollectionManagerV1.ThreadId, this.dummyDataCollectionManagerV2.ThreadId);
        }

        [TestMethod]
        public void AfterTestRunEndShouldReturnNullIfNoDataCollectorManagersAreProvided()
        {
            this.dataCollectionCoordinator = new DataCollectionCoordinator(null);

            var result = this.dataCollectionCoordinator.AfterTestRunEnd(isCancelled: true);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void AfterTestRunEndShouldThrowExceptionIfExceptionIsThrownByDataCollectionManager()
        {
            this.dummyDataCollectionManagerV1.sessionEndedThrowsException = true;

            Assert.ThrowsException<AggregateException>(
                () =>
                {
                    var result = this.dataCollectionCoordinator.AfterTestRunEnd(isCancelled: false);
                });
        }

        [TestMethod]
        public void DisposeShouldCallDisposeOfDataCollectionManagers()
        {
            this.dataCollectionCoordinator.Dispose();

            Assert.IsTrue(this.dummyDataCollectionManagerV1.isDisposedInvoked);
            Assert.IsTrue(this.dummyDataCollectionManagerV2.isDisposedInvoked);
        }

        [TestMethod]
        public void DisposeShouldDisposeResourcesIfNoDataCollectionManagersAreProvided()
        {
            this.dataCollectionCoordinator = new DataCollectionCoordinator(null);

            this.dataCollectionCoordinator.Dispose();
        }
    }

    internal class DummyDataCollectionManager : IDataCollectionManager
    {
        public bool isLoadCollectorsInvoked;
        public bool isSessionStartedInvoked;
        public bool isSessionEndedInvoked;
        public Dictionary<string, string> envVariables;
        public bool loadDataCollectorsThrowException;
        public Collection<AttachmentSet> attachments;
        public bool sessionEndedThrowsException;
        public bool isDisposedInvoked;
        public int ThreadId;

        public void Dispose()
        {
            this.isDisposedInvoked = true;
        }

        public Dictionary<string, string> LoadDataCollectors(RunSettings settingsXml)
        {
            this.ThreadId = Thread.CurrentThread.ManagedThreadId;

            if (this.loadDataCollectorsThrowException)
            {
                throw new Exception("DataCollectionManagerException");
            }

            this.isLoadCollectorsInvoked = true;
            return this.envVariables;

        }

        public void TestCaseStarted(TestCaseStartEventArgs testCaseStartEventArgs)
        {
            throw new NotImplementedException();
        }

        public Collection<AttachmentSet> SessionEnded(bool isCancelled)
        {
            this.ThreadId = Thread.CurrentThread.ManagedThreadId;

            if (this.sessionEndedThrowsException)
            {
                throw new Exception("DataCollectionManagerException");
            }

            this.isSessionEndedInvoked = true;
            return this.attachments;
        }

        public bool SessionStarted()
        {
            this.ThreadId = Thread.CurrentThread.ManagedThreadId;
            this.isSessionStartedInvoked = true;
            return true;
        }

        public Collection<AttachmentSet> TestCaseEnded(TestCase testCase, TestOutcome testOutcome)
        {
            throw new NotImplementedException();
        }
    }
}
