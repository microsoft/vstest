// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests.Utilities
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class XmlRunSettingsUtilitiesTests
    {
        #region private variables

        private readonly string runSettingsXmlWithDataCollectors = @"<RunSettings>
<RunConfiguration>
</RunConfiguration>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Exclude>
                <ModulePath>.*CPPUnitTestFramework.*</ModulePath>
              </Exclude>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>";

        private readonly string runSettingsXmlWithDataCollectorsDisabled = @"<RunSettings>
<RunConfiguration>
</RunConfiguration>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" enabled=""false"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Exclude>
                <ModulePath>.*CPPUnitTestFramework.*</ModulePath>
              </Exclude>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>";

        private readonly string runSettingsXmlWithIncorrectDataCollectorSettings = @"<RunSettings>
<RunConfiguration>
</RunConfiguration>
  <DataCollectionRunSettings>
    <DataCollectors attributethatshouldneverexist=""false"">
      <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" enabled=""false"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Exclude>
                <ModulePath>.*CPPUnitTestFramework.*</ModulePath>
              </Exclude>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>";

        #endregion

        #region GetTestRunParameters tests

        [TestMethod]
        public void GetTestRunParametersReturnsEmptyDictionaryOnNullRunSettings()
        {
            Dictionary<string, object> trp = XmlRunSettingsUtilities.GetTestRunParameters(null);
            Assert.IsNotNull(trp);
            Assert.AreEqual(0, trp.Count);
        }

        [TestMethod]
        public void GetTestRunParametersReturnsEmptyDictionaryWhenNoTestRunParameters()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>.\TestResults</ResultsDirectory>
                       <TargetPlatform>x86</TargetPlatform>
                       <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
                     </RunConfiguration>
                </RunSettings>";

            Dictionary<string, object> trp = XmlRunSettingsUtilities.GetTestRunParameters(settingsXml);
            Assert.IsNotNull(trp);
            Assert.AreEqual(0, trp.Count);
        }

        [TestMethod]
        public void GetTestRunParametersReturnsEmptyDictionaryForEmptyTestRunParametersNode()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>.\TestResults</ResultsDirectory>
                       <TargetPlatform>x86</TargetPlatform>
                       <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
                     </RunConfiguration>
                     <TestRunParameters>
                     </TestRunParameters>
                </RunSettings>";

            Dictionary<string, object> trp = XmlRunSettingsUtilities.GetTestRunParameters(settingsXml);
            Assert.IsNotNull(trp);
            Assert.AreEqual(0, trp.Count);
        }

        [TestMethod]
        public void GetTestRunParametersReturns1EntryOn1TestRunParameter()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>.\TestResults</ResultsDirectory>
                       <TargetPlatform>x86</TargetPlatform>
                       <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
                     </RunConfiguration>
                     <TestRunParameters>
                        <Parameter name=""webAppUrl"" value=""http://localhost"" />
                     </TestRunParameters>
                </RunSettings>";

            Dictionary<string, object> trp = XmlRunSettingsUtilities.GetTestRunParameters(settingsXml);
            Assert.IsNotNull(trp);
            Assert.AreEqual(1, trp.Count);

            // Verify Parameter Values.
            Assert.IsTrue(trp.ContainsKey("webAppUrl"));
            Assert.AreEqual(trp["webAppUrl"], "http://localhost");
        }

        [TestMethod]
        public void GetTestRunParametersReturns3EntryOn3TestRunParameter()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>.\TestResults</ResultsDirectory>
                       <TargetPlatform>x86</TargetPlatform>
                       <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
                     </RunConfiguration>
                     <TestRunParameters>
                        <Parameter name=""webAppUrl"" value=""http://localhost"" />
                        <Parameter name=""webAppUserName"" value=""Admin"" />
                        <Parameter name=""webAppPassword"" value=""Password"" />
                     </TestRunParameters>
                </RunSettings>";

            Dictionary<string, object> trp = XmlRunSettingsUtilities.GetTestRunParameters(settingsXml);
            Assert.IsNotNull(trp);
            Assert.AreEqual(3, trp.Count);

            // Verify Parameter Values.
            Assert.IsTrue(trp.ContainsKey("webAppUrl"));
            Assert.AreEqual(trp["webAppUrl"], "http://localhost");
            Assert.IsTrue(trp.ContainsKey("webAppUserName"));
            Assert.AreEqual(trp["webAppUserName"], "Admin");
            Assert.IsTrue(trp.ContainsKey("webAppPassword"));
            Assert.AreEqual(trp["webAppPassword"], "Password");
        }

        [TestMethod]
        public void GetTestRunParametersThrowsWhenTRPNodeHasAttributes()
        {
            string settingsXml =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>.\TestResults</ResultsDirectory>
                       <TargetPlatform>x86</TargetPlatform>
                       <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
                     </RunConfiguration>
                     <TestRunParameters count=""1"">
                        <Parameter name=""webAppUrl"" value=""http://localhost"" />
                     </TestRunParameters>
                </RunSettings>";

            Assert.ThrowsException<SettingsException>(() => XmlRunSettingsUtilities.GetTestRunParameters(settingsXml));
        }

        [TestMethod]
        public void GetTestRunParametersThrowsWhenTRPNodeHasNonParameterTypeChildNodes()
        {
            string settingsXml =
               @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>.\TestResults</ResultsDirectory>
                       <TargetPlatform>x86</TargetPlatform>
                       <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
                     </RunConfiguration>
                     <TestRunParameters>
                        <Parameter name=""webAppUrl"" value=""http://localhost"" />
                        <TargetPlatform>x86</TargetPlatform>
                     </TestRunParameters>
                </RunSettings>";

            Assert.ThrowsException<SettingsException>(() => XmlRunSettingsUtilities.GetTestRunParameters(settingsXml));
        }

        [TestMethod]
        public void GetTestRunParametersIgnoresMalformedKeyValues()
        {
            string settingsXml =
               @"<?xml version=""1.0"" encoding=""utf-8""?>
                <RunSettings>
                     <RunConfiguration>
                       <ResultsDirectory>.\TestResults</ResultsDirectory>
                       <TargetPlatform>x86</TargetPlatform>
                       <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
                     </RunConfiguration>
                     <TestRunParameters>
                        <Parameter name=""webAppUrl"" values=""http://localhost"" />
                     </TestRunParameters>
                </RunSettings>";

            Dictionary<string, object> trp = XmlRunSettingsUtilities.GetTestRunParameters(settingsXml);
            Assert.IsNotNull(trp);
            Assert.AreEqual(0, trp.Count);
        }

        [TestMethod]
        public void GetInProcDataCollectionRunSettingsFromSettings()
        {
            string settingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";
            var inProcDCRunSettings = XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(settingsXml);
            Assert.IsNotNull(inProcDCRunSettings);
            Assert.AreEqual(inProcDCRunSettings.DataCollectorSettingsList.Count, 1);
        }

        [TestMethod]
        public void GetInProcDataCollectionRunSettingsThrowsExceptionWhenXMLNotValid()
        {
            string settingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyNames='Test Impact' uris='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";

            Assert.ThrowsException<SettingsException>(
                () => XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(settingsXml));
        }
        #endregion

        #region CreateDefaultRunSettings tests

        [TestMethod]
        public void CreateDefaultRunSettingsShouldReturnABasicRunSettings()
        {
            var defaultRunSettings = XmlRunSettingsUtilities.CreateDefaultRunSettings().CreateNavigator().OuterXml;
            var expectedRunSettings =
                "<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

            Assert.AreEqual(expectedRunSettings, defaultRunSettings);
        }

        #endregion

        #region IsDataCollectionEnabled tests

        [TestMethod]
        public void IsDataCollectionEnabledShouldReturnFalseIfRunSettingsIsNull()
        {
            Assert.IsFalse(XmlRunSettingsUtilities.IsDataCollectionEnabled(null));
        }

        [TestMethod]
        public void IsDataCollectionEnabledShouldReturnFalseIfDataCollectionNodeIsNotPresent()
        {
            Assert.IsFalse(XmlRunSettingsUtilities.IsDataCollectionEnabled("<RunSettings></RunSettings>"));
        }

        [TestMethod]
        public void IsDataCollectionEnabledShouldReturnFalseIfDataCollectionIsDisabled()
        {
            Assert.IsFalse(XmlRunSettingsUtilities.IsDataCollectionEnabled(this.runSettingsXmlWithDataCollectorsDisabled));
        }


        [TestMethod]
        public void IsDataCollectionEnabledShouldReturnTrueIfDataCollectionIsEnabled()
        {
            Assert.IsTrue(XmlRunSettingsUtilities.IsDataCollectionEnabled(this.runSettingsXmlWithDataCollectors));
        }

        #endregion

        #region IsInProcDataCollectionEnabled tests.

        [TestMethod]
        public void IsInProcDataCollectionEnabledShouldReturnFalseIfRunSettingsIsNull()
        {
            Assert.IsFalse(XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(null));
        }

        [TestMethod]
        public void IsInProcDataCollectionEnabledShouldReturnFalseIfDataCollectionNodeIsNotPresent()
        {
            Assert.IsFalse(XmlRunSettingsUtilities.IsInProcDataCollectionEnabled("<RunSettings></RunSettings>"));
        }

        [TestMethod]
        public void IsInProcDataCollectionEnabledShouldReturnFalseIfDataCollectionIsDisabled()
        {
            Assert.IsFalse(XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(this.ConvertOutOfProcDataCollectionSettingsToInProcDataCollectionSettings(this.runSettingsXmlWithDataCollectorsDisabled)));
        }

        [TestMethod]
        public void IsInProcDataCollectionEnabledShouldReturnTrueIfDataCollectionIsEnabled()
        {
            Assert.IsTrue(XmlRunSettingsUtilities.IsInProcDataCollectionEnabled(this.ConvertOutOfProcDataCollectionSettingsToInProcDataCollectionSettings(this.runSettingsXmlWithDataCollectors)));
        }

        #endregion

        #region GetDataCollectionRunSettings tests

        [TestMethod]
        public void GetDataCollectionRunSettingsShouldReturnNullIfSettingsIsNull()
        {
            Assert.IsNull(XmlRunSettingsUtilities.GetDataCollectionRunSettings(null));
        }

        [TestMethod]
        public void GetDataCollectionRunSettingsShouldReturnNullOnNoDataCollectorSettings()
        {
            Assert.IsNull(XmlRunSettingsUtilities.GetDataCollectionRunSettings("<RunSettings></RunSettings>"));
        }

        [TestMethod]
        public void GetDataCollectionRunSettingsShouldReturnDataCollectorRunSettings()
        {
            Assert.IsNotNull(XmlRunSettingsUtilities.GetDataCollectionRunSettings(this.runSettingsXmlWithDataCollectors));
        }

        [TestMethod]
        public void GetDataCollectionRunSettingsShouldReturnDataCollectorRunSettingsEvenIfDisabled()
        {
            Assert.IsNotNull(XmlRunSettingsUtilities.GetDataCollectionRunSettings(this.runSettingsXmlWithDataCollectorsDisabled));
        }

        [TestMethod]
        public void GetDataCollectionRunSettingsShouldThrowOnMalformedDataCollectorSettings()
        {
            Assert.ThrowsException<SettingsException>(() => XmlRunSettingsUtilities.GetDataCollectionRunSettings(this.runSettingsXmlWithIncorrectDataCollectorSettings));
        }

        #endregion

        private string ConvertOutOfProcDataCollectionSettingsToInProcDataCollectionSettings(string settings)
        {
            return
                settings.Replace("DataCollectionRunSettings", "InProcDataCollectionRunSettings")
                    .Replace("<DataCollectors>", "<InProcDataCollectors>")
                    .Replace("</DataCollectors>", "</InProcDataCollectors>")
                    .Replace("<DataCollector ", "<InProcDataCollector ")
                    .Replace("</DataCollector>", "</InProcDataCollector>");
        }
    }
}
