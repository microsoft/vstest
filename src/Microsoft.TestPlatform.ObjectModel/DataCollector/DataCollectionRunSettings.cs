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
    /// The in procedure data collection run settings.
    /// </summary>
    public class DataCollectionRunSettings : TestRunSettings
    {
        private string dataCollectionRootName = string.Empty;

        private string dataCollectionsName = string.Empty;

        private string dataCollectorName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRunSettings"/> class.
        /// </summary>
        public DataCollectionRunSettings() : base(Constants.DataCollectionRunSettingsName)
        {
            this.DataCollectorSettingsList = new Collection<DataCollectorSettings>();
            this.dataCollectionRootName = Constants.DataCollectionRunSettingsName;
            this.dataCollectionsName = Constants.DataCollectorsSettingName;
            this.dataCollectorName = Constants.DataCollectorSettingName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRunSettings"/> class.
        /// </summary>
        /// <param name="dataCollectionRootName">
        /// The data collection root name.
        /// </param>
        /// <param name="dataCollectionsName">
        /// The data collections name.
        /// </param>
        /// <param name="dataCollectorName">
        /// The data collector name.
        /// </param>
        public DataCollectionRunSettings(
            string dataCollectionRootName,
            string dataCollectionsName,
            string dataCollectorName)
            : base(dataCollectionRootName)
        {
            this.dataCollectionRootName = dataCollectionRootName;
            this.dataCollectionsName = dataCollectionsName;
            this.dataCollectorName = dataCollectorName;
            this.DataCollectorSettingsList = new Collection<DataCollectorSettings>();
        }

        /// <summary>
        /// Gets the in procedure data collector settings list.
        /// </summary>
        public Collection<DataCollectorSettings> DataCollectorSettingsList
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether is in procedure data collection enabled.
        /// </summary>
        public bool IsCollectionEnabled
        {
            get
            {
                return this.DataCollectorSettingsList.Any<DataCollectorSettings>(setting => setting.IsEnabled);
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
            XmlElement root = doc.CreateElement(this.dataCollectionRootName);
            XmlElement subRoot = doc.CreateElement(this.dataCollectionsName);
            root.AppendChild(subRoot);

            foreach (var collectorSettings in this.DataCollectorSettingsList)
            {
                XmlNode child = doc.ImportNode(collectorSettings.ToXml(this.dataCollectorName), true);
                subRoot.AppendChild(child);
            }

            return root;
        }

        /// <summary>
        /// The from xml.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The <see cref="DataCollectionRunSettings"/>.
        /// </returns>
        /// <exception cref="SettingsException">
        /// Settings exception
        /// </exception>
        public static DataCollectionRunSettings FromXml(XmlReader reader)
        {
            return CreateDataCollectionRunSettings(
                reader,
                Constants.DataCollectionRunSettingsName,
                Constants.DataCollectorsSettingName,
                Constants.DataCollectorSettingName);
        }

        public static DataCollectionRunSettings FromXml(XmlReader reader, string dataCollectionName, string dataCollectorsName, string dataCollectorName)
        {
            return CreateDataCollectionRunSettings(reader, dataCollectionName, dataCollectorsName, dataCollectorName);
        }

        public static DataCollectionRunSettings CreateDataCollectionRunSettings(
            XmlReader reader, string dataCollectionName,            
            string dataCollectorsName, string dataCollectorName)
        {
            ValidateArg.NotNull<XmlReader>(reader, "reader");
            ValidateArg.NotNull<string>(dataCollectorsName, "dataCollectorsName");
            ValidateArg.NotNull<string>(dataCollectorName, "dataCollectorName");

            DataCollectionRunSettings settings = new DataCollectionRunSettings(dataCollectionName, dataCollectorsName, dataCollectorName);
            bool empty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                throw new SettingsException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.InvalidSettingsXmlAttribute,
                        dataCollectorsName,
                        reader.Name));
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name.Equals(dataCollectorsName))
                    {
                        var items = ReadListElementFromXml(reader, dataCollectorName);
                            foreach (var item in items)
                            {
                                settings.DataCollectorSettingsList.Add(item);
                            }
                    }
                    else
                    {
                        throw new SettingsException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.Resources.InvalidSettingsXmlElement,
                                dataCollectorsName,
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
        internal static List<DataCollectorSettings> ReadListElementFromXml(XmlReader reader, string dataCollectorsName)
        {
            List<DataCollectorSettings> settings = new List<DataCollectorSettings>();
            bool empty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                throw new SettingsException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.InvalidSettingsXmlAttribute,
                        dataCollectorsName,
                        reader.Name));
            }

            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name.Equals(dataCollectorsName))
                    {
                        settings.Add(DataCollectorSettings.FromXml(reader));
                    }
                    else
                    {
                        throw new SettingsException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsXmlElement,
                                    dataCollectorsName,
                                    reader.Name));
                    }
                }

                reader.ReadEndElement();
            }

            return settings;
        }
    }
}
