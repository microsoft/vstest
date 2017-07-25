// Copyright (c) Microsoft. All rights reserved.

namespace SampleUnitTestProject3
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestSessionTimeoutTest
    {
        [TestMethod]
        public void TestWhichTakeSomeTime1()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }

        [TestMethod]
        public void TestWhichTakeSomeTime2()
        {
            System.Threading.Thread.Sleep(3 * 1000);
        }

        [TestMethod]
        public void TestWhichTakeSomeTime3()
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
