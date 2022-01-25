﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InactivityTimerTests
    {
        private int callBackCount = 0;
        private readonly ManualResetEventSlim timerEvent = new();

        [TestMethod]
        public void InactivityTimerShouldResetAndCallbackWhenResetIsCalled()
        {
            var timer = new InactivityTimer(TimerCallback);
            timer.ResetTimer(TimeSpan.FromMilliseconds(1));
            timerEvent.Wait(1000);
            Assert.AreEqual(1, callBackCount, "Should have fired once.");
        }

        private void TimerCallback()
        {
            callBackCount++;
            timerEvent.Set();
        }
    }
}
