
namespace Microsoft.TestPlatform.ObjectModel.UnitTests.RunSettings
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.TestPlatform.ObjectModel.UnitTests.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InProcDataCollectionRunSettingsTests
    {

        private string settingsXml = @"<RunSettings>
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

        private Collection<DataCollectorSettings> inProcDCSettings;
            
        [TestInitialize]
        public void InitializeTests()
        {
            InProcDataCollectionUtilities.ReadInProcDataCollectionRunSettings(this.settingsXml);
            this.inProcDCSettings = InProcDataCollectionUtilities.GetInProcDataCollectorSettings();
            Assert.IsNotNull(this.inProcDCSettings);
            Assert.AreEqual(this.inProcDCSettings.Count, 1);
        }

        [TestMethod]
        public void InProcDataCollectorIsReadingUri()
        {                    
            Assert.IsTrue(string.Equals(this.inProcDCSettings[0].Uri.ToString(), "InProcDataCollector://Microsoft/TestImpact/1.0", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void InProcDataCollectorIsReadingFriendlyName()
        {
            Assert.IsTrue(string.Equals(this.inProcDCSettings[0].FriendlyName, "Test Impact", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void InProcDataCollectorIsReadingAssemblyQualifiedName()
        {
            Assert.IsTrue(string.Equals(this.inProcDCSettings[0].AssemblyQualifiedName.ToString(), "TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void InProcDataCollectorIsReadingCodeBase()
        {
            Assert.IsTrue(string.Equals(this.inProcDCSettings[0].CodeBase.ToString(), @"E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void InProcDataCollectorIsReadingConfiguration()
        {
            Assert.IsTrue(string.Equals(this.inProcDCSettings[0].Configuration.OuterXml.ToString(), @"<Configuration><Port>4312</Port></Configuration>", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void InProcDataCollectorIsReadingMultipleDataCollector()
        {
            var multiSettingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                            <InProcDataCollector friendlyName='InProcDataCol' uri='InProcDataCollector://Microsoft/InProcDataCol/2.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";
            InProcDataCollectionUtilities.ReadInProcDataCollectionRunSettings(multiSettingsXml);
            var inProcDCSettings = InProcDataCollectionUtilities.GetInProcDataCollectorSettings();
            Assert.IsNotNull(inProcDCSettings);
            Assert.AreEqual(inProcDCSettings.Count, 2);
        }

        [TestMethod]
        public void InProcDataCollectorWillThrowExceptionForInavlidAttributes()
        {
            var invalidSettingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll' value='Invalid'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";

            Assert.ThrowsException<SettingsException>(
                () => InProcDataCollectionUtilities.ReadInProcDataCollectionRunSettings(invalidSettingsXml));
        }

        [TestMethod]
        public void InProcDataCollectorReadingWhenDataCollectorsArePresent()
        {
            string settingsXml = @"<RunSettings>
                                    <DataCollectionRunSettings>
                                         <DataCollectors>
                                            <DataCollector friendlyName='Code Coverage' uri='datacollector://Microsoft/CodeCoverage/2.0' assemblyQualifiedName='Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'>
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

            InProcDataCollectionUtilities.ReadInProcDataCollectionRunSettings(settingsXml);
            var inProcDCSettings = InProcDataCollectionUtilities.GetInProcDataCollectorSettings();
            Assert.IsNotNull(inProcDCSettings);
            Assert.AreEqual(inProcDCSettings.Count, 1);
        }

        [TestMethod]
        public void InProcDataCollectionSettingsToXMLCheck()
        {
            string actualXML =
                @"<InProcDataCollectionRunSettings><InProcDataCollectors><InProcDataCollector uri=""InProcDataCollector://Microsoft/TestImpact/1.0"" assemblyQualifiedName=""TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a""  friendlyName=""Test Impact""><Configuration><Port>4312</Port></Configuration></InProcDataCollector></InProcDataCollectors></InProcDataCollectionRunSettings>";
            InProcDataCollectionUtilities.ReadInProcDataCollectionRunSettings(this.settingsXml);
            Assert.IsTrue(string.Equals(InProcDataCollectionUtilities.InProcDataCollectionRunSettings.ToXml().OuterXml.Trim().Replace(" ", ""), actualXML.Trim().Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        }
    }
}
