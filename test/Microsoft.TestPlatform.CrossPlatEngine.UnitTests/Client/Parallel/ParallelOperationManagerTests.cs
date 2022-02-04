// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;

[TestClass]
public class ParallelOperationManagerTests
{
    private MockParallelOperationManager _proxyParallelManager;

    [TestInitialize]
    public void InitializeTests()
    {
        Func<SampleConcurrentClass> sampleCreator =
            () => new SampleConcurrentClass();

        _proxyParallelManager = new MockParallelOperationManager(sampleCreator, 2, true);
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

        _proxyParallelManager = new MockParallelOperationManager(sampleCreator, 3, true);

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

        _proxyParallelManager = new MockParallelOperationManager(sampleCreator, 1, true);

        Assert.AreEqual(1, createdSampleClasses.Count, "Number of Concurrent Objects created should be 1");

        _proxyParallelManager.UpdateParallelLevel(4);

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

        _proxyParallelManager = new MockParallelOperationManager(sampleCreator, 4, true);

        Assert.AreEqual(4, createdSampleClasses.Count, "Number of Concurrent Objects created should be 4");

        int count = 0;
        _proxyParallelManager.DoActionOnAllConcurrentObjects(
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
        // At the beginning it should be equal to parallel level
        Assert.AreEqual(2, _proxyParallelManager.GetConcurrentManagersCount());

        _proxyParallelManager.AddManager(new SampleConcurrentClass(true), new SampleHandlerClass());

        Assert.AreEqual(3, _proxyParallelManager.GetConcurrentManagersCount());
        Assert.AreEqual(1, _proxyParallelManager.GetConcurrentManagerInstances().Count(m => m.CheckValue));
    }

    [TestMethod]
    public void RemoveManagerShouldRemoveAManagerFromConcurrentManagerList()
    {
        var manager = new SampleConcurrentClass(true);
        _proxyParallelManager.AddManager(manager, new SampleHandlerClass());

        Assert.AreEqual(3, _proxyParallelManager.GetConcurrentManagersCount());

        _proxyParallelManager.RemoveManager(manager);

        Assert.AreEqual(2, _proxyParallelManager.GetConcurrentManagersCount());
        Assert.AreEqual(0, _proxyParallelManager.GetConcurrentManagerInstances().Count(m => m.CheckValue));
    }

    [TestMethod]
    public void UpdateHandlerForManagerShouldAddNewHandlerIfNotexist()
    {
        var manager = new SampleConcurrentClass(true);
        _proxyParallelManager.UpdateHandlerForManager(manager, new SampleHandlerClass());

        Assert.AreEqual(3, _proxyParallelManager.GetConcurrentManagersCount());
        Assert.AreEqual(1, _proxyParallelManager.GetConcurrentManagerInstances().Count(m => m.CheckValue));
    }

    [TestMethod]
    public void UpdateHandlerForManagerShouldUpdateHandlerForGivenManager()
    {
        var manager = new SampleConcurrentClass(true);
        _proxyParallelManager.AddManager(manager, new SampleHandlerClass());

        // For current handler the value of variable CheckValue should be false;
        Assert.IsFalse(_proxyParallelManager.GetHandlerForGivenManager(manager).CheckValue);

        var newHandler = new SampleHandlerClass(true);

        // Update manager with new handler
        _proxyParallelManager.UpdateHandlerForManager(manager, newHandler);

        // It should not add new manager but update the current one
        Assert.AreEqual(3, _proxyParallelManager.GetConcurrentManagersCount());
        Assert.IsTrue(_proxyParallelManager.GetHandlerForGivenManager(manager).CheckValue);
    }

    private class MockParallelOperationManager : ParallelOperationManager<SampleConcurrentClass, SampleHandlerClass>
    {
        public MockParallelOperationManager(Func<SampleConcurrentClass> createNewClient, int parallelLevel, bool sharedHosts) :
            base(createNewClient, parallelLevel, sharedHosts)
        {
        }

        public void DoActionOnAllConcurrentObjects(Action<SampleConcurrentClass> action)
        {
            DoActionOnAllManagers(action, false);
        }
    }

    private class SampleConcurrentClass
    {
        public readonly bool CheckValue;
        public SampleConcurrentClass(bool value = false)
        {
            CheckValue = value;
        }
    }

    private class SampleHandlerClass
    {
        public readonly bool CheckValue;
        public SampleHandlerClass(bool value = false)
        {
            CheckValue = value;
        }

    }
}
