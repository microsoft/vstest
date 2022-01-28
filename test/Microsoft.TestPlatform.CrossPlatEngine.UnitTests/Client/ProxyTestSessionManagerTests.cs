// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.Client;

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using VisualStudio.TestTools.UnitTesting;

using Moq;

[TestClass]
public class ProxyTestSessionManagerTests
{
    private readonly IList<string> _fakeTestSources = new List<string>() { @"C:\temp\FakeTestAsset.dll" };
    private readonly IList<string> _fakeTestMultipleSources = new List<string>() {
        @"C:\temp\FakeTestAsset1.dll",
        @"C:\temp\FakeTestAsset2.dll",
        @"C:\temp\FakeTestAsset3.dll",
        @"C:\temp\FakeTestAsset4.dll",
        @"C:\temp\FakeTestAsset5.dll",
        @"C:\temp\FakeTestAsset6.dll",
        @"C:\temp\FakeTestAsset7.dll",
        @"C:\temp\FakeTestAsset8.dll",
    };
    private readonly string _fakeRunSettings = "FakeRunSettings";
    private Mock<ITestSessionEventsHandler> _mockEventsHandler;

    [TestInitialize]
    public void TestInitialize()
    {
        TestSessionPool.Instance = null;

        _mockEventsHandler = new Mock<ITestSessionEventsHandler>();

        _mockEventsHandler.Setup(e => e.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>()))
            .Callback((TestSessionInfo tsi) => { });
    }

    [TestMethod]
    public void StartSessionShouldSucceedIfCalledOnlyOnce()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(true);

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // First call to StartSession should succeed.
        Assert.IsTrue(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                testSessionCriteria.Sources,
                testSessionCriteria.RunSettings),
            Times.Once);
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Once);

        // Second call to StartSession should fail.
        Assert.IsFalse(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                testSessionCriteria.Sources,
                testSessionCriteria.RunSettings),
            Times.Once);
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Once);
    }

    [TestMethod]
    public void StartSessionShouldSucceedWhenCalledWithMultipleSources()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(true);

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestMultipleSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // First call to StartSession should succeed.
        Assert.IsTrue(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                It.IsAny<IEnumerable<string>>(),
                testSessionCriteria.RunSettings),
            Times.Exactly(_fakeTestMultipleSources.Count));
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Once);
    }

    [TestMethod]
    public void StartSessionShouldFailIfProxyCreatorIsNull()
    {
        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, null);

        Assert.IsFalse(proxyManager.StartSession(_mockEventsHandler.Object));
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Never);
    }

    [TestMethod]
    public void StartSessionShouldFailIfSetupChannelReturnsFalse()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(false);
        mockProxyOperationManager.Setup(pom => pom.Close()).Callback(() => { });

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // Call fails because SetupChannel returns false.
        Assert.IsFalse(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>()),
            Times.Once);
        mockProxyOperationManager.Verify(pom => pom.Close(), Times.Never);
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Never);
    }

    [TestMethod]
    public void StartSessionShouldFailIfSetupChannelThrowsException()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Throws(new TestPlatformException("Dummy exception."));
        mockProxyOperationManager.Setup(pom => pom.Close()).Callback(() => { });

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // Call fails because SetupChannel returns false.
        Assert.IsFalse(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>()),
            Times.Once);
        mockProxyOperationManager.Verify(pom => pom.Close(), Times.Never);
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Never);
    }

    [TestMethod]
    public void StartSessionShouldFailIfAddSessionFails()
    {
        var mockTestSessionPool = new Mock<TestSessionPool>();
        mockTestSessionPool.Setup(tsp => tsp.AddSession(It.IsAny<TestSessionInfo>(), It.IsAny<ProxyTestSessionManager>()))
            .Returns(false);
        TestSessionPool.Instance = mockTestSessionPool.Object;

        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(true);
        mockProxyOperationManager.Setup(pom => pom.Close()).Callback(() => { });

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // Call to StartSession should fail because AddSession fails.
        Assert.IsFalse(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                testSessionCriteria.Sources,
                testSessionCriteria.RunSettings),
            Times.Once);
        mockProxyOperationManager.Verify(pom => pom.Close(), Times.Once);
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Never);
    }

    [TestMethod]
    public void StopSessionShouldSucceedIfCalledOnlyOnce()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(true);
        mockProxyOperationManager.Setup(pom => pom.Close()).Callback(() => { });

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // StartSession should succeed.
        Assert.IsTrue(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                testSessionCriteria.Sources,
                testSessionCriteria.RunSettings),
            Times.Once);
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Once);

        // First call to StopSession should succeed.
        Assert.IsTrue(proxyManager.StopSession());
        mockProxyOperationManager.Verify(pom => pom.Close(), Times.Once);

        // Second call to StopSession should fail.
        Assert.IsFalse(proxyManager.StopSession());
        mockProxyOperationManager.Verify(pom => pom.Close(), Times.Once);
    }

    [TestMethod]
    public void StopSessionShouldSucceedWhenCalledWithMultipleSources()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(true);
        mockProxyOperationManager.Setup(pom => pom.Close()).Callback(() => { });

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestMultipleSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // StartSession should succeed.
        Assert.IsTrue(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                It.IsAny<IEnumerable<string>>(),
                testSessionCriteria.RunSettings),
            Times.Exactly(testSessionCriteria.Sources.Count));
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Once);

        // First call to StopSession should succeed.
        Assert.IsTrue(proxyManager.StopSession());
        mockProxyOperationManager.Verify(pom => pom.Close(), Times.Exactly(testSessionCriteria.Sources.Count));
    }

    [TestMethod]
    public void DequeueProxyShouldSucceedIfIdentificationCriteriaAreMet()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(true);

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // StartSession should succeed.
        Assert.IsTrue(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                It.IsAny<IEnumerable<string>>(),
                testSessionCriteria.RunSettings),
            Times.Exactly(testSessionCriteria.Sources.Count));
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Once);

        // First call to DequeueProxy fails because of source mismatch.
        Assert.ThrowsException<InvalidOperationException>(() => proxyManager.DequeueProxy(
            @"C:\temp\FakeTestAsset2.dll",
            testSessionCriteria.RunSettings));

        // Second call to DequeueProxy fails because of runsettings mismatch.
        Assert.ThrowsException<InvalidOperationException>(() => proxyManager.DequeueProxy(
            testSessionCriteria.Sources[0],
            "DummyRunSettings"));

        // Third call to DequeueProxy succeeds.
        Assert.AreEqual(proxyManager.DequeueProxy(
                testSessionCriteria.Sources[0],
                testSessionCriteria.RunSettings),
            mockProxyOperationManager.Object);

        // Fourth call to DequeueProxy fails because proxy became unavailable following successful deque.
        Assert.ThrowsException<InvalidOperationException>(() => proxyManager.DequeueProxy(
            testSessionCriteria.Sources[0],
            testSessionCriteria.RunSettings));
    }

    [TestMethod]
    public void EnqueueProxyShouldSucceedIfIdentificationCriteriaAreMet()
    {
        var mockProxyOperationManager = new Mock<ProxyOperationManager>(null, null, null);
        mockProxyOperationManager.Setup(pom => pom.SetupChannel(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(true);

        var testSessionCriteria = ProxyTestSessionManagerTests.CreateTestSession(_fakeTestSources, _fakeRunSettings);
        var proxyManager = ProxyTestSessionManagerTests.CreateProxy(testSessionCriteria, mockProxyOperationManager.Object);

        // Validate sanity checks.
        Assert.ThrowsException<ArgumentException>(() => proxyManager.EnqueueProxy(-1));
        Assert.ThrowsException<ArgumentException>(() => proxyManager.EnqueueProxy(1));

        // StartSession should succeed.
        Assert.IsTrue(proxyManager.StartSession(_mockEventsHandler.Object));
        mockProxyOperationManager.Verify(pom => pom.SetupChannel(
                It.IsAny<IEnumerable<string>>(),
                testSessionCriteria.RunSettings),
            Times.Exactly(testSessionCriteria.Sources.Count));
        _mockEventsHandler.Verify(eh => eh.HandleStartTestSessionComplete(
                It.IsAny<TestSessionInfo>()),
            Times.Once);

        // Call throws exception because proxy is already available.
        Assert.ThrowsException<InvalidOperationException>(() => proxyManager.EnqueueProxy(0));

        // Call succeeds.
        Assert.AreEqual(proxyManager.DequeueProxy(
                testSessionCriteria.Sources[0],
                testSessionCriteria.RunSettings),
            mockProxyOperationManager.Object);
        Assert.IsTrue(proxyManager.EnqueueProxy(0));
    }

    private static StartTestSessionCriteria CreateTestSession(IList<string> sources, string runSettings)
    {
        return new StartTestSessionCriteria()
        {
            Sources = sources,
            RunSettings = runSettings
        };
    }

    private static ProxyTestSessionManager CreateProxy(
        StartTestSessionCriteria testSessionCriteria,
        ProxyOperationManager proxyOperationManager)
    {
        return new ProxyTestSessionManager(
            testSessionCriteria,
            testSessionCriteria.Sources.Count,
            () => proxyOperationManager);
    }
}