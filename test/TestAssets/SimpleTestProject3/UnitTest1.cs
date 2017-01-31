// Copyright (c) Microsoft. All rights reserved.

namespace SampleUnitTestProject3
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
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

        [TestMethod]
        public void SleepForSomeTimeTest()
        {
            Console.Error.WriteLine("Std Error Message");
            Environment.Exit(1);
        }
    }
}
