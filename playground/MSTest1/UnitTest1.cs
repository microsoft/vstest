using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest1
{
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }



        [TestMethod]
        public void TestMethod1()
        {
            TestContext.WriteLine("io");
        }
        [TestMethod]
        public void TestMethod2()
        {
            TestContext.WriteLine("io");
        }
        [TestMethod]
        public void TestMethod3()
        {
            TestContext.WriteLine("io");
        }
        [TestMethod]
        public void TestMethod4()
        {
            TestContext.WriteLine("io");
        }
        [TestMethod]
        public void TestMethod5()
        {
            TestContext.WriteLine("io");
        }
        [TestMethod]
        public void TestMethod6()
        {
            TestContext.WriteLine("io");
        }
    }
}
