// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces
{
    using System;

    public interface IThread
    {
        /// <summary>
        /// Runs the action in a thread with given apartment state.
        /// </summary>
        /// <param name="action">The Action to be called.</param>
        /// <param name="apartmentState">The apartment state.</param>
        /// <param name="waitForCompletion"> True for running in Sync, False for running in Async</param>
        void Run(Action action, PlatformApartmentState apartmentState, bool waitForCompletion);
    }
}
