// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

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

            CodeCoverageDataAdapterUtilities.UpdateWithCodeCoverageSettingsIfNotConfigured(GetXPathNavigable(xmlDocument));

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

            CodeCoverageDataAdapterUtilities.UpdateWithCodeCoverageSettingsIfNotConfigured(GetXPathNavigable(xmlDocument));

            var expectedRunSettings =
                "<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector uri=\"datacollector://microsoft/CodeCoverage/2.0\"></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            Assert.AreEqual(expectedRunSettings, xmlDocument.OuterXml);
        }

        private static IXPathNavigable GetXPathNavigable(XmlDocument doc)
        {
#if NET46
            return doc;
#else
            return doc.ToXPathNavigable();
#endif
        }
    }
}
