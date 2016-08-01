// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using System;

    /// <summary>
    /// Base interface for discovery and execution operations.
    /// </summary>
    public interface IProxyOperationManager : IDisposable
    {
        /// <summary>
        /// Ensure that the engine is ready for test operations.
        /// Usually includes starting up the test host process.
        /// </summary>
        /// <param name="testHostManager">
        /// Manager for the test host process
        /// </param>
        void Initialize(ITestHostManager testHostManager);

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        void Abort();
    }
}
