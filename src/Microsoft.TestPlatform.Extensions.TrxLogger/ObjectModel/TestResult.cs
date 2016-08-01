// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Class to uniquely identify test results
    /// </summary>
    public sealed class TestResultId : IXmlTestStore
    {
        #region Fields
        private Guid runId;

        // id of test within run
        private TestExecId executionId;

        private TestId testId;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResultId"/> class.
        /// </summary>
        /// <param name="runId">
        /// The run id.
        /// </param>
        /// <param name="executionId">
        /// The execution id.
        /// </param>
        /// <param name="parentExecutionId">
        /// The parent execution id.
        /// </param>
        /// <param name="testId">
        /// The test id.
        /// </param>
        public TestResultId(Guid runId, TestExecId executionId, TestId testId)
        {
            this.runId = runId;
            this.executionId = executionId;
            this.testId = testId;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the execution id.
        /// </summary>
        public TestExecId ExecutionId
        {
            get { return this.executionId; }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Override function for Equals
        /// </summary>
        /// <param name="obj">
        /// The object to compare
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            TestResultId tmpId = obj as TestResultId;
            if (tmpId == null)
            {
                return false;
            }

            return this.runId.Equals(tmpId.runId) && this.executionId.Equals((object)tmpId.executionId);
        }

        /// <summary>
        /// Override function for GetHashCode.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.runId.GetHashCode() ^ this.executionId.GetHashCode();
        }

        /// <summary>
        /// Override function for ToString.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return this.executionId.Id.ToString("B");
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
        /// The parameter
        /// </param>
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();

            if (this.executionId != null)
            {
                helper.SaveGuid(element, "@executionId", this.executionId.Id);
            }

            helper.SaveObject(this.testId, element, null);
        }

        #endregion
    }

    /// <summary>
    /// The test result error info class.
    /// </summary>
    internal sealed class TestResultErrorInfo : IXmlTestStore
    {
        [StoreXmlSimpleField("Message", "")]
        private string message;

        [StoreXmlSimpleField("StackTrace", "")]
        private string stackTrace;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResultErrorInfo"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public TestResultErrorInfo(string message)
        {
            Debug.Assert(message != null, "message is null");
            this.message = message;
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message
        {
            get { return this.message; }
            set { this.message = value; }
        }

        /// <summary>
        /// Gets or sets the stack trace.
        /// </summary>
        public string StackTrace
        {
            get { return this.stackTrace; }
            set { this.stackTrace = value; }
        }

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameter
        /// </param>
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence.SaveUsingReflection(element, this, typeof(TestResultErrorInfo), parameters);
        }

        #endregion
    }
}
