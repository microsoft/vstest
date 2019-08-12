using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace EnvironmentVariablesTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var variable = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            Assert.AreEqual(variable, @"C:\ProgramFiles\dotnet");
        }
    }
}
