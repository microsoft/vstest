// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    public class PlatformThread : IThread
    {
        /// <inheritdoc/>
        public void Run(Action action, PlatformApartmentState apartmentState, bool waitForCompletion)
        {
            if (apartmentState == PlatformApartmentState.STA)
            {
                throw new NotSupportedThreadApartmentStateException("Currently STA Thread apartment state not supported for UAP10.0");
            }

            if (action == null)
            {
                return;
            }

            Task task = Task.Run(action);
            if (waitForCompletion)
            {
                task.Wait();
            }
        }
    }
}
