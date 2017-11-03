// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.UnitTests
{
    using System;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class XmlUtilitiesTests
    {
        #region GetNodeXml tests

        [TestMethod]
        public void GetNodeXmlShouldThrowIfxmlDocumentIsNull()
        {
            Assert.ThrowsException<NullReferenceException>(() => XmlUtilities.GetNodeXml(null, @"/RunSettings/RunConfiguration"));
        }

        [TestMethod]
        public void GetNodeXmlShouldThrowIfXPathIsNull()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            Assert.ThrowsException<XPathException>(() => XmlUtilities.GetNodeXml(xmlDocument.CreateNavigator(), null));
        }

        [TestMethod]
        public void GetNodeXmlShouldThrowIfXPathIsInvalid()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            Assert.ThrowsException<XPathException>(() => XmlUtilities.GetNodeXml(xmlDocument.CreateNavigator(), @"Rs\r"));
        }

        [TestMethod]
        public void GetNodeXmlShouldReturnNullIfNodeDoesNotExist()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            Assert.IsNull(XmlUtilities.GetNodeXml(xmlDocument.CreateNavigator(), @"/RunSettings/RunConfiguration"));
        }

        [TestMethod]
        public void GetNodeXmlShouldReturnNodeValue()
        {
            var settingsXml = @"<RunSettings><RC>abc</RC></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            Assert.AreEqual("abc", XmlUtilities.GetNodeXml(xmlDocument.CreateNavigator(), @"/RunSettings/RC"));
        }

        #endregion

        #region IsValidNodeXmlValue tests

        [TestMethod]
        public void IsValidNodeXmlValueShouldReturnFalseOnArgumentException()
        {
            Func<string, bool> validator = (string xml) =>
                {
                    Enum.Parse(typeof(Architecture), xml);
                    return true;
                };

            Assert.IsFalse(XmlUtilities.IsValidNodeXmlValue("foo", validator));
        }

        [TestMethod]
        public void IsValidNodeXmlValueShouldReturnFalseIfValidatorReturnsFalse()
        {
            Func<string, bool> validator = (string xml) =>
            {
                return false;
            };

            Assert.IsFalse(XmlUtilities.IsValidNodeXmlValue("foo", validator));
        }

        [TestMethod]
        public void IsValidNodeXmlValueShouldReturnTrueIfValidatorReturnsTrue()
        {
            Func<string, bool> validator = (string xml) =>
            {
                return true;
            };

            Assert.IsTrue(XmlUtilities.IsValidNodeXmlValue("foo", validator));
        }

        #endregion

        #region AppendOrModifyChild tests

        [TestMethod]
        public void AppendOrModifyChildShouldModifyExistingNode()
        {
            var settingsXml = @"<RunSettings><RC>abc</RC></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            XmlUtilities.AppendOrModifyChild(xmlDocument, @"/RunSettings/RC", "RC", "ab");

            var rcxmlDocument = xmlDocument.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcxmlDocument);
            Assert.AreEqual("ab", rcxmlDocument.InnerXml);
        }

        [TestMethod]
        public void AppendOrModifyChildShouldAppendANewNode()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            XmlUtilities.AppendOrModifyChild(xmlDocument, @"/RunSettings/RC", "RC", "abc");

            var rcxmlDocument = xmlDocument.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcxmlDocument);
            Assert.AreEqual("abc", rcxmlDocument.InnerXml);
        }

        [TestMethod]
        public void AppendOrModifyChildShouldNotModifyExistingXmlIfInnerXmlPassedInIsNull()
        {
            var settingsXml = @"<RunSettings><RC>abc</RC></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            XmlUtilities.AppendOrModifyChild(xmlDocument, @"/RunSettings/RC", "RC", null);

            var rcxmlDocument = xmlDocument.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcxmlDocument);
            Assert.AreEqual("abc", rcxmlDocument.InnerXml);
        }

        [TestMethod]
        public void AppendOrModifyChildShouldCreateAnEmptyNewNodeIfInnerXmlPassedInIsNull()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            XmlUtilities.AppendOrModifyChild(xmlDocument, @"/RunSettings/RC", "RC", null);

            var rcxmlDocument = xmlDocument.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcxmlDocument);
            Assert.AreEqual(string.Empty, rcxmlDocument.InnerXml);
        }

        [TestMethod]
        public void AppendOrModifyChildShouldNotModifyIfParentNodeDoesNotExist()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            XmlUtilities.AppendOrModifyChild(xmlDocument, @"/RunSettings/RC/RD", "RD", null);

            Assert.AreEqual(settingsXml, xmlDocument.OuterXml);
        }

        #endregion

        #region RemoveChildNode tests

        [TestMethod]
        public void RemoveChildNodeShouldNotModifyExistingXmlIfNodeDoesnotExist()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);

            XmlUtilities.RemoveChildNode(xmlDocument.CreateNavigator(), @"/RunSettings/RC", "RC");

            Assert.AreEqual(settingsXml, xmlDocument.OuterXml);
        }

        [TestMethod]
        public void RemoveChildNodeShouldRemoveXmlIfExist()
        {
            var settingsXml = @"<RunSettings><RC>abc</RC></RunSettings>";
            var xmlDocument = this.GetXmlDocument(settingsXml);
            var navigator = xmlDocument.CreateNavigator();
            navigator.MoveToChild("RunSettings", string.Empty);
            XmlUtilities.RemoveChildNode(navigator, @"/RunSettings/RC", "RC");

            Assert.AreEqual(@"<RunSettings></RunSettings>", xmlDocument.OuterXml);
        }

        #endregion

        #region private methods

        private XmlDocument GetXmlDocument(string settingsXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);

            return doc;
        }

        #endregion
    }
}
