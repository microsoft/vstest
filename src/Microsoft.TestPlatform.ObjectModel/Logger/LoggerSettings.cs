// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable CheckNamespace
namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// The logger settings.
    /// </summary>
    public class LoggerSettings
    {
        /// <summary>
        /// Gets or sets the uri.
        /// </summary>
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the assembly qualified name.
        /// </summary>
        public string AssemblyQualifiedName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets value CodeBase of logger DLL. The syntax is same as Code Base in AssemblyName class.
        /// </summary>
        public string CodeBase
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the friendly name.
        /// </summary>
        public string FriendlyName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        public XmlElement Configuration
        {
            get;
            set;
        }

        // TODO: ToXml()

        ///// <summary>
        ///// The to xml.
        ///// </summary>
        ///// <returns>
        ///// The <see cref="XmlElement"/>.
        ///// </returns>
        //[SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
        //    Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        //public XmlElement ToXml()
        //{
        //    XmlDocument doc = new XmlDocument();
        //    XmlElement root = doc.CreateElement(Constants.DataCollectorSettingName);
        //    AppendAttribute(doc, root, "uri", this.Uri.ToString());
        //    AppendAttribute(doc, root, "assemblyQualifiedName", this.AssemblyQualifiedName);
        //    AppendAttribute(doc, root, "friendlyName", this.FriendlyName);

        //    root.AppendChild(doc.ImportNode(this.Configuration, true));

        //    return root;
        //}

        ///// <summary>
        ///// The to xml.
        ///// </summary>
        ///// <param name="dataCollectorName">
        ///// The data collector name.
        ///// </param>
        ///// <returns>
        ///// The <see cref="XmlElement"/>.
        ///// </returns>
        //[SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
        //    Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        //public XmlElement ToXml(string dataCollectorName)
        //{
        //    XmlDocument doc = new XmlDocument();
        //    XmlElement root = doc.CreateElement(dataCollectorName);
        //    if (this.Uri != null)
        //    {
        //        AppendAttribute(doc, root, "uri", this.Uri.ToString());
        //    }

        //    if (!string.IsNullOrWhiteSpace(this.AssemblyQualifiedName))
        //    {
        //        AppendAttribute(doc, root, "assemblyQualifiedName", this.AssemblyQualifiedName);
        //    }

        //    if (!string.IsNullOrWhiteSpace(this.FriendlyName))
        //    {
        //        AppendAttribute(doc, root, "friendlyName", this.FriendlyName);
        //    }

        //    AppendAttribute(doc, root, "enabled", this.IsEnabled.ToString());

        //    if (this.Configuration != null)
        //    {
        //        root.AppendChild(doc.ImportNode(this.Configuration, true));
        //    }

        //    return root;
        //}


        internal static LoggerSettings FromXml(XmlReader reader)
        {
            // TODO: shorten methods

            var elementName = reader.Name;
            var empty = reader.IsEmptyElement;
            var settings = new LoggerSettings
            {
                IsEnabled = true
            };

            // Read attributes.
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name.ToLowerInvariant())
                    {
                        case Constants.LoggerFriendlyNameLower:
                            settings.FriendlyName = reader.Value;
                            break;

                        case Constants.LoggerUriName:
                            try
                            {
                                settings.Uri = new Uri(reader.Value);
                            }
                            catch (UriFormatException)
                            {
                                throw new SettingsException(
                                    String.Format(
                                        CultureInfo.CurrentCulture,
                                        Resources.Resources.InvalidUriInSettings,
                                        reader.Value,
                                        elementName));
                            }
                            break;

                        case Constants.LoggerAssemblyQualifiedNameLower:
                            settings.AssemblyQualifiedName = reader.Value;
                            break;

                        case Constants.LoggerCodeBaseLower:
                            settings.CodeBase = reader.Value;
                            break;

                        case Constants.LoggerEnabledName:
                            bool.TryParse(reader.Value, out var value);
                            settings.IsEnabled = value;
                            break;

                        default:
                            throw new SettingsException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsXmlAttribute,
                                    elementName,
                                    reader.Name));
                    }
                }
            }

            // Check for required atttributes.
            if (string.IsNullOrWhiteSpace(settings.FriendlyName) &&
                string.IsNullOrWhiteSpace(settings.Uri?.ToString()) &&
                string.IsNullOrWhiteSpace(settings.AssemblyQualifiedName))
            {
                throw new SettingsException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.MissingLoggerAttributes,
                        elementName,
                        Constants.LoggerFriendlyName));
            }

            // Move to next node.
            reader.Read();

            // Return empty settings if previous element is empty.
            if (empty)
            {
                return settings;
            }

            // Read inner elements.
            while (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name.ToLowerInvariant())
                {
                    case Constants.LoggerConfigurationNameLower:
                        var document = new XmlDocument();
                        var element = document.CreateElement(reader.Name);
                        element.InnerXml = reader.ReadInnerXml();
                        settings.Configuration = element;
                        break;
                    default:
                        throw new SettingsException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.Resources.InvalidSettingsXmlElement,
                                elementName,
                                reader.Name));
                }
            }
            reader.ReadEndElement();

            return settings;
        }

        //private static void AppendAttribute(XmlDocument doc, XmlElement owner, string attributeName, string attributeValue)
        //{
        //    XmlAttribute attribute = doc.CreateAttribute(attributeName);
        //    attribute.Value = attributeValue;
        //    owner.Attributes.Append(attribute);
        //}
    }
}
