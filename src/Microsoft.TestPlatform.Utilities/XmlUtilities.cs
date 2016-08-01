// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Security;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// Utilities class to read and operate on Xml content.
    /// </summary>
    internal class XmlUtilities
    {
        /// <summary>
        /// Gets the Inner XML of the specified node.
        /// </summary>
        /// <param name="runSettingsNavigator"> The xml navigator. </param>
        /// <param name="nodeXPath"> The xPath of the node. </param>
        /// <returns></returns>
        internal static string GetNodeXml(XPathNavigator runSettingsNavigator, string nodeXPath)
        {
            var node = runSettingsNavigator.SelectSingleNode(nodeXPath);
            return node?.InnerXml;
        }

        /// <summary>
        /// Validates if the Node value is correct according to the provided validator.
        /// </summary>
        /// <param name="xmlNodeValue"> The node value. </param>
        /// <param name="validator"> The validator. </param>
        /// <returns></returns>
        internal static bool IsValidNodeXmlValue(string xmlNodeValue, Func<string, bool> validator)
        {
            try
            {
                return validator.Invoke(xmlNodeValue);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// If xml node exists with given path, its value is set to innerXml, otherwise a new node is created.
        /// </summary>
        /// <remarks> Ensure that the navigator is set to right parent.</remarks>
        internal static void AppendOrModifyChild(
            XPathNavigator parentNavigator,
            string nodeXPath,
            string nodeName,
            string innerXml)
        {
            var childNodeNavigator = parentNavigator.SelectSingleNode(nodeXPath);

            // Todo: There isn't an equivalent API to SecurityElement.Escape in Core yet. 
            // So trusting that the XML is always valid for now.
#if NET46
            var secureInnerXml = SecurityElement.Escape(innerXml);
#else
            var secureInnerXml = innerXml;
#endif
            if (childNodeNavigator == null)
            {
                var doc = new XmlDocument();
                var childElement = doc.CreateElement(nodeName);

                if (!string.IsNullOrEmpty(innerXml))
                {
                    childElement.InnerXml = secureInnerXml;
                }

                childNodeNavigator = childElement.CreateNavigator();
                parentNavigator.AppendChild(childNodeNavigator);
            }
            else if (!string.IsNullOrEmpty(innerXml))
            {
                try
                {
                    childNodeNavigator.InnerXml = secureInnerXml;
                }
                catch (XmlException)
                {
                    // .Net Core has a bug where calling childNodeNavigator.InnerXml throws an XmlException with "Data at the root level is invalid".
                    // So doing the below instead.
                    var doc = new XmlDocument();

                    var childElement = doc.CreateElement(nodeName);

                    if (!string.IsNullOrEmpty(innerXml))
                    {
                        childElement.InnerXml = secureInnerXml;
                    }

                    childNodeNavigator.ReplaceSelf(childElement.CreateNavigator().OuterXml);
                }
            }
        }
    }
}
