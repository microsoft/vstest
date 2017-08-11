// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.UnitTests
{
    using System.IO;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.EventLogCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CollectionNameValueConfigurationManagerTests
    {
        private const string ConfigurationString =
            @"<Configuration><Setting name=""key"" value=""value"" /></Configuration>";

        private const string EmptyConfigurationString =
            @"<Configuration/>";

        [TestMethod]
        public void ConstructorShouldInitializeNameValuePairDictionary()
        {
            XmlDocument xmlDocument = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(new StringReader(ConfigurationString)))
            {
                xmlDocument.Load(reader);
            }

            var configManager = new CollectorNameValueConfigurationManager(xmlDocument.DocumentElement);

            Assert.AreEqual("value", configManager["key"]);
        }

        [TestMethod]
        public void ConstructorShouldNotInitializeNameValuePairIfEmptyXmlElementIsPassed()
        {
            XmlDocument xmlDocument = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(new StringReader(EmptyConfigurationString)))
            {
                xmlDocument.Load(reader);
            }

            var configManager = new CollectorNameValueConfigurationManager(xmlDocument.DocumentElement);
            Assert.AreEqual(configManager.nameValuePairs.Count, 0);
        }

        [TestMethod]
        public void ConstructorShouldNotInitializeNameValuePairNullIsPassed()
        {
            var configManager = new CollectorNameValueConfigurationManager(null);
            Assert.AreEqual(configManager.nameValuePairs.Count, 0);
        }
    }
}
