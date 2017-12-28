// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// The logger settings.
    /// </summary>
    public class LoggerSetting
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name
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

        /// <summary>
        /// The to xml.
        /// </summary>
        /// <returns>
        /// The <see cref="XmlElement"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public XmlElement ToXml()
        {
            return ToXml(Constants.LoggerSettingName);
        }

        /// <summary>
        /// The to xml.
        /// </summary>
        /// <param name="loggerName">
        /// The logger name.
        /// </param>
        /// <returns>
        /// The <see cref="XmlElement"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public XmlElement ToXml(string loggerName)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement(loggerName);
            if (this.Name != null)
                AppendAttribute(doc, root, "name", this.Name);

            AppendAttribute(doc, root, "enabled", this.IsEnabled.ToString());

            if (this.Configuration != null)
                root.AppendChild(doc.ImportNode(this.Configuration, true));

            return root;
        }

        /// <summary>
        /// The from xml.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The <see cref="LoggerSettings"/>.
        /// </returns>
        /// <exception cref="SettingsException">
        /// Settings exception
        /// </exception>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        internal static LoggerSetting FromXml(XmlReader reader)
        {
            LoggerSetting settings = new LoggerSetting();
            settings.IsEnabled = true;
            bool empty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "name":
                            ValidateArg.NotNullOrEmpty(reader.Value, "name");
                            settings.Name = reader.Value;
                            break;

                        case "enabled":
                            settings.IsEnabled = bool.Parse(reader.Value);
                            break;

                        default:
                            throw new SettingsException(
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsXmlAttribute,
                                    Constants.LoggerRunSettingsName,
                                    reader.Name));
                    }
                }

            }

            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                // TODO: try one logger without name attr.
                // TODO: uncomment

                //throw new SettingsException(
                //String.Format(CultureInfo.CurrentCulture, Resources.Resources.MissingLoggerAttributes, "Name"));
            }

            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Configuration":
                            XmlDocument doc = new XmlDocument();
                            XmlElement element = doc.CreateElement("Configuration");
                            element.InnerXml = reader.ReadInnerXml();
                            settings.Configuration = element;
                            break;

                        default:
                            throw new SettingsException(
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsXmlElement,
                                    Constants.LoggerRunSettingsName,
                                    reader.Name));
                    }
                }
                reader.ReadEndElement();
            }
            return settings;
        }

        private static void AppendAttribute(XmlDocument doc, XmlElement owner, string attributeName, string attributeValue)
        {
            XmlAttribute attribute = doc.CreateAttribute(attributeName);
            attribute.Value = attributeValue;
            owner.Attributes.Append(attribute);
        }
    }
}
