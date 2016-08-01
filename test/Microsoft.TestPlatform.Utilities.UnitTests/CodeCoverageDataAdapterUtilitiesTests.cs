// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Utilities.Tests
{
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CodeCoverageDataAdapterUtilitiesTests
    {
        [TestMethod]
        public void UpdateWithCodeCoverageSettingsIfNotConfiguredShouldNotUpdateIfStaticCCIsAlreadySpecified()
        {
            var runSettingsXml = @"<RunSettings>
                                        <DataCollectionRunSettings>
                                            <DataCollectors>
                                                <DataCollector uri=""datacollector://microsoft/CodeCoverage/1.0"">
                                                </DataCollector>
                                            </DataCollectors>
                                        </DataCollectionRunSettings>
                                    </RunSettings>";

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(runSettingsXml);

            CodeCoverageDataAdapterUtilities.UpdateWithCodeCoverageSettingsIfNotConfigured(
                xmlDocument.ToXPathNavigable());

            var expectedRunSettings =
                "<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector uri=\"datacollector://microsoft/CodeCoverage/1.0\"></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            Assert.AreEqual(expectedRunSettings, xmlDocument.OuterXml);
        }

        [TestMethod]
        public void UpdateWithCodeCoverageSettingsIfNotConfiguredShouldNotUpdateIfDynamicCCIsAlreadySpecified()
        {
            var runSettingsXml = @"<RunSettings>
                                        <DataCollectionRunSettings>
                                            <DataCollectors>
                                                <DataCollector uri=""datacollector://microsoft/CodeCoverage/2.0"">
                                                </DataCollector>
                                            </DataCollectors>
                                        </DataCollectionRunSettings>
                                    </RunSettings>";

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(runSettingsXml);

            CodeCoverageDataAdapterUtilities.UpdateWithCodeCoverageSettingsIfNotConfigured(
                xmlDocument.ToXPathNavigable());

            var expectedRunSettings =
                "<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector uri=\"datacollector://microsoft/CodeCoverage/2.0\"></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            Assert.AreEqual(expectedRunSettings, xmlDocument.OuterXml);
        }
    }
}
