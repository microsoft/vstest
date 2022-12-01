// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Parallelize the execution
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace SerializeTestRunTestProject
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly object Lock = new object();

        private void LogFile(string testName)
        {
            lock (Lock)
            {
                string folderToLogTo = Environment.GetEnvironmentVariable("VSTEST_LOGFOLDER");
                File.AppendAllText(Path.Combine(folderToLogTo, $"TestHost_{Process.GetCurrentProcess().Id}.txt"), testName + "\n");
            }
        }

        [TestMethod]
        public void TestMethod1() => LogFile(nameof(TestMethod1));

        [TestMethod]
        public void TestMethod2() => LogFile(nameof(TestMethod2));

        [TestMethod]
        public void TestMethod3() => LogFile(nameof(TestMethod3));

        [TestMethod]
        public void TestMethod4() => LogFile(nameof(TestMethod4));

        [TestMethod]
        public void TestMethod5() => LogFile(nameof(TestMethod5));

        [TestMethod]
        public void TestMethod6() => LogFile(nameof(TestMethod6));

        [TestMethod]
        public void TestMethod7() => LogFile(nameof(TestMethod7));

        [TestMethod]
        public void TestMethod8() => LogFile(nameof(TestMethod8));

        [TestMethod]
        public void TestMethod9() => LogFile(nameof(TestMethod9));

        [TestMethod]
        public void TestMethod10() => LogFile(nameof(TestMethod10));
    }
}
