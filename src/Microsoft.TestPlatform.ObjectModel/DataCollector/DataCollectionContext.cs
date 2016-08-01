// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    /// <summary>
    /// Class representing the context in which data collection occurs.
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public class DataCollectionContext
    {
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
        /// Constructs a DataCollectionContext indicating that there is a session,
        /// but no executing test, in context.
        /// </summary>
        /// <param name="sessionId">The session under which the data collection occurs.  Cannot be null.</param>
        protected internal DataCollectionContext(SessionId sessionId)
            : this(sessionId, null)
        {
        }

        /// <summary>
        /// Constructs a DataCollectionContext indicating that there is a session and an executing test,
        /// but no test step, in context.
        /// </summary>
        /// <param name="sessionId">The session under which the data collection occurs.  Cannot be null.</param>
        /// <param name="testExecId">The test execution under which the data collection occurs,
        /// or null if no executing test case is in context</param>
        protected internal DataCollectionContext(SessionId sessionId, TestExecId testExecId)
        {
            //todo
            //EqtAssert.ParameterNotNull(sessionId, "sessionId");

            this.sessionId = sessionId;
            this.testExecId = testExecId;
            this.hashCode = ComputeHashCode();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Identifies the session under which the data collection occurs.  Will not be null.
        /// </summary>
        public SessionId SessionId
        {
            get
            {
                return sessionId;
            }
        }

        /// <summary>
        /// Identifies the test execution under which the data collection occurs,
        /// or null if no such test exists.
        /// </summary>
        public TestExecId TestExecId
        {
            get
            {
                return testExecId;
            }
        }

        /// <summary>
        /// Returns true if there is an executing test case associated with this context.
        /// </summary>
        public bool HasTestCase
        {
            get { return testExecId != null; }
        }

        #endregion

        #region Equals and Hashcode

        public static bool operator ==(DataCollectionContext context1, DataCollectionContext context2)
        {
            return object.Equals(context1, context2);
        }

        public static bool operator !=(DataCollectionContext context1, DataCollectionContext context2)
        {
            return !(context1 == context2);
        }

        public override bool Equals(object obj)
        {
            DataCollectionContext other = obj as DataCollectionContext;

            if (other == null)
            {
                return false;
            }

            return sessionId.Equals(other.sessionId)
                && (testExecId == null ? other.testExecId == null : testExecId.Equals(other.testExecId));
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        #endregion

        #region Private Methods

        private int ComputeHashCode()
        {
            int hashCode = 17;

            hashCode = 31 * hashCode + sessionId.GetHashCode();

            if (testExecId != null)
            {
                hashCode = 31 * hashCode + testExecId.GetHashCode();
            }

            return hashCode;
        }

        #endregion

        #region Private Fields

        private readonly SessionId sessionId;
        private readonly TestExecId testExecId;
        private readonly int hashCode;

        #endregion
    }
}
