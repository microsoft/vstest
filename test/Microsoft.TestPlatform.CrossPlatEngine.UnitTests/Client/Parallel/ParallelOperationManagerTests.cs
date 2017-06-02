// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class ParallelOperationManagerTests
    {
        private MockParallelOperationManager proxyParallelManager;

        [TestMethod]
        public void AbstractProxyParallelManagerShouldCreateCorrectNumberOfConcurrentObjects()
        {
            var createdSampleClasses = new List<SampleConcurrentClass>();
            Func<SampleConcurrentClass> sampleCreator =
                () =>
                {
                    var sample = new SampleConcurrentClass();
                    createdSampleClasses.Add(sample);
                    return sample;
                };

            this.proxyParallelManager = new MockParallelOperationManager(sampleCreator, 3, true);

            Assert.AreEqual(3, createdSampleClasses.Count, "Number of Concurrent Objects created should be 3");
        }

        [TestMethod]
        public void AbstractProxyParallelManagerShouldUpdateToCorrectNumberOfConcurrentObjects()
        {
            var createdSampleClasses = new List<SampleConcurrentClass>();
            Func<SampleConcurrentClass> sampleCreator =
                () =>
                {
                    var sample = new SampleConcurrentClass();
                    createdSampleClasses.Add(sample);
                    return sample;
                };

            this.proxyParallelManager = new MockParallelOperationManager(sampleCreator, 1, true);

            Assert.AreEqual(1, createdSampleClasses.Count, "Number of Concurrent Objects created should be 1");

            this.proxyParallelManager.UpdateParallelLevel(4);

            Assert.AreEqual(4, createdSampleClasses.Count, "Number of Concurrent Objects created should be 4");
        }

        [TestMethod]
        public void DoActionOnConcurrentObjectsShouldCallAllObjects()
        {
            var createdSampleClasses = new List<SampleConcurrentClass>();
            Func<SampleConcurrentClass> sampleCreator =
                () =>
                {
                    var sample = new SampleConcurrentClass();
                    createdSampleClasses.Add(sample);
                    return sample;
                };

            this.proxyParallelManager = new MockParallelOperationManager(sampleCreator, 4, true);

            Assert.AreEqual(4, createdSampleClasses.Count, "Number of Concurrent Objects created should be 4");

            int count = 0;
            this.proxyParallelManager.DoActionOnAllConcurrentObjects(
                (sample) =>
                {
                    count++;
                    Assert.IsTrue(createdSampleClasses.Contains(sample), "Called object must be in the created list.");
                    // Make sure action is not called on same object multiple times
                    createdSampleClasses.Remove(sample);
                });

            Assert.AreEqual(4, count, "Number of Concurrent Objects called should be 4");

            Assert.AreEqual(0, createdSampleClasses.Count, "All concurrent objects must be called.");
        }

        private class MockParallelOperationManager : ParallelOperationManager<SampleConcurrentClass, SampleHandlerClass>
        {
            public MockParallelOperationManager(Func<SampleConcurrentClass> createNewClient, int parallelLevel, bool sharedHosts) : 
                base(createNewClient, parallelLevel, sharedHosts)
            {
            }

            public void DoActionOnAllConcurrentObjects(Action<SampleConcurrentClass> action)
            {
                this.DoActionOnAllManagers(action, false);
            }

            protected override void DisposeInstance(SampleConcurrentClass clientInstance)
            {
                clientInstance.IsDisposeCalled = true;
            }
        }

        private class SampleConcurrentClass
        {
            public bool IsDisposeCalled = false;
        }

        private class SampleHandlerClass
        {

        }
    }
}
