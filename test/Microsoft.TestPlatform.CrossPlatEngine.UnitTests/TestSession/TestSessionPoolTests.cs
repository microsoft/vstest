﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.TestSession
{
    using System;
    
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestSessionPoolTests
    {
        [TestMethod]
        public void AddSessionShouldSucceedIfTestSessionInfoIsUnique()
        {
            TestSessionPool.Instance = null;

            var testSessionInfo = new TestSessionInfo();
            var proxyTestSessionManager = new ProxyTestSessionManager(
                new StartTestSessionCriteria(),
                1,
                () => null);

            Assert.IsTrue(TestSessionPool.Instance.AddSession(testSessionInfo, proxyTestSessionManager));
            Assert.IsFalse(TestSessionPool.Instance.AddSession(testSessionInfo, proxyTestSessionManager));
        }

        [TestMethod]
        public void KillSessionShouldSucceedIfTestSessionExists()
        {
            TestSessionPool.Instance = null;

            var testSessionInfo = new TestSessionInfo();
            var mockProxyTestSessionManager = new Mock<ProxyTestSessionManager>(
                new StartTestSessionCriteria(),
                1,
                (Func<ProxyOperationManager>)(() => null));

            mockProxyTestSessionManager.SetupSequence(tsm => tsm.StopSession())
                .Returns(true)
                .Returns(false);

            Assert.IsFalse(TestSessionPool.Instance.KillSession(testSessionInfo));
            mockProxyTestSessionManager.Verify(tsm => tsm.StopSession(), Times.Never);

            Assert.IsTrue(TestSessionPool.Instance.AddSession(testSessionInfo, mockProxyTestSessionManager.Object));
            Assert.IsTrue(TestSessionPool.Instance.KillSession(testSessionInfo));
            mockProxyTestSessionManager.Verify(tsm => tsm.StopSession(), Times.Once);

            Assert.IsTrue(TestSessionPool.Instance.AddSession(testSessionInfo, mockProxyTestSessionManager.Object));
            Assert.IsFalse(TestSessionPool.Instance.KillSession(testSessionInfo));
            mockProxyTestSessionManager.Verify(tsm => tsm.StopSession(), Times.Exactly(2));
        }

        [TestMethod]
        public void TakeProxyShouldSucceedIfMatchingCriteriaAreCorrect()
        {
            TestSessionPool.Instance = null;

            var testSessionInfo = new TestSessionInfo();
            var mockProxyTestSessionManager = new Mock<ProxyTestSessionManager>(
                new StartTestSessionCriteria(),
                1,
                (Func<ProxyOperationManager>)(() => null));

            mockProxyTestSessionManager.SetupSequence(tsm => tsm.DequeueProxy(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Test Exception"))
                .Returns(new ProxyOperationManager(null, null, null));

            // Take proxy fails because test session is invalid.
            Assert.IsNull(TestSessionPool.Instance.TryTakeProxy(new TestSessionInfo(), string.Empty, string.Empty));
            mockProxyTestSessionManager.Verify(tsm => tsm.DequeueProxy(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            Assert.IsTrue(TestSessionPool.Instance.AddSession(testSessionInfo, mockProxyTestSessionManager.Object));

            // First TakeProxy fails because of throwing, see setup sequence.
            Assert.IsNull(TestSessionPool.Instance.TryTakeProxy(testSessionInfo, string.Empty, string.Empty));
            mockProxyTestSessionManager.Verify(tsm => tsm.DequeueProxy(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Second TakeProxy succeeds, see setup sequence.
            Assert.IsNotNull(TestSessionPool.Instance.TryTakeProxy(testSessionInfo, string.Empty, string.Empty));
            mockProxyTestSessionManager.Verify(tsm => tsm.DequeueProxy(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void ReturnProxyShouldSucceedIfProxyIdIsValid()
        {
            TestSessionPool.Instance = null;

            var testSessionInfo = new TestSessionInfo();
            var mockProxyTestSessionManager = new Mock<ProxyTestSessionManager>(
                new StartTestSessionCriteria(),
                1,
                (Func<ProxyOperationManager>)(() => null));

            mockProxyTestSessionManager.SetupSequence(tsm => tsm.EnqueueProxy(It.IsAny<int>()))
                .Throws(new ArgumentException("Test Exception"))
                .Throws(new InvalidOperationException("Test Exception"))
                .Returns(true);

            Assert.IsFalse(TestSessionPool.Instance.ReturnProxy(new TestSessionInfo(), 0));
            mockProxyTestSessionManager.Verify(tsm => tsm.EnqueueProxy(It.IsAny<int>()), Times.Never);

            Assert.IsTrue(TestSessionPool.Instance.AddSession(testSessionInfo, mockProxyTestSessionManager.Object));

            // Simulates proxy id not found (see setup sequence).
            Assert.ThrowsException<ArgumentException>(() => TestSessionPool.Instance.ReturnProxy(testSessionInfo, 0));
            mockProxyTestSessionManager.Verify(tsm => tsm.EnqueueProxy(It.IsAny<int>()), Times.Once);

            // Simulates proxy already available (see setup sequence).
            Assert.IsFalse(TestSessionPool.Instance.ReturnProxy(testSessionInfo, 0));
            mockProxyTestSessionManager.Verify(tsm => tsm.EnqueueProxy(It.IsAny<int>()), Times.Exactly(2));

            // EnqueueProxy call succeeds.
            Assert.IsTrue(TestSessionPool.Instance.ReturnProxy(testSessionInfo, 0));
            mockProxyTestSessionManager.Verify(tsm => tsm.EnqueueProxy(It.IsAny<int>()), Times.Exactly(3));
        }
    }
}
