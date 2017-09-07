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
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 10);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 11);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 12);
        }

        [TestMethod]
        public void TestMethod2()
        {
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 20);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 21);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 22);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 23);
        }

        [TestMethod]
        public void TestMethod3()
        {
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 30);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 31);
            EventLog.WriteEntry("Application", "Application", EventLogEntryType.Error, 32);
        }
    }
}
