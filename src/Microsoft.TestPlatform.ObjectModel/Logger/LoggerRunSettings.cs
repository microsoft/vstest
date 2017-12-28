// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// The logger run settings.
    /// </summary>
    public class LoggerRunSettings : TestRunSettings
    {
        private string loggerRootName = string.Empty;
        private string loggersName = string.Empty;
        private string loggerName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerRunSettings"/> class.
        /// </summary>
        public LoggerRunSettings() : base(Constants.DataCollectionRunSettingsName)
        {
            this.LoggerSettings = new Collection<LoggerSetting>();
            this.loggerRootName = Constants.LoggerRunSettingsName;
            this.loggersName = Constants.LoggersSettingName;
            this.loggerName = Constants.LoggerSettingName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerRunSettings"/> class.
        /// </summary>
        /// <param name="loggerRootName">
        /// The loggers root name.
        /// </param>
        /// <param name="loggersName">
        /// The logger collection name.
        /// </param>
        /// <param name="loggerName">
        /// The logger name.
        /// </param>
        public LoggerRunSettings(
            string loggerRootName,
            string loggersName,
            string loggerName)
            : base(loggerRootName)
        {
            this.loggerRootName = loggerRootName;
            // TODO: change loggerName and loggersName
            this.loggersName = loggersName;
            this.loggerName = loggerName;
            this.LoggerSettings = new Collection<LoggerSetting>();
        }

        /// <summary>
        /// Gets the logger settings list.
        /// </summary>
        public Collection<LoggerSetting> LoggerSettings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether logger is enabled.
        /// </summary>
        public bool IsLoggerEnabled
        {
            get
            {
                return this.LoggerSettings.Any<LoggerSetting>(setting => setting.IsEnabled);
            }
        }

        /// <summary>
        /// The to xml.
        /// </summary>
        /// <returns>
        /// The <see cref="XmlElement"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public override XmlElement ToXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement(this.loggerRootName);
            XmlElement subRoot = doc.CreateElement(this.loggersName);
            root.AppendChild(subRoot);

            foreach (var loggerSettings in this.LoggerSettings)
            {
                XmlNode child = doc.ImportNode(loggerSettings.ToXml(this.loggerName), true);
                subRoot.AppendChild(child);
            }

            return root;
        }

        /// <summary>
        /// The from xml.
        /// </summary>
        /// <param name="reader">
        /// The xml reader.
        /// </param>
        /// <returns>
        /// The <see cref="LoggerRunSettings"/>.
        /// </returns>
        /// <exception cref="SettingsException">
        /// Settings exception
        /// </exception>
        public static LoggerRunSettings FromXml(XmlReader reader)
        {
            return CreateLoggerRunSettings(
                reader,
                Constants.LoggerRunSettingsName,
                Constants.LoggersSettingName,
                Constants.LoggerSettingName);
        }

        /// <summary>
        /// The from xml.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <param name="loggerRootName">The loggers root name.</param>
        /// <param name="loggersName">The logger collection name.</param>
        /// <param name="loggerName">The logger name.</param>
        /// <returns>
        /// The <see cref="LoggerRunSettings"/>.
        /// </returns>
        /// <exception cref="SettingsException">
        /// Settings exception
        /// </exception>
        public static LoggerRunSettings FromXml(XmlReader reader, string loggerRootName, string loggersName, string loggerName)
        {
            return CreateLoggerRunSettings(reader, loggerRootName, loggersName, loggerName);
        }

        /// <summary>
        /// Creates logger run settings object from xml reader.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <param name="loggerRootName">The loggers root name.</param>
        /// <param name="loggersName">The logger collection name.</param>
        /// <param name="loggerName">The logger name.</param>
        /// <returns>The <see cref="LoggerRunSettings"/>.</returns>
        public static LoggerRunSettings CreateLoggerRunSettings(
            XmlReader reader, string loggerRootName,
            string loggersName, string loggerName)
        {
            ValidateArg.NotNull<XmlReader>(reader, "reader");
            ValidateArg.NotNull<string>(loggersName, "loggersName");
            ValidateArg.NotNull<string>(loggerName, "loggerName");

            LoggerRunSettings settings = new LoggerRunSettings(loggerRootName, loggersName, loggerName);
            bool empty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                throw new SettingsException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.InvalidSettingsXmlAttribute,
                        loggersName,
                        reader.Name));
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name.Equals(loggersName))
                    {
                        var items = ReadListElementFromXml(reader, loggerName);
                        foreach (var item in items)
                        {
                            settings.LoggerSettings.Add(item);
                        }
                    }
                    else
                    {
                        throw new SettingsException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.Resources.InvalidSettingsXmlElement,
                                loggersName,
                                reader.Name));
                    }
                }

                reader.ReadEndElement();
            }

            return settings;
        }

        /// <summary>
        /// The read list element from xml.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        /// <exception cref="SettingsException">
        /// </exception>
        internal static List<LoggerSetting> ReadListElementFromXml(XmlReader reader, string loggersName)
        {
            List<LoggerSetting> settings = new List<LoggerSetting>();
            bool empty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                throw new SettingsException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.InvalidSettingsXmlAttribute,
                        loggersName,
                        reader.Name));
            }

            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name.Equals(loggersName))
                    {
                        settings.Add(LoggerSetting.FromXml(reader));
                    }
                    else
                    {
                        throw new SettingsException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsXmlElement,
                                    loggersName,
                                    reader.Name));
                    }
                }

                reader.ReadEndElement();
            }

            return settings;
        }
    }
}
