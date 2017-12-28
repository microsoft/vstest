// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Logging
{
    using System.Collections.Generic;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Utility class that loggers can use to read name/value configuration information from the XML element sent to them
    /// </summary>
    internal class LoggerNameValueConfigurationManager
    {
        /// <summary>
        /// The name/value pairs loaded from the configuration XML element
        /// </summary>
        private Dictionary<string, string> nameValuePairs = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerNameValueConfigurationManager"/> class.
        /// Loads the configuration name/value information from the provided XML element into a dictionary
        /// </summary>
        /// <param name="configurationElement">
        /// XML element containing the configuration
        /// </param>
        public LoggerNameValueConfigurationManager(XmlElement configurationElement)
        {
            Initialize(configurationElement);
        }

        public Dictionary<string, string> NameValuePairs => this.nameValuePairs;

        private void Initialize(XmlElement configurationElement)
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
                string settingName = settingElement.Name;

                // Get the setting value
                string settingValue = settingElement.InnerText;

                if (settingValue.Contains(";") || settingValue.Contains("="))
                {
                    if (EqtTrace.IsWarningEnabled)
                        EqtTrace.Warning("Skipping configuration setting '{0}' due to invalid value '{1}'", settingName, settingValue);

                    continue;
                }

                if (string.IsNullOrWhiteSpace(settingValue))
                {
                    if (EqtTrace.IsWarningEnabled)
                        EqtTrace.Warning("Skipping configuration setting '{0}' due to missing value", settingName);

                    continue;
                }

                // Save the name/value pair in the dictionary. Note that duplicate settings are
                // overwritten with the last occurrance's value.
                if (this.nameValuePairs.ContainsKey(settingName))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(
                            "Duplicate configuration setting found for '{0}'. Using the last setting.",
                            settingName);
                    }
                }

                this.nameValuePairs[settingName] = settingValue;
            }
        }
    }
}
