// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.DataCollection
{
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;

    [TestClass]
    public class DataCollectorsSettingsProviderTests
    {
        private string xmlSettings =
           "<DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=\"Custom DataCollector\" uri=\"datacollector://MyCompany/MyDataCollector/1.0\" assemblyQualifiedName=\"MyDataCollector.CustomDataCollector, MyDataCollector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\"></DataCollector></DataCollectors>\r\n  </DataCollectionRunSettings>";

        [TestMethod]
        public void LoadShouldLoadDataCollectorRunSettingsFromXML()
        {
            var dcSettingsProvider = new DataCollectorsSettingsProvider();
            var reader = XmlReader.Create(new StringReader(this.xmlSettings));
            dcSettingsProvider.Load(reader);
            Assert.IsNotNull(dcSettingsProvider.Settings);
            Assert.AreEqual(dcSettingsProvider.Settings.Name, "DataCollectionRunSettings");
            Assert.AreEqual(dcSettingsProvider.Settings.DataCollectorSettingsList.Count, 1);
            Assert.AreEqual(dcSettingsProvider.Settings.DataCollectorSettingsList[0].AssemblyQualifiedName, "MyDataCollector.CustomDataCollector, MyDataCollector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            Assert.AreEqual(dcSettingsProvider.Settings.DataCollectorSettingsList[0].FriendlyName, "Custom DataCollector");
            Assert.AreEqual(dcSettingsProvider.Settings.DataCollectorSettingsList[0].Uri, "datacollector://MyCompany/MyDataCollector/1.0");
        }

        [TestMethod]
        public void LoadShouldThrowExceptionIfXmlWithoutDataCollectionRunSettingsIsPassed()
        {
            Assert.ThrowsException<SettingsException>(
                () =>
                {
                    var dcSettingsProvider = new DataCollectorsSettingsProvider();
                    var reader = XmlReader.Create(new StringReader("<RunSettings><abc></abc></RunSettings>)"));
                    dcSettingsProvider.Load(reader);
                });
        }
    }
}