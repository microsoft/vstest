using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var config = File.ReadAllText(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            Assert.AreEqual("TestHostAppDomain", AppDomain.CurrentDomain.FriendlyName);
            Assert.IsTrue(config.Contains("Assembly1"));
        }
    }
}
