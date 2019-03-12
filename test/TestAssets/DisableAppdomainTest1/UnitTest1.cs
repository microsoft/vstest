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
            // make sure the last test was run in different process.
            var envvariable = Environment.GetEnvironmentVariable("{60BBC8A3-3AEB-40C4-AAD5-F6DA6305C6C7}");
            Assert.IsTrue(string.IsNullOrEmpty(envvariable));

            // make sure the appdomain is the one created by testhost
            var config = File.ReadAllText(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            Assert.AreEqual("TestHostAppDomain", AppDomain.CurrentDomain.FriendlyName);

            // make sure config file is honoured
            Assert.IsTrue(config.Contains("Assembly1"));

            // set env variable so that next test can assert if this is not carried forward
            Environment.SetEnvironmentVariable("{60BBC8A3-3AEB-40C4-AAD5-F6DA6305C6C7}", "{60BBC8A3-3AEB-40C4-AAD5-F6DA6305C6C7}");
        }
    }
}
