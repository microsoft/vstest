// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Utility class that collectors can use to read name/value configuration information from
    /// the XML element sent to them
    /// </summary>
    internal class CollectorNameValueConfigurationManager
    {
        #region Private constants

        // Configuration XML constants
        private const string ConfigurationNodeName = "Configuration";

        private const string SettingNodeName = "Setting";

        private const string SettingNameAttributeName = "name";

        private const string SettingValueAttributeName = "value";

        #endregion

        #region Private fields

        /// <summary>
        /// The name/value pairs loaded from the configuration XML element
        /// </summary>
        private Dictionary<string, string> nameValuePairs = new Dictionary<string, string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectorNameValueConfigurationManager"/> class. 
        /// Loads the configuration name/value information from the provided XML element into a
        /// dictionary
        /// </summary>
        /// <param name="configurationElement">
        /// XML element containing the configuration
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        public CollectorNameValueConfigurationManager(XmlElement configurationElement)
        {
            if (configurationElement == null)
            {
                // There is no configuration
                return;
            }

            // Iterate through top-level XML elements within the configuration element and store
            // name/value information for elements that have name/value attributes.
            foreach (XmlNode settingNode in configurationElement.ChildNodes)
            {
                // Skip all non-elements
                var settingElement = settingNode as XmlElement;
                if (settingElement == null)
                {
                    continue;
                }

                // Get the setting name
                string settingName = settingElement.GetAttribute(SettingNameAttributeName);
                if (string.IsNullOrEmpty(settingName))
                {
                    EqtTrace.Warning("Skipping configuration setting due to missing setting name");
                    continue;
                }

                // Get the setting value
                string settingValue = settingElement.GetAttribute(SettingValueAttributeName);
                if (string.IsNullOrEmpty(settingValue))
                {
                    EqtTrace.Warning("Skipping configuration setting '{0}' due to missing value", settingName);
                    continue;
                }

                // Save the name/value pair in the dictionary. Note that duplicate settings are
                // overwritten with the last occurrance's value.
                if (this.nameValuePairs.ContainsKey(settingName))
                {
                    EqtTrace.Verbose(
                        "Duplicate configuration setting found for '{0}'. Using the last setting.",
                        settingName);
                }

                this.nameValuePairs[settingName] = settingValue;
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the value of the setting specified by name, or null if it was not found
        /// </summary>
        /// <param name="name">The setting name</param>
        /// <returns>The setting value, or null if the setting was not found</returns>
        public string this[string name]
        {
            get
            {
                if (name == null)
                {
                    return null;
                }

                string settingValue;
                this.nameValuePairs.TryGetValue(name, out settingValue);
                return settingValue;
            }
            set
            {
                this.nameValuePairs[name] = value;
            }
        }

        /// <summary>
        /// Export the settings back to xml so it can be saved back to the configuration file
        /// </summary>
        /// <returns>XML in the format expected by DataCollectorSettings.Configuration</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public XmlElement ExportToXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode mainnode = doc.CreateNode(XmlNodeType.Element, ConfigurationNodeName, string.Empty);
            doc.AppendChild(mainnode);

            foreach (string setting in this.nameValuePairs.Keys)
            {
                XmlNode node = doc.CreateNode(XmlNodeType.Element, SettingNodeName, string.Empty);

                // create the attribute for the setting name
                XmlAttribute attr = doc.CreateAttribute(SettingNameAttributeName);
                attr.Value = setting;
                node.Attributes.Append(attr);

                // create the attribute for the setting value
                attr = doc.CreateAttribute(SettingValueAttributeName);
                attr.Value = this.nameValuePairs[setting];
                node.Attributes.Append(attr);

                mainnode.AppendChild(node);
            }

            return doc.DocumentElement;
        }

        #endregion
    }
}

