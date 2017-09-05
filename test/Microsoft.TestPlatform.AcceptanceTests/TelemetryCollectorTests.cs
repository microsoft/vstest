using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using System.IO;
    using System.Linq;

    [TestClass]
    public class TelemetryCollectorTests : AcceptanceTestBase
    {
        private const string path = @"c:\temp\MyTest.txt";

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void TelemetryCollecorShouldLogEvents(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);
            var assemblyPaths = this.GetAssetFullPath("SimpleTestProject.dll");
            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            this.ValidateOutput();
        }

        private void ValidateOutput()
        {
            bool isValid = false;
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length ; i++)
                {
                    if (lines[i].Contains(UnitTestTelemetryDataConstants.RunState))
                    {
                        isValid = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(isValid);
        }
    }
}
