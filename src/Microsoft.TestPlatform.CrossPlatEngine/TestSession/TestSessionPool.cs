// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Represents the test session pool.
    /// </summary>
    public class TestSessionPool
    {
        private static object instanceLockObject = new object();
        private static volatile TestSessionPool instance;

        private object lockObject = new object();
        private Dictionary<TestSessionInfo, ProxyTestSessionManager> sessionPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSessionPool"/> class.
        /// </summary>
        private TestSessionPool()
        {
            this.sessionPool = new Dictionary<TestSessionInfo, ProxyTestSessionManager>();
        }

        /// <summary>
        /// Gets the test session pool instance.
        /// </summary>
        /// 
        /// <remarks>Thread-safe singleton pattern.</remarks>
        public static TestSessionPool Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLockObject)
                    {
                        if (instance == null)
                        {
                            instance = new TestSessionPool();
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Adds a session to the pool.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info object.</param>
        /// <param name="proxyManager">The proxy manager object.</param>
        /// 
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public bool AddSession(
            TestSessionInfo testSessionInfo,
            ProxyTestSessionManager proxyManager)
        {
            lock (this.lockObject)
            {
                // Check if the session info already exists.
                if (this.sessionPool.ContainsKey(testSessionInfo))
                {
                    return false;
                }

                // Adds an association between session info and proxy manager to the pool.
                this.sessionPool.Add(testSessionInfo, proxyManager);
                return true;
            }
        }

        /// <summary>
        /// Kills and removes a session from the pool.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info object.</param>
        /// 
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public bool KillSession(TestSessionInfo testSessionInfo)
        {
            // TODO (copoiena): What happens if some request is running for the current session ?
            // Should we stop the request as well ? Probably yes.
            ProxyTestSessionManager proxyManager = null;

            lock (this.lockObject)
            {
                // Check if the session info exists.
                if (!this.sessionPool.ContainsKey(testSessionInfo))
                {
                    return false;
                }

                // Remove the session from the pool.
                proxyManager = this.sessionPool[testSessionInfo];
                this.sessionPool.Remove(testSessionInfo);
            }

            // Kill the session.
            proxyManager.StopSession();
            return true;
        }

        /// <summary>
        /// Gets a reference to the proxy object from the session pool.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info object.</param>
        /// <param name="runSettings">The run settings.</param>
        /// 
        /// <returns>The proxy object.</returns>
        public ProxyOperationManager TakeProxy(
            TestSessionInfo testSessionInfo,
            string runSettings)
        {
            ProxyTestSessionManager sessionManager = null;
            lock (this.lockObject)
            {
                // Gets the session manager reference from the pool.
                sessionManager = this.sessionPool[testSessionInfo];
            }

            // Deque an actual proxy to do work.
            //
            // This can potentially throw, but let the caller handle this as it must recover from
            // this error by creating its own proxy.
            return sessionManager.DequeueProxy(runSettings);
        }

        /// <summary>
        /// Returns the proxy object to the session pool.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info object.</param>
        /// <param name="proxyId">The proxy id to be returned.</param>
        public void ReturnProxy(TestSessionInfo testSessionInfo, Guid proxyId)
        {
            ProxyTestSessionManager sessionManager = null;
            lock (this.lockObject)
            {
                // Gets the session manager reference from the pool.
                sessionManager = this.sessionPool[testSessionInfo];
            }

            try
            {
                // Try re-enqueueing the specified proxy.
                sessionManager.EnqueueProxy(proxyId);
            }
            catch (InvalidOperationException ex)
            {
                // If we are unable to re-enqueue the proxy we just eat up the exception here as
                // it is safe to proceed.
                // 
                // WARNING: This should not normally happen and it raises questions regarding the
                // test session pool operation and consistency.
                EqtTrace.Warning("TestSessionPool.ReturnProxy failed: {0}", ex.ToString());
            }
        }
    }
}
