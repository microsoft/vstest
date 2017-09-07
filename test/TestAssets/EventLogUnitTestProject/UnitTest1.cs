// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EventLogUnitTestProject
{
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.VisualStudio.TestTools.UnitTesting;


    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            EventLog.WriteEntry("TestPlatform", "Application", EventLogEntryType.Error, 123);
        }

        [TestMethod]
        public void TestMethod2()
        {
            EventLog.WriteEntry("TestPlatform", "Application", EventLogEntryType.Error, 234);
        }

        [TestMethod]
        public void TestMethod3()
        {
            EventLog.WriteEntry("TestPlatform", "Application", EventLogEntryType.Error, 345);
        }
    }
}
