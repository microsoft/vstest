// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;

    /// <summary>
    /// Class representing the context in which data collection occurs.
    /// </summary>
#if NET46
    [Serializable]
#endif
    public class DataCollectionContext
    {
        #region Private Fields

        /// <summary>
        /// The session id.
        /// </summary>
        private readonly SessionId sessionId;

        /// <summary>
        /// The test exec id.
        /// </summary>
        private readonly TestExecId testExecId;

        /// <summary>
        /// The hash code.
        /// </summary>
        private readonly int hashCode;

        #endregion

        #region Constructors

        // NOTE: These constructors are protected internal to allow 3rd parties to
        //       do unit testing of their data collectors.
        // 
        //       We do not want to make the constructors of this class public as it
        //       would lead to a great deal of user error when they start creating
        //       their own data collection context instances to log errors/warnings
        //       or send files with.  The potential for this type of error still
        //       exists by having the protected constructor, but it is less likely
        //       and we have added safeguards in our DataCollectinLogger and
        //       DataCollectionDataSink to safeguard against derived types being
        //       passed to us.
        //
        //       In order to create mock instances of the DataCollectionContext for
        //       unit testing purposes, 3rd parties can derive from this class and
        //       have public constructors.  This will allow them to instantiate their
        //       class and pass to us for creating data collection events.

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionContext"/> class indicating that there is a session,
        /// but no executing test, in context.
        /// </summary>
        /// <param name="sessionId">
        /// The session under which the data collection occurs.  Cannot be null.
        /// </param>
        protected internal DataCollectionContext(SessionId sessionId)
            : this(sessionId, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionContext"/> class indicating that there is a session and an executing test,
        /// but no test step, in context.
        /// </summary>
        /// <param name="sessionId">
        /// The session under which the data collection occurs.  Cannot be null.
        /// </param>
        /// <param name="testExecId">
        /// The test execution under which the data collection occurs,
        /// or null if no executing test case is in context
        /// </param>
        protected internal DataCollectionContext(SessionId sessionId, TestExecId testExecId)
        {
            ValidateArg.NotNull(sessionId, "sessionId");

            this.sessionId = sessionId;
            this.testExecId = testExecId;
            this.hashCode = this.ComputeHashCode();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the session under which the data collection occurs.
        /// </summary>
        public SessionId SessionId
        {
            get
            {
                return this.sessionId;
            }
        }

        /// <summary>
        /// Gets the test execution under which the data collection occurs,
        /// or null if no such test exists.
        /// </summary>
        public TestExecId TestExecId
        {
            get
            {
                return this.testExecId;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is an executing test case associated with this context.
        /// </summary>
        public bool HasTestCase
        {
            get { return this.testExecId != null; }
        }

        #endregion

        #region Equals and Hashcode

        /// <summary>
        /// The ==.
        /// </summary>
        /// <param name="context1">
        /// The context 1.
        /// </param>
        /// <param name="context2">
        /// The context 2.
        /// </param>
        /// <returns>Value indicating whether the data collection contexts are equal.
        /// </returns>
        public static bool operator ==(DataCollectionContext context1, DataCollectionContext context2)
        {
            return object.Equals(context1, context2);
        }

        /// <summary>
        /// The !=.
        /// </summary>
        /// <param name="context1">
        /// The context 1.
        /// </param>
        /// <param name="context2">
        /// The context 2.
        /// </param>
        /// <returns>Value indicating whether the data collection contexts are equal.
        /// </returns>
        public static bool operator !=(DataCollectionContext context1, DataCollectionContext context2)
        {
            return !(context1 == context2);
        }

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="obj">
        /// The object.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var other = obj as DataCollectionContext;

            if (other == null)
            {
                return false;
            }

            return this.sessionId.Equals(other.sessionId)
                && (this.testExecId == null ? other.testExecId == null : this.testExecId.Equals(other.testExecId));
        }

        /// <summary>
        /// The get hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// The compute hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private int ComputeHashCode()
        {
            var hashCode = 17;

            hashCode = 31 * hashCode + sessionId.GetHashCode();

            if (this.testExecId != null)
            {
                hashCode = 31 * hashCode + this.testExecId.GetHashCode();
            }

            return hashCode;
        }

        #endregion
    }
}
