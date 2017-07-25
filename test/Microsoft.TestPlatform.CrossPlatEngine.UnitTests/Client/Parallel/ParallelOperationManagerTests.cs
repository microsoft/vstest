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

        [TestInitialize]
        public void InitializeTests()
        {
            Func<SampleConcurrentClass> sampleCreator =
                () =>
                {
                    return new SampleConcurrentClass();
                };

            this.proxyParallelManager = new MockParallelOperationManager(sampleCreator, 2, true);
        }

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

        [TestMethod]
        public void AddManagerShouldAddAManagerWithHandlerInConcurrentManagerList()
        {
            // At the begining it should be equal to parallel level
            Assert.AreEqual(2, this.proxyParallelManager.GetConcurrentManagersCount());

            this.proxyParallelManager.AddManager(new SampleConcurrentClass(true), new SampleHandlerClass());

            Assert.AreEqual(3, this.proxyParallelManager.GetConcurrentManagersCount());
            Assert.AreEqual(1, this.proxyParallelManager.GetConcurrentManagerInstances().Where(m => m.CheckValue == true).Count());
        }

        [TestMethod]
        public void RemoveManagerShouldRemoveAManagerFromConcurrentManagerList()
        {
            var manager = new SampleConcurrentClass(true);
            this.proxyParallelManager.AddManager(manager, new SampleHandlerClass());

            Assert.AreEqual(3, this.proxyParallelManager.GetConcurrentManagersCount());

            this.proxyParallelManager.RemoveManager(manager);

            Assert.AreEqual(2, this.proxyParallelManager.GetConcurrentManagersCount());
            Assert.AreEqual(0, this.proxyParallelManager.GetConcurrentManagerInstances().Where(m => m.CheckValue == true).Count());
        }

        [TestMethod]
        public void UpdateHandlerForManagerShouldAddNewHandlerIfNotexist()
        {
            var manager = new SampleConcurrentClass(true);
            this.proxyParallelManager.UpdateHandlerForManager(manager, new SampleHandlerClass());

            Assert.AreEqual(3, this.proxyParallelManager.GetConcurrentManagersCount());
            Assert.AreEqual(1, this.proxyParallelManager.GetConcurrentManagerInstances().Where(m => m.CheckValue == true).Count());
        }

        [TestMethod]
        public void UpdateHandlerForManagerShouldUpdateHandlerForGivenManager()
        {
            var manager = new SampleConcurrentClass(true);
            this.proxyParallelManager.AddManager(manager, new SampleHandlerClass());

            // For current handler the value of variable CheckValue should be false;
            Assert.IsFalse(this.proxyParallelManager.GetHandlerForGivenManager(manager).CheckValue);

            var newHandler = new SampleHandlerClass(true);

            // Update manager with new handler
            this.proxyParallelManager.UpdateHandlerForManager(manager, newHandler);

            // It should not add new manager but update the current one
            Assert.AreEqual(3, this.proxyParallelManager.GetConcurrentManagersCount());
            Assert.IsTrue(this.proxyParallelManager.GetHandlerForGivenManager(manager).CheckValue);
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
            public bool CheckValue;
            public SampleConcurrentClass(bool value=false)
            {
                this.CheckValue = value;
            }
        }

        private class SampleHandlerClass
        {
            public bool CheckValue;
            public SampleHandlerClass(bool value=false)
            {
                this.CheckValue = value;
            }

        }
    }
}
