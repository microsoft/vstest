// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests
{
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class XmlPersistenceTests
    {
        [TestMethod]
        public void SaveObjectShouldRemoveInvalidCharacter()
        {
            System.Diagnostics.Debugger.Launch();
            XmlPersistence xmlPersistence = new XmlPersistence();
            var node = xmlPersistence.CreateRootElement("TestRun");

            string strWithInvalidCharForXml = "This string has these \0 \v invalid characters";

            xmlPersistence.SaveObject(strWithInvalidCharForXml, node, null, "dummy");

            string expectedResult = "This string has these \\u0000 \\u000b invalid characters";
            Assert.AreEqual(string.Compare(expectedResult, node.InnerXml), 0);
        }

        [TestMethod]
        public void SaveObjectShouldDoesNotRemoveValidCharacter()
        {
            XmlPersistence xmlPersistence = new XmlPersistence();
            var node = xmlPersistence.CreateRootElement("TestRun");

            string strWithInvalidCharForXml = "This string has these \\0 \v invalid characters";

            xmlPersistence.SaveObject(strWithInvalidCharForXml, node, null, "dummy");

            string expectedResult = "This string has these \\0 \\u000b invalid characters";
            Assert.AreEqual(string.Compare(expectedResult, node.InnerXml), 0);
        }
    }
}
