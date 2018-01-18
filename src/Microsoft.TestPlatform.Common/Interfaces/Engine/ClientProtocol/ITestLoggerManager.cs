// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Orchestrates logger operations for this engine.
    /// </summary>
    public interface ITestLoggerManager
    {
        /// <summary>
        /// Initialize loggers.
        /// </summary>
        void Initialize(string runSettings);
    }
}
