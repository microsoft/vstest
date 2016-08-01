// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// This is a record about one run-level event that happened at run execution or around it.
    /// </summary>
    internal sealed class RunInfo : IXmlTestStore
    {
        #region Fields

        [StoreXmlSimpleField("Text", "")]
        private string text;

        private Exception exception;

        [StoreXmlSimpleField("@computerName", "")]
        private string computer;

        [StoreXmlSimpleField("@outcome")]
        private TestOutcome outcome;

        [StoreXmlSimpleField]
        private DateTime timestamp;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RunInfo"/> class.
        /// </summary>
        /// <param name="textMessage">
        /// The text message.
        /// </param>
        /// <param name="ex">
        /// The exception
        /// </param>
        /// <param name="computer">
        /// The computer.
        /// </param>
        /// <param name="outcome">
        /// The outcome.
        /// </param>
        public RunInfo(string textMessage, Exception ex, string computer, TestOutcome outcome)
        {
            Debug.Assert(computer != null, "computer is null");

            this.text = textMessage;
            this.exception = ex;
            this.computer = computer;
            this.outcome = outcome;
            this.timestamp = DateTime.Now.ToUniversalTime();
        }

        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();
            helper.SaveSingleFields(element, this, parameters);
            helper.SaveSimpleField(element, "Exception", this.exception, null);
        }

        #endregion
    }
}
