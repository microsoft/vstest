// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Class that provides a basic implementation of IUriAttachment, which can be used by plugin
    /// writers to send any resource accessible by a URI as an attachment.
    /// </summary>
    internal class UriDataAttachment : IDataAttachment, IXmlTestStore
    {
        private readonly TrxFileHelper trxFileHelper;
        #region Private fields

        /// <summary>
        /// The name for the attachment
        /// </summary>
        private string description;

        /// <summary>
        /// The URI pointing to the resource that forms the data for this attachment
        /// </summary>
        private Uri uri;

        #endregion

        /// <summary>
        /// Initializes the URI data attachment
        /// </summary>
        /// <param name="description">Short description for the attachment</param>
        /// <param name="uri">The URI pointing to the resource</param>
        /// <param name="trxFileHelper">InternalFileHelper class instance to use in file operations.</param>
        /// <exception cref="ArgumentException">'name' is null or empty</exception>
        /// <exception cref="ArgumentNullException">'uri' is null</exception>
        public UriDataAttachment(string description, Uri uri, TrxFileHelper trxFileHelper)
        {
            this.trxFileHelper = trxFileHelper;

            Initialize(description, uri);
        }

        #region IDataAttachment Members

        /// <summary>
        /// Gets short description for the attachment.
        /// </summary>
        public string Description
        {
            get
            {
                return this.description;
            }
        }

        /// <summary>
        /// Gets the URI that can be used to obtain the data of this attachment
        /// </summary>
        public Uri Uri
        {
            get
            {
                return this.uri;
            }
        }

        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement.
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameter
        /// </param>
        public void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            EqtAssert.ParameterNotNull(element, "element");

            XmlPersistence helper = new XmlPersistence();
            helper.SaveSimpleField(element, ".", this.description, null);

            // The URI is not a true URI, it must always be a local path represented as a URI. Also, the URI can be absolute or
            // relative. We use OriginalString because:
            //   - ToString gets a string in the form "file://..." for an absolute URI and what was passed in for a relative URI
            //   - AbsoluteUri only works for an absolute URI naturally
            //   - LocalPath only works for an absolute URI
            // Due to the above assumption, that it is always an absolute or relative local path to a file, it's simplest and
            // safest to treat the URI as a string and just use OriginalString.
            helper.SaveSimpleField(element, "@href", this.uri.OriginalString, null);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Clones the instance and makes the URI in the clone absolute using the specified base directory
        /// </summary>
        /// <param name="baseDirectory">The base directory to use to make the URI absolute</param>
        /// <param name="useAbsoluteUri">True to use an absolute URI in the clone, false to use a relative URI</param>
        /// <returns>A clone of the instance, with the URI made absolute</returns>
        internal UriDataAttachment Clone(string baseDirectory, bool useAbsoluteUri)
        {
            Debug.Assert(!string.IsNullOrEmpty(baseDirectory), "'baseDirectory' is null or empty");
            Debug.Assert(baseDirectory == baseDirectory.Trim(), "'baseDirectory' contains whitespace at the ends");

            if (useAbsoluteUri != this.uri.IsAbsoluteUri)
            {
                Uri uriToUse;
                if (useAbsoluteUri)
                {
                    uriToUse = new Uri(Path.Combine(baseDirectory, this.uri.OriginalString), UriKind.Absolute);
                }
                else
                {
                    uriToUse = new Uri(trxFileHelper.MakePathRelative(this.uri.OriginalString, baseDirectory), UriKind.Relative);
                }

                return new UriDataAttachment(this.description, uriToUse, trxFileHelper);
            }

            // The URI in this instance is already how we want it, and since this class is immutable, no need to clone
            return this;
        }

        #endregion

        #region Private Methods

        private void Initialize(string desc, Uri uri)
        {
            EqtAssert.ParameterNotNull(desc, "desc");
            EqtAssert.ParameterNotNull(uri, "uri");

            this.description = desc;
            this.uri = uri;
        }

        #endregion
    }
}
