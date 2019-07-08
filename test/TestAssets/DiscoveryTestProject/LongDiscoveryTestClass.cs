using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DiscoveryTestProject3
{
    [TestClass]
    public class LongDiscoveryTestClass
    {
        [MyTestMethod]
        public void CustomTestMethod()
        {

        }
    }

    internal class MyTestMethodAttribute : TestMethodAttribute
    {
        public MyTestMethodAttribute()
        {
            Thread.Sleep(10000);
        }
    }
}
