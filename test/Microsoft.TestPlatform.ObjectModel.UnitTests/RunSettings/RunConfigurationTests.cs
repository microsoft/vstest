using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    [TestClass]
    public class RunConfigurationTests
    {
        [TestMethod]
        public void RunConfigurationDefaultValuesMustBeUsedOnCreation()
        {
            var runConfiguration = new RunConfiguration();

            // Verify Default
            Assert.AreEqual(Constants.DefaultPlatform, runConfiguration.TargetPlatform);
            Assert.AreEqual(Framework.DefaultFramework, runConfiguration.TargetFrameworkVersion);
            Assert.AreEqual(Constants.DefaultResultsDirectory, runConfiguration.ResultsDirectory);
            Assert.AreEqual(null, runConfiguration.SolutionDirectory);
            Assert.AreEqual(Constants.DefaultTreatTestAdapterErrorsAsWarnings, runConfiguration.TreatTestAdapterErrorsAsWarnings);
            Assert.AreEqual(null, runConfiguration.BinariesRoot);
            Assert.AreEqual(null, runConfiguration.TestAdaptersPaths);
            Assert.AreEqual(Constants.DefaultCpuCount, runConfiguration.MaxCpuCount);
            Assert.AreEqual(false, runConfiguration.DisableAppDomain);
            Assert.AreEqual(false, runConfiguration.DisableParallelization);
        }

        [TestMethod]
        public void RunConfigurationThrowsExceptionOnUnknownElements()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <BadElement>TestResults</BadElement>
                     </RunConfiguration>
                </RunSettings>";


            Assert.ThrowsException<SettingsException>(() =>
                XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml));
        }

        [TestMethod]
        public void RunConfigurationReadsValuesCorrectlyFromXml()
        {
            string settingsXml =
              @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>TestResults</ResultsDirectory>
                       <TargetPlatform>x64</TargetPlatform>
                       <TargetFrameworkVersion>FrameworkCore10</TargetFrameworkVersion>
                       <SolutionDirectory>%temp%</SolutionDirectory>
                       <TreatTestAdapterErrorsAsWarnings>true</TreatTestAdapterErrorsAsWarnings>
                       <DisableAppDomain>true</DisableAppDomain>
                       <DisableParallelization>true</DisableParallelization>
                       <MaxCpuCount>2</MaxCpuCount>
                       <TestAdaptersPaths>C:\a\b;D:\x\y</TestAdaptersPaths>
                       <BinariesRoot>E:\x\z</BinariesRoot>
                     </RunConfiguration>
                </RunSettings>";

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);

            // Verify Default
            Assert.AreEqual(Architecture.X64, runConfiguration.TargetPlatform);

            var expectedFramework = Framework.FromString("FrameworkCore10");
            var actualFramework = runConfiguration.TargetFrameworkVersion;
            Assert.AreEqual(expectedFramework.Name, runConfiguration.TargetFrameworkVersion.Name);
            Assert.AreEqual(expectedFramework.Version, runConfiguration.TargetFrameworkVersion.Version);

            Assert.AreEqual("TestResults", runConfiguration.ResultsDirectory);

            var expectedSolutionPath = Environment.ExpandEnvironmentVariables("%temp%");
            Assert.AreEqual(expectedSolutionPath, runConfiguration.SolutionDirectory);

            Assert.AreEqual(true, runConfiguration.TreatTestAdapterErrorsAsWarnings);
            Assert.AreEqual(@"E:\x\z", runConfiguration.BinariesRoot);
            Assert.AreEqual(@"C:\a\b;D:\x\y", runConfiguration.TestAdaptersPaths);
            Assert.AreEqual(2, runConfiguration.MaxCpuCount);
            Assert.AreEqual(true, runConfiguration.DisableAppDomain);
            Assert.AreEqual(true, runConfiguration.DisableParallelization);
        }
    }
}
