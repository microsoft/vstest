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
        public void GetNodeXmlShouldThrowIfNavigatorIsNull()
        {
            Assert.ThrowsException<NullReferenceException>(() => XmlUtilities.GetNodeXml(null, @"/RunSettings/RunConfiguration"));
        }

        [TestMethod]
        public void GetNodeXmlShouldThrowIfXPathIsNull()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settingsXml);

            Assert.ThrowsException<XPathException>(() => XmlUtilities.GetNodeXml(navigator, null));
        }

        [TestMethod]
        public void GetNodeXmlShouldThrowIfXPathIsInvalid()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settingsXml);

            Assert.ThrowsException<XPathException>(() => XmlUtilities.GetNodeXml(navigator, @"Rs\r"));
        }

        [TestMethod]
        public void GetNodeXmlShouldReturnNullIfNodeDoesNotExist()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settingsXml);

            Assert.IsNull(XmlUtilities.GetNodeXml(navigator, @"/RunSettings/RunConfiguration"));
        }

        [TestMethod]
        public void GetNodeXmlShouldReturnNodeValue()
        {
            var settingsXml = @"<RunSettings><RC>abc</RC></RunSettings>";
            var navigator = this.GetNavigator(settingsXml);

            Assert.AreEqual("abc", XmlUtilities.GetNodeXml(navigator, @"/RunSettings/RC"));
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
            var navigator = this.GetNavigator(settingsXml);

            navigator.MoveToChild("RunSettings", string.Empty);

            XmlUtilities.AppendOrModifyChild(navigator, @"/RunSettings/RC", "RC", "ab");

            var rcNavigator = navigator.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcNavigator);
            Assert.AreEqual("ab", rcNavigator.InnerXml);
        }

        [TestMethod]
        public void AppendOrModifyChildShouldAppendANewNode()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settingsXml);

            navigator.MoveToChild("RunSettings", string.Empty);

            XmlUtilities.AppendOrModifyChild(navigator, @"/RunSettings/RC", "RC", "abc");

            var rcNavigator = navigator.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcNavigator);
            Assert.AreEqual("abc", rcNavigator.InnerXml);
        }

        [TestMethod]
        public void AppendOrModifyChildShouldNotModifyExistingXmlIfInnerXmlPassedInIsNull()
        {
            var settingsXml = @"<RunSettings><RC>abc</RC></RunSettings>";
            var navigator = this.GetNavigator(settingsXml);

            navigator.MoveToChild("RunSettings", string.Empty);

            XmlUtilities.AppendOrModifyChild(navigator, @"/RunSettings/RC", "RC", null);

            var rcNavigator = navigator.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcNavigator);
            Assert.AreEqual("abc", rcNavigator.InnerXml);
        }

        [TestMethod]
        public void AppendOrModifyChildShouldCreateAnEmptyNewNodeIfInnerXmlPassedInIsNull()
        {
            var settingsXml = @"<RunSettings></RunSettings>";
            var navigator = this.GetNavigator(settingsXml);

            navigator.MoveToChild("RunSettings", string.Empty);

            XmlUtilities.AppendOrModifyChild(navigator, @"/RunSettings/RC", "RC", null);

            var rcNavigator = navigator.SelectSingleNode(@"/RunSettings/RC");
            Assert.IsNotNull(rcNavigator);
            Assert.AreEqual(string.Empty, rcNavigator.InnerXml);
        }

        #endregion

        #region private methods

        private XPathNavigator GetNavigator(string settingsXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);

            return doc.CreateNavigator();
        }

        #endregion
    }
}
