﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Threading;

    public class InactivityTimer : IInactivityTimer
    {
        private readonly Timer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="InactivityTimer"/> class.
        /// Creates a new timer with infinite timeout
        /// </summary>
        /// <param name="timerCallback">Function to callback once the timer times out.</param>
        public InactivityTimer(Action timerCallback)
        {
            timer = new Timer((object state) => timerCallback());
        }

        /// <inheritdoc />
        public void ResetTimer(TimeSpan inactivityTimespan)
        {
            timer.Change(inactivityTimespan, TimeSpan.FromMilliseconds(-1));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            timer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
