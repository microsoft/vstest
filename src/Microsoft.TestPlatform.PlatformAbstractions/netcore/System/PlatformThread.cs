// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    public class PlatformThread : IThread
    {
        /// <inheritdoc/>
        /// <remarks> ApartmentState is not honored in netcoreapp1.0. </remarks>
        public void Run(Action action, PlatformApartmentState apartmentState, bool waitForCompletion)
        {
            if (apartmentState == PlatformApartmentState.STA)
            {
                throw new Exception("Currently STA Thread apartment state not supported for .NetCore");
            }

            if (action == null)
            {
                return;
            }

            Exception exThrown = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    exThrown = e;
                }
            });

            // ApartmentState is not supported in netcoreapp1.0.
            thread.IsBackground = true;
            thread.Start();
            if (waitForCompletion)
            {
                thread.Join();
                if (exThrown != null)
                {
                    throw exThrown;
                }
            }
        }
    }
}
