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
        public void Run(Action action, PlatformApartmentState apartmentState, bool waitForCompletion)
        {
            if (action == null)
            {
                return;
            }

            Exception exThrown = null;
            var thread = new System.Threading.Thread(() =>
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

            ApartmentState state = Enum.TryParse(apartmentState.ToString(), out state) ? state : ApartmentState.MTA;
            thread.SetApartmentState(state);
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
