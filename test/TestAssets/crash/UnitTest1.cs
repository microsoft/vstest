using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace timeout
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // stack overflow
            Span<byte> s = stackalloc byte[int.MaxValue];
        }
    }
}
