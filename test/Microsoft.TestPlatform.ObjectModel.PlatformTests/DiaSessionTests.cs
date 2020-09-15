// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.PlatformTests
{
    using System;
    using System.Diagnostics;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiaSessionTests : IntegrationTestBase
    {
        private const string NET451 = "net451";
        private const string NETCOREAPP21 = "netcoreapp2.1";

        public static string GetAndSetTargetFrameWork(IntegrationTestEnvironment testEnvironment)
        {
            var currentTargetFrameWork = testEnvironment.TargetFramework;
#if NETFRAMEWORK
            testEnvironment.TargetFramework = NET451;
#else
            testEnvironment.TargetFramework = NETCOREAPP21;
#endif
            return currentTargetFrameWork;
        }

        [TestMethod]
        public void GetNavigationDataShouldReturnCorrectFileNameAndLineNumber()
        {
            var currentTargetFrameWork = GetAndSetTargetFrameWork(this.testEnvironment);
            var assemblyPath = GetAssetFullPath("SimpleClassLibrary.dll");

            DiaSession diaSession = new DiaSession(assemblyPath);
            DiaNavigationData diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1", "PassingTest");

            Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
            StringAssert.EndsWith(diaNavigationData.FileName, @"\SimpleClassLibrary\Class1.cs");

            this.ValidateMinLineNumber(12, diaNavigationData.MinLineNumber);
            Assert.AreEqual(14, diaNavigationData.MaxLineNumber, "Incorrect max line number");

            this.testEnvironment.TargetFramework = currentTargetFrameWork;
        }

        [TestMethod]
        public void GetNavigationDataShouldReturnCorrectDataForAsyncMethod()
        {
            var currentTargetFrameWork = GetAndSetTargetFrameWork(this.testEnvironment);
            var assemblyPath = this.GetAssetFullPath("SimpleClassLibrary.dll");

            DiaSession diaSession = new DiaSession(assemblyPath);
            DiaNavigationData diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1+<AsyncTestMethod>d__1", "MoveNext");

            Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
            StringAssert.EndsWith(diaNavigationData.FileName, @"\SimpleClassLibrary\Class1.cs");

            this.ValidateMinLineNumber(17, diaNavigationData.MinLineNumber);
            Assert.AreEqual(19, diaNavigationData.MaxLineNumber, "Incorrect max line number");

            this.testEnvironment.TargetFramework = currentTargetFrameWork;
        }

        [TestMethod]
        public void GetNavigationDataShouldReturnCorrectDataForOverLoadedMethod()
        {
            var currentTargetFrameWork = GetAndSetTargetFrameWork(this.testEnvironment);
            var assemblyPath = this.GetAssetFullPath("SimpleClassLibrary.dll");

            DiaSession diaSession = new DiaSession(assemblyPath);
            DiaNavigationData diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1", "OverLoadedMethod");

            Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
            StringAssert.EndsWith(diaNavigationData.FileName, @"\SimpleClassLibrary\Class1.cs");

            // Weird why DiaSession is now returning the first overloaded method
            // as compared to before when it used to return second method
            this.ValidateLineNumbers(diaNavigationData.MinLineNumber, diaNavigationData.MaxLineNumber);

            this.testEnvironment.TargetFramework = currentTargetFrameWork;
        }

        [TestMethod]
        public void GetNavigationDataShouldReturnNullForNotExistMethodNameOrNotExistTypeName()
        {
            var currentTargetFrameWork = GetAndSetTargetFrameWork(this.testEnvironment);
            var assemblyPath = GetAssetFullPath("SimpleClassLibrary.dll");

            DiaSession diaSession = new DiaSession(assemblyPath);

            // Not exist method name
            DiaNavigationData diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.Class1", "NotExistMethod");
            Assert.IsNull(diaNavigationData);

            // Not Exist Type name
            diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.NotExistType", "PassingTest");
            Assert.IsNull(diaNavigationData);

            this.testEnvironment.TargetFramework = currentTargetFrameWork;
        }

        [TestMethod]
        public void DiaSessionPerfTest()
        {
            var currentTargetFrameWork = GetAndSetTargetFrameWork(this.testEnvironment);
            var assemblyPath = this.GetAssetFullPath("SimpleClassLibrary.dll");

            var watch = Stopwatch.StartNew();
            DiaSession diaSession = new DiaSession(assemblyPath);
            DiaNavigationData diaNavigationData = diaSession.GetNavigationData("SimpleClassLibrary.HugeMethodSet", "MSTest_D1_01");
            watch.Stop();

            Assert.IsNotNull(diaNavigationData, "Failed to get navigation data");
            StringAssert.EndsWith(diaNavigationData.FileName, @"\SimpleClassLibrary\HugeMethodSet.cs");
            this.ValidateMinLineNumber(9, diaNavigationData.MinLineNumber);
            Assert.AreEqual(10, diaNavigationData.MaxLineNumber);
            var expectedTime = 150;
            Assert.IsTrue(watch.Elapsed.Milliseconds < expectedTime, string.Format("DiaSession Perf test Actual time:{0} ms Expected time:{1} ms", watch.Elapsed.Milliseconds, expectedTime));

            this.testEnvironment.TargetFramework = currentTargetFrameWork;
        }

        private void ValidateLineNumbers(int min, int max)
        {
            // Release builds optimize code, hence min line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(min, max, "Incorrect min line number");
            }
            else
            {
                if (max == 23)
                {
                    Assert.AreEqual(min + 1, max, "Incorrect min line number");
                }

                else if (max == 27)
                {
                    Assert.AreEqual(min + 1, max, "Incorrect min line number");
                }
                else
                {
                    Assert.Fail("Incorrect min/max line number");
                }
            }
        }

        private void ValidateMinLineNumber(int expected, int actual)
        {
            // Release builds optimize code, hence min line numbers are different.
            if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(expected + 1, actual, "Incorrect min line number");
            }
            else
            {
                Assert.AreEqual(expected, actual, "Incorrect min line number");
            }
        }
    }
}
