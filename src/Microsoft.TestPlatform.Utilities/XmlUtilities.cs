// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Diagnostics.CodeAnalysis;
#if NETFRAMEWORK
    using System.Security;
#endif
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
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        internal static void AppendOrModifyChild(
            XmlDocument xmlDocument,
            string nodeXPath,
            string nodeName,
            string innerXml)
        {
            var childNode = xmlDocument.SelectSingleNode(nodeXPath);

            // TODO: There isn't an equivalent API to SecurityElement.Escape in Core yet.
            // So trusting that the XML is always valid for now.
#if NETFRAMEWORK
            var secureInnerXml = SecurityElement.Escape(innerXml);
#else
            // fixing manually as we currently target to netcore 1.1 and we don't have default implementation for Escape functionality
            var secureInnerXml = string.IsNullOrEmpty(innerXml) ? innerXml : innerXml.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
#endif
            if (childNode == null)
            {
                var childElement = xmlDocument.CreateElement(nodeName);

                if (!string.IsNullOrEmpty(innerXml))
                {
                    childElement.InnerXml = secureInnerXml;
                }

                var parentNode = xmlDocument.SelectSingleNode(nodeXPath.Substring(0, nodeXPath.LastIndexOf('/')));
                parentNode?.AppendChild(childElement);
            }
            else if (!string.IsNullOrEmpty(innerXml))
            {
                childNode.InnerXml = secureInnerXml;
            }
        }

        internal static void RemoveChildNode(XPathNavigator parentNavigator, string nodeXPath, string childName)
        {
            var childNodeNavigator = parentNavigator.SelectSingleNode(nodeXPath);
            if (childNodeNavigator != null)
            {
                parentNavigator.MoveToChild(childName, string.Empty);
                parentNavigator.DeleteSelf();
            }
        }
    }
}
