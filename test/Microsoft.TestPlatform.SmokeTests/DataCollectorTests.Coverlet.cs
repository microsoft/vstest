// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.SmokeTests
{
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using System;
    using System.IO;
    using System.Linq;

    [TestClass]
    public class DataCollectorTestsCoverlets : IntegrationTestBase
    {
        [TestMethod]
        public void RunCoverletCoverage()
        {
            // Collector is supported only for netcoreapp2.1, is compiled for netcoreapp2.1 and packaged as netstandard
            if (this.testEnvironment.TargetFramework != CoreRunnerFramework)
            {
                return;
            }

            // We use netcoreapp runner 
            // "...\vstest\tools\dotnet\dotnet.exe "...\vstest\artifacts\Debug\netcoreapp2.1\vstest.console.dll" --collect:"XPlat Code Coverage" ...
            this.testEnvironment.RunnerFramework = CoreRunnerFramework;

            string coverletAdapterPath = Path.GetDirectoryName(Directory.GetFiles(this.testEnvironment.GetNugetPackage("coverlet.collector"), "coverlet.collector.dll", SearchOption.AllDirectories).Single());
            string logId = Guid.NewGuid().ToString("N");
            string assemblyPath = this.BuildMultipleAssemblyPath("CoverletCoverageTestProject.dll").Trim('\"');
            string logPath = Path.Combine(Path.GetDirectoryName(assemblyPath), $"coverletcoverage.{logId}.log");
            string logPathDirectory = Path.GetDirectoryName(logPath);
            string argument = $"--collect:{"XPlat Code Coverage".AddDoubleQuote()} {PrepareArguments(assemblyPath, coverletAdapterPath, "", ".NETCoreApp,Version=v2.1")} --diag:{logPath.AddDoubleQuote()}";
            this.InvokeVsTest(argument);

            // Verify vstest.console.dll CollectArgumentProcessor fix codeBase for coverlet package
            // This assert check that we're sure that we've updated collector setting code base with full path, 
            // otherwise without "custom coverlet code" inside ProxyExecutionManager coverlet dll won't be resolved inside testhost.
            var log = Directory.GetFiles(logPathDirectory, $"coverletcoverage.{logId}.log").Single();
            Assert.IsTrue(File.ReadAllText(log).Contains("CoverletDataCollector in-process codeBase path"));

            // Verify out-of-proc coverlet collector load
            var dataCollectorLog = Directory.GetFiles(logPathDirectory, $"coverletcoverage.{logId}.datacollector*log").Single();
            Assert.IsTrue(File.ReadAllText(dataCollectorLog).Contains("[coverlet]Initializing CoverletCoverageDataCollector"));

            // Verify in-proc coverlet collector load
            var hostLog = Directory.GetFiles(logPathDirectory, $"coverletcoverage.{logId}.host*log").Single();
            Assert.IsTrue(File.ReadAllText(hostLog).Contains("[coverlet]Initialize CoverletInProcDataCollector"));

            // Verify default coverage file is generated
            this.StdOutputContains("coverage.cobertura.xml");
        }
    }
}