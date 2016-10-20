// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// The in procedure data collector settings.
    /// </summary>
    public class DataCollectorSettings
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
        /// Gets or sets value CodeBase of collector DLL. The syntax is same as Code Base in AssemblyName class.
        /// </summary>
        public string CodeBase
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
        public XmlElement ToXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement(Constants.DataCollectorSettingName);
            AppendAttribute(doc, root, "uri", this.Uri.ToString());
            AppendAttribute(doc, root, "assemblyQualifiedName", this.AssemblyQualifiedName);
            AppendAttribute(doc, root, "friendlyName", this.FriendlyName);

            root.AppendChild(doc.ImportNode(this.Configuration, true));

            return root;
        }

        /// <summary>
        /// The to xml.
        /// </summary>
        /// <param name="dataCollectorName">
        /// The data collector name.
        /// </param>
        /// <returns>
        /// The <see cref="XmlElement"/>.
        /// </returns>
        public XmlElement ToXml(string dataCollectorName)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement(dataCollectorName);
            AppendAttribute(doc, root, "uri", this.Uri.ToString());
            AppendAttribute(doc, root, "assemblyQualifiedName", this.AssemblyQualifiedName);
            AppendAttribute(doc, root, "friendlyName", this.FriendlyName);

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
        /// The <see cref="InProcDataCollectorSettings"/>.
        /// </returns>
        /// <exception cref="SettingsException">
        /// Settings exception
        /// </exception>
        internal static DataCollectorSettings FromXml(XmlReader reader)
        {
            DataCollectorSettings settings = new DataCollectorSettings();
            settings.IsEnabled = true;
            bool empty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "uri":
                            ValidateArg.NotNullOrEmpty(reader.Value, "uri");
                            settings.Uri = new Uri(reader.Value);
                            break;

                        case "assemblyQualifiedName":
                            ValidateArg.NotNullOrEmpty(reader.Value, "assemblyQualifiedName");
                            settings.AssemblyQualifiedName = reader.Value;
                            break;

                        case "friendlyName":
                            ValidateArg.NotNullOrEmpty(reader.Value, "FriendlyName");
                            settings.FriendlyName = reader.Value;
                            break;

                        case "enabled":
                            settings.IsEnabled = bool.Parse(reader.Value);
                            break;

                        case "codebase":
                            settings.CodeBase = reader.Value; // Optional.
                            break;

                        default:
                            throw new SettingsException(
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsXmlAttribute,
                                    Constants.DataCollectionRunSettingsName,
                                    reader.Name));
                    }
                }

            }

            if (settings.Uri == null)
            {
                throw new SettingsException(
                    String.Format(CultureInfo.CurrentCulture, Resources.Resources.MissingDataCollectorAttributes, "Uri"));
            }
            if (settings.AssemblyQualifiedName == null)
            {
                throw new SettingsException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.MissingDataCollectorAttributes,
                        "AssemblyQualifiedName"));
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
                                    Constants.DataCollectionRunSettingsName,
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
