// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    /// <summary>
    /// Interface defining the parallel operation manager
    /// </summary>
    public interface IParallelOperationManager
    {
        /// <summary>
        /// Update the parallelism level of the manager
        /// </summary>
        /// <param name="parallelLevel">Parallelism level</param>
        void UpdateParallelLevel(int parallelLevel);
    }
}
