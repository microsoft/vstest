// Copyright (c) Microsoft. All rights reserved.

namespace SampleUnitTestProject2
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;

    /// <summary>
    /// The unit test 1.
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// The passing test.
        /// </summary>
        [TestMethod]
        public void PassingTest()
        {
            Assert.AreEqual(2, 2);
        }

        /// <summary>
        /// The failing test.
        /// </summary>
        [TestMethod]
        public void FailingTest()
        {
#if NET46
            // current App domain should be write to file to test DisableAppDomain acceptance test.
            var appDomainFilePath = Path.Combine(Path.GetTempPath(), "appdomain_test.txt");
            File.WriteAllText(appDomainFilePath, "AppDomain FriendlyName: " + AppDomain.CurrentDomain.FriendlyName);
#endif
            Assert.AreEqual(2, 3);
        }

        /// <summary>
        /// The skipping test.
        /// </summary>
        [Ignore]
        [TestMethod]
        public void SkippingTest()
        {
        }
    }
}
