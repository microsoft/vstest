// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MSTest.TestFramework.AssertExtensions;

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
            Assert.AreEqual(Constants.DefaultBatchSize, runConfiguration.BatchSize);
            Assert.AreEqual(0, runConfiguration.TestSessionTimeout);
            Assert.AreEqual(Constants.DefaultResultsDirectory, runConfiguration.ResultsDirectory);
            Assert.AreEqual(null, runConfiguration.SolutionDirectory);
            Assert.AreEqual(Constants.DefaultTreatTestAdapterErrorsAsWarnings, runConfiguration.TreatTestAdapterErrorsAsWarnings);
            Assert.AreEqual(null, runConfiguration.BinariesRoot);
            Assert.AreEqual(null, runConfiguration.TestAdaptersPaths);
            Assert.AreEqual(Constants.DefaultCpuCount, runConfiguration.MaxCpuCount);
            Assert.AreEqual(false, runConfiguration.DisableAppDomain);
            Assert.AreEqual(false, runConfiguration.DisableParallelization);
            Assert.AreEqual(false, runConfiguration.DesignMode);
            Assert.AreEqual(runConfiguration.DesignMode, runConfiguration.ShouldCollectSourceInformation);
            Assert.AreEqual(Constants.DefaultExecutionThreadApartmentState, runConfiguration.ExecutionThreadApartmentState);
        }

        [TestMethod]
        public void RunConfigurationShouldNotThrowExceptionOnUnknownElements()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <BadElement>TestResults</BadElement>
                       <DesignMode>true</DesignMode>
                     </RunConfiguration>
                </RunSettings>";

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);

            Assert.IsNotNull(runConfiguration);
            Assert.IsTrue(runConfiguration.DesignMode);
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
                       <BatchSize>5</BatchSize>
                       <TestSessionTimeout>10000</TestSessionTimeout>
                       <TestAdaptersPaths>C:\a\b;D:\x\y</TestAdaptersPaths>
                       <BinariesRoot>E:\x\z</BinariesRoot>
                       <DesignMode>true</DesignMode>
                       <CollectSourceInformation>false</CollectSourceInformation>
                       <ExecutionThreadApartmentState>STA</ExecutionThreadApartmentState>
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
            Assert.AreEqual(5, runConfiguration.BatchSize);
            Assert.AreEqual(10000, runConfiguration.TestSessionTimeout);
            Assert.AreEqual(true, runConfiguration.DisableAppDomain);
            Assert.AreEqual(true, runConfiguration.DisableParallelization);
            Assert.AreEqual(true, runConfiguration.DesignMode);
            Assert.AreEqual(false, runConfiguration.ShouldCollectSourceInformation);
            Assert.AreEqual(PlatformApartmentState.STA, runConfiguration.ExecutionThreadApartmentState);
        }

        [TestMethod]
        public void RunConfigurationFromXmlThrowsSettingsExceptionIfBatchSizeIsInvalid()
        {
            string settingsXml =
             @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <BatchSize>Foo</BatchSize>
                     </RunConfiguration>
                </RunSettings>";

            
            Assert.That.Throws<SettingsException>(
                    () => XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml))
                    .WithExactMessage("Invalid settings 'RunConfiguration'.  Invalid value 'Foo' specified for 'BatchSize'.");
        }

        [TestMethod]
        public void RunConfigurationFromXmlThrowsSettingsExceptionIfTestSessionTimeoutIsInvalid()
        {
            string settingsXml =
             @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TestSessionTimeout>0</TestSessionTimeout>
                     </RunConfiguration>
                </RunSettings>";


            Assert.That.Throws<SettingsException>(
                    () => XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml))
                    .WithExactMessage("Invalid settings 'RunConfiguration'.  Invalid value '0' specified for 'TestSessionTimeout'.");
        }

        [TestMethod]
        public void RunConfigurationFromXmlThrowsSettingsExceptionIfExecutionThreadApartmentStateIsInvalid()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ExecutionThreadApartmentState>RandomValue</ExecutionThreadApartmentState>
                     </RunConfiguration>
                </RunSettings>";


            Assert.That.Throws<SettingsException>(
                    () => XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml))
                .WithExactMessage("Invalid settings 'RunConfiguration'.  Invalid value 'RandomValue' specified for 'ExecutionThreadApartmentState'.");
        }

        [TestMethod]
        public void RunConfigurationFromXmlThrowsSettingsExceptionIfBatchSizeIsNegativeInteger()
        {
            string settingsXml =
             @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <BatchSize>-10</BatchSize>
                     </RunConfiguration>
                </RunSettings>";

            Assert.That.Throws<SettingsException>(
              () => XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml))
              .WithExactMessage("Invalid settings 'RunConfiguration'.  Invalid value '-10' specified for 'BatchSize'.");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void RunConfigurationShouldReadValueForDesignMode(bool designModeValue)
        {
            string settingsXml = string.Format(
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <DesignMode>{0}</DesignMode>
                     </RunConfiguration>
                </RunSettings>", designModeValue);

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);

            Assert.AreEqual(designModeValue, runConfiguration.DesignMode);
        }

        [TestMethod]
        public void RunConfigurationShouldSetDesignModeAsFalseByDefault()
        {
            string settingsXml =
              @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TargetPlatform>x64</TargetPlatform>
                     </RunConfiguration>
                </RunSettings>";

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);
            
            Assert.IsFalse(runConfiguration.DesignMode);
        }

        [TestMethod]
        public void RunConfigurationToXmlShouldProvideDesignMode()
        {
            var runConfiguration = new RunConfiguration { DesignMode = true };

            StringAssert.Contains(runConfiguration.ToXml().InnerXml, "<DesignMode>True</DesignMode>");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void RunConfigurationShouldReadValueForCollectSourceInformation(bool val)
        {
            string settingsXml = string.Format(
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <CollectSourceInformation>{0}</CollectSourceInformation>
                     </RunConfiguration>
                </RunSettings>", val);

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);

            Assert.AreEqual(val, runConfiguration.ShouldCollectSourceInformation);
        }

        [TestMethod]
        public void RunConfigurationShouldSetCollectSourceInformationSameAsDesignModeByDefault()
        {
            string settingsXml =
              @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <TargetPlatform>x64</TargetPlatform>
                     </RunConfiguration>
                </RunSettings>";

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);
            
            Assert.AreEqual(runConfiguration.DesignMode, runConfiguration.ShouldCollectSourceInformation);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RunConfigurationToXmlShouldProvideCollectSourceInformationSameAsDesignMode(bool val)
        {
            var runConfiguration = new RunConfiguration { DesignMode = val };
            StringAssert.Contains(runConfiguration.ToXml().InnerXml.ToUpperInvariant(), $"<CollectSourceInformation>{val}</CollectSourceInformation>".ToUpperInvariant());
        }

        [TestMethod]
        public void RunConfigurationToXmlShouldProvideExecutionThreadApartmentState()
        {
            var runConfiguration = new RunConfiguration { ExecutionThreadApartmentState = PlatformApartmentState.STA };

            StringAssert.Contains(runConfiguration.ToXml().InnerXml, "<ExecutionThreadApartmentState>STA</ExecutionThreadApartmentState>");
        }
    }
}
