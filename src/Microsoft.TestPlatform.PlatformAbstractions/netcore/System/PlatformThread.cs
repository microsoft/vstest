﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    public class PlatformThread : IThread
    {
        /// <inheritdoc/>
        public void Run(Action action, PlatformApartmentState apartmentState, bool waitForCompletion)
        {
            if (apartmentState == PlatformApartmentState.STA)
            {
                throw new ThreadApartmentStateNotSupportedException();
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
                    // Preserve the stacktrace when re-throwing the exception.
                    ExceptionDispatchInfo.Capture(exThrown).Throw();
                }
            }
        }
    }
}

#endif
