// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

namespace DiscoveryTestProject3
{
    [TestClass]
    public class LongDiscoveryTestClass
    {
        // This is for discovery cancellation test.
        // 20 tests below to be discovered until we reach the X_ Y_ Z_LongDiscoveryTestMethod which haver attribute that
        // takes a very long time to create, which prolongs the discovery time and keeps us discovering while we
        // are cancelling the discovery from the test.
        #region 20 empty tests

        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod]
        public void TestMethod2()
        {
        }

        [TestMethod]
        public void TestMethod3()
        {
        }

        [TestMethod]
        public void TestMethod4()
        {
        }

        [TestMethod]
        public void TestMethod5()
        {
        }

        [TestMethod]
        public void TestMethod6()
        {
        }

        [TestMethod]
        public void TestMethod7()
        {
        }

        [TestMethod]
        public void TestMethod8()
        {
        }

        [TestMethod]
        public void TestMethod9()
        {
        }

        [TestMethod]
        public void TestMethod10()
        {
        }

        [TestMethod]
        public void TestMethod11()
        {
        }

        [TestMethod]
        public void TestMethod12()
        {
        }

        [TestMethod]
        public void TestMethod13()
        {
        }

        [TestMethod]
        public void TestMethod14()
        {
        }

        [TestMethod]
        public void TestMethod15()
        {
        }

        [TestMethod]
        public void TestMethod16()
        {
        }

        [TestMethod]
        public void TestMethod17()
        {
        }

        [TestMethod]
        public void TestMethod18()
        {
        }

        [TestMethod]
        public void TestMethod19()
        {
        }

        [TestMethod]
        public void TestMethod20()
        {
        }

        #endregion

        // X_ to make it discover last.
        [TestMethodWithDelay]
        public void X_LongDiscoveryTestMethod()
        {

        }

        // Y_ to make it discover last.
        [TestMethodWithDelay]
        public void Y_LongDiscoveryTestMethod()
        {

        }

        // Z_ to make it discover last.
        [TestMethodWithDelay]
        public void Z_LongDiscoveryTestMethod()
        {

        }
    }

    internal class TestMethodWithDelayAttribute : TestMethodAttribute
    {
        public TestMethodWithDelayAttribute()
        {
            // This will be multiplied by 3 because the framework will internally create this
            // attribute 3 times. And by another 3 because we have 3 slow tests.
            Thread.Sleep(500);
        }
    }
}
