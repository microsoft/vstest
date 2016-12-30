// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;

namespace SampleUnitTestProject3
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void PassingTest()
        {
            Assert.AreEqual(2, 2);
        }

        [TestMethod]
        public async Task AsyncTestMethod()
        {
            await Task.CompletedTask;
        }
    }

    public class Class1
    {
        public void OverLoadededMethod()
        {
        }

        public void OverLoadededMethod(string name)
        {
        }
    }
}
