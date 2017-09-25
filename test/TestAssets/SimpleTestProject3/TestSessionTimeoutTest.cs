// Copyright (c) Microsoft. All rights reserved.

namespace SampleUnitTestProject3
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestSessionTimeoutTest
    {
        [TestMethod]
        public void TestWhichTakeSomeTime11()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }

        [TestMethod]
        public void TestWhichTakeSomeTime12()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }

        [TestMethod]
        public void TestWhichTakeSomeTime13()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }

        [TestMethod]
        public void TestWhichTakeSomeTime4()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }

        [TestMethod]
        public void TestWhichTakeSomeTime5()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }

        [TestMethod]
        public void TestWhichTakeSomeTime6()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }
    }
}
