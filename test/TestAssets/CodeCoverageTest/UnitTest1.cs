using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeCoverageTest
{
    [TestClass]
    public class UnitTest1
    {
        private Logic logic;

        public UnitTest1()
        {
            this.logic = new Logic();
        }

        [TestMethod]
        public void TestAbs()
        {
            Assert.AreEqual(logic.Abs(0), 0);
            Assert.AreEqual(logic.Abs(-5), 5);
            Assert.AreEqual(logic.Abs(7), 7);
        }

        [TestMethod]
        public void TestSign()
        {
            Assert.AreEqual(logic.Sign(0), 0);
            Assert.AreEqual(logic.Sign(-5), -1);
            Assert.AreEqual(logic.Sign(7), 1);
        }

        [TestMethod]
        public void __CxxPureMSILEntry_Test()
        {
            Assert.AreEqual(logic.Abs(0), 0);
        }
    }
}
