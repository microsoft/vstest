using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace timeout
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Thread.Sleep(10_000);
        }
    }
}
