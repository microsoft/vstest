// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;

namespace SampleUnitTestProject3
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;
    using System.Reflection;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void WorkingDirectoryTest()
        {
            Assert.AreEqual(Path.GetDirectoryName(typeof(UnitTest1).GetTypeInfo().Assembly.Location), Directory.GetCurrentDirectory());
        }
    }
}
