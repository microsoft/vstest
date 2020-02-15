// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.SmokeTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    [TestClass]
    public class DataCollectorTestsCoverlets : IntegrationTestBase
    {
        [TestMethod]
        public void RunCoverletCoverage()
        {
            string projectFileName = this.GetProjectAssetFullPath("CoverletCoverageTestProject.csproj", "CoverletCoverageTestProject.csproj");
            string logId = Guid.NewGuid().ToString("N");
            this.InvokeDotnetTest($"--collect:\"XPlat Code Coverage\" {projectFileName} --diag:coverletcoverage.{logId}.log");
            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Verify vstest.console.dll CollectArgumentProcessor fix codeBase for coverlet package
            var log = Directory.GetFiles(currentDir, $"coverletcoverage.{logId}.log").Single();
            Assert.IsTrue(File.ReadAllText(log).Contains("CoverletDataCollector in-process codeBase path"));

            // Verify out-of-proc coverlet collector load
            var dataCollectorLog = Directory.GetFiles(currentDir, $"coverletcoverage.{logId}.datacollector*log").Single();
            Assert.IsTrue(File.ReadAllText(dataCollectorLog).Contains("[coverlet]Initializing CoverletCoverageDataCollector"));

            // Verify in-proc coverlet collector load
            var hostLog = Directory.GetFiles(currentDir, $"coverletcoverage.{logId}.host*log").Single();
            Assert.IsTrue(File.ReadAllText(hostLog).Contains("[coverlet]Initialize CoverletInProcDataCollector"));

            // Verify default coverage file is generated
            this.StdOutputContains("coverage.cobertura.xml");
        }
    }
}
