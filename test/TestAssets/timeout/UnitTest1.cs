// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

#pragma warning disable IDE1006 // Naming Styles
namespace timeout
#pragma warning restore IDE1006 // Naming Styles
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Thread.Sleep(30_000);
        }

        [TestMethod]
        public void TestMethod2()
        {
            // Sleep long enough so the blame TestTimeout (10s) always fires
            // while the test is still running.
            Thread.Sleep(30_000);
        }
    }
}
