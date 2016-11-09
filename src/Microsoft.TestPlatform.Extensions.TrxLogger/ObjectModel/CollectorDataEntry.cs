// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Data entry from a collector along with information what this collector is and what agent it was collected on.
    /// Instances are created by Agent and sent to Controller. Then controller puts agent names in.
    /// The user is not supposed to create instances of this.
    /// </summary>
    internal class CollectorDataEntry : IXmlTestStore
    {
        #region Private Fields

        /// <summary>
        /// List of data attachments. These attachments can be things such as files that the
        /// collector wants to make available to the publishers.
        /// </summary>
        private readonly List<IDataAttachment> attachments;

        /// <summary>
        /// Name of the agent from which we received the data
        /// </summary>
        private string agentName;

        /// <summary>
        /// Display name of the agent from which we received the data
        /// </summary>
        private string agentDisplayName;

        /// <summary>
        /// Flag indicating whether this data is coming from a remote (not hosted) agent
        /// </summary>
        private bool isFromRemoteAgent;

        /// <summary>
        /// URI of the collector.
        /// </summary>
        private Uri uri;

        /// <summary>
        /// Name of the collector that should be displayed to the user.
        /// </summary>
        private string collectorDisplayName;

        #endregion

        #region Constructor

        /// <summary>
        /// Used by the aggregator to put collector Uri, agentName, string agentDisplayName, whether it's remote data, and
        /// Attachments.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="collectorDisplayName">
        /// The collector Display Name.
        /// </param>
        /// <param name="agentName">
        /// The agent Name.
        /// </param>
        /// <param name="agentDisplayName">
        /// The agent Display Name.
        /// </param>
        /// <param name="isFromRemoteAgent">
        /// Is From Remote Agent.
        /// </param>
        /// <param name="attachments">
        /// The attachments.
        /// </param>
        public CollectorDataEntry(Uri uri, string collectorDisplayName, string agentName, string agentDisplayName, bool isFromRemoteAgent, IList<IDataAttachment> attachments)
            : this()
        {
            this.Initialize(uri, collectorDisplayName, agentName, agentDisplayName, isFromRemoteAgent, attachments);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectorDataEntry"/> class.
        /// </summary>
        /// <remarks>
        /// For XML persistence
        /// </remarks>
        internal CollectorDataEntry()
        {
            this.attachments = new List<IDataAttachment>();
        }

        /// <summary>
        /// Copies the specified collector data entry, making the paths in the data attachments absolute or relative, with
        /// respect to the results directory
        /// </summary>
        /// <param name="other">The instance to copy from</param>
        /// <param name="resultsDirectory">The results directory to use to make paths in the data attachments relative or absolute</param>
        /// <param name="useAbsolutePaths">True to use absolute paths in this instance, false to use relative paths</param>
        private CollectorDataEntry(CollectorDataEntry other, string resultsDirectory, bool useAbsolutePaths)
        {
            Debug.Assert(other != null, "'other' is null");
            Debug.Assert(other.attachments != null, "'other.m_attachments' is null");
            Debug.Assert(!string.IsNullOrEmpty(resultsDirectory), "'resultsDirectory' is null or empty");

            this.attachments = new List<IDataAttachment>(other.attachments.Count);
            this.Initialize(other.uri, other.collectorDisplayName, other.agentName, other.agentDisplayName, other.isFromRemoteAgent, null);

            // Clone the attachments
            foreach (IDataAttachment attachment in other.attachments)
            {
                Debug.Assert(attachment != null, "'attachment' is null");

                UriDataAttachment uriDataAttachment = attachment as UriDataAttachment;
                if (uriDataAttachment != null)
                {
                    this.attachments.Add(uriDataAttachment.Clone(resultsDirectory, useAbsolutePaths));
                }
                else
                {
                    this.attachments.Add(attachment);
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the read-only list of data attachments
        /// </summary>
        public IList<IDataAttachment> Attachments
        {
            get
            {
                return this.attachments.AsReadOnly();
            }
        }

        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the state to the XML element
        /// </summary>
        /// <param name="element">The XML element to save to</param>
        /// <param name="parameters">Parameters to customize the save behavior</param>
        public void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            EqtAssert.ParameterNotNull(element, "element");

            XmlPersistence helper = new XmlPersistence();
            helper.SaveSimpleField(element, "@agentName", this.agentName, null);
            helper.SaveSimpleField(element, "@agentDisplayName", this.agentDisplayName, this.agentName);
            helper.SaveSimpleField(element, "@isFromRemoteAgent", this.isFromRemoteAgent, false);
            helper.SaveSimpleField(element, "@uri", this.uri.AbsoluteUri, null);
            helper.SaveSimpleField(element, "@collectorDisplayName", this.collectorDisplayName, string.Empty);

            IList<UriDataAttachment> uriAttachments = new List<UriDataAttachment>();
            foreach (IDataAttachment attachment in this.Attachments)
            {
                UriDataAttachment uriAtt = attachment as UriDataAttachment;
                if (uriAtt != null)
                {
                    uriAttachments.Add(uriAtt);
                }
            }

            helper.SaveIEnumerable(uriAttachments, element, "UriAttachments", "A", "UriAttachment", parameters);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Adds a data attachment to the list of data attachments
        /// </summary>
        /// <param name="attachment">The attachment to add</param>
        internal void AddAttachment(IDataAttachment attachment)
        {
            if (attachment == null)
            {
                throw new ArgumentNullException(nameof(attachment));
            }

            this.attachments.Add(attachment);
        }

        /// <summary>
        /// Clones the instance and attachments, with file paths in file attachments absolute or relative as specified
        /// </summary>
        /// <param name="resultsDirectory">The results directory to use to make paths in the data attachments relative or absolute</param>
        /// <param name="useAbsolutePaths">True to use absolute paths in this instance, false to use relative paths</param>
        /// <returns>A clone of the instance containing cloned attachments with file paths made absolute or relative</returns>
        internal CollectorDataEntry Clone(string resultsDirectory, bool useAbsolutePaths)
        {
            Debug.Assert(!string.IsNullOrEmpty(resultsDirectory), "'resultsDirectory' is null or empty");
            Debug.Assert(resultsDirectory == resultsDirectory.Trim(), "'resultsDirectory' contains whitespace at the ends");

            return new CollectorDataEntry(this, resultsDirectory, useAbsolutePaths);
        }

        #endregion

        #region Private Methods

        private void Initialize(Uri uri, string collectorDisplayName, string agentName, string agentDisplayName, bool isFromRemoteAgent, IEnumerable<IDataAttachment> attachments)
        {
            EqtAssert.ParameterNotNull(uri, "uri");
            EqtAssert.StringNotNullOrEmpty(collectorDisplayName, "collectorDisplayName");
            EqtAssert.StringNotNullOrEmpty(agentName, "agentName");
            EqtAssert.StringNotNullOrEmpty(agentDisplayName, "agentDisplayName");

            if (null != attachments)
            {
                // Copy the attachments
                foreach (IDataAttachment attachment in attachments)
                {
                    this.AddAttachment(attachment);
                }
            }

            // Note that the data can be null.
            this.uri = uri;
            this.collectorDisplayName = collectorDisplayName;
            this.agentName = agentName.Trim();
            this.agentDisplayName = agentDisplayName.Trim();
            this.isFromRemoteAgent = isFromRemoteAgent;
        }

        #endregion
    }
}
