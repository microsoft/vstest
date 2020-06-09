// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    /// <summary>
    /// IMultiTestRunsFinalizationRequest 
    /// </summary>
    public interface IMultiTestRunsFinalizationRequest : IRequest
    {
        /// <summary>
        /// Starts tests discovery async.
        /// </summary>
        void FinalizeMultiTestRunsAsync();

        /// <summary>
        /// Aborts the discovery request
        /// </summary>
        void Abort();
    }
}
