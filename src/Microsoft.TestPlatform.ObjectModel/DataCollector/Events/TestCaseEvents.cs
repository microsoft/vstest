// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Base class for all test case event arguments.
    /// </summary>
#if NET451
        [Serializable] 
#endif
    public abstract class TestCaseEventArgs : DataCollectionEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case.
        /// </param>
        protected TestCaseEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase)
            : base(context)
        {
            TestCaseId = testCaseId;
            //TcmInformation = tcmInformation;
            TestCaseName = testCaseName == null ? string.Empty : testCaseName;
            IsChildTestCase = isChildTestCase;
        }

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testElement">The test element of the test that this event is for.</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        protected TestCaseEventArgs(
            DataCollectionContext context,
            TestCase testElement
            //TcmInformation tcmInformation
            )
            : this(context, Guid.Empty, null, false)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.

            //todo
            //EqtAssert.ParameterNotNull(testElement, "testElement");

            TestElement = testElement;
            TestCaseId = testElement.Id;
            TestCaseName = testElement.DisplayName;
            //IsChildTestCase = testElement != null &&
            //                  !testElement.ParentExecId.Equals(TestExecId.Empty);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the test case ID
        /// </summary>
        public Guid TestCaseId
        {
            get;
            private set;
        }

        /// <summary>
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </summary>
        //public TcmInformation TcmInformation
        //{
        //    get;
        //    private set;
        //}

        /// <summary>
        /// Gets the test case name
        /// </summary>
        public string TestCaseName
        {
            get;
            private set;
        }

        /// <summary>
        /// True if this is a child test case, false if this is a top-level test case
        /// </summary>
        public bool IsChildTestCase
        {
            get;
            private set;
        }

        /// <summary>
        /// Test element of the test this event is for.
        /// </summary>
        public TestCase TestElement
        {
            get;
            internal set;
        }

        #endregion
    }

    /// <summary>
    /// Test Case Start event arguments.
    /// </summary>
#if NET451
        [Serializable]
#endif
    public sealed class TestCaseStartEventArgs : TestCaseEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case
        /// </param>
        internal TestCaseStartEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase)
            : base(context, testCaseId, testCaseName, isChildTestCase)
        {
            Debug.Assert(context.HasTestCase, "Context is not for a test case");
        }

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testElement">The test element of the test that this event is for.</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        public TestCaseStartEventArgs(
            DataCollectionContext context,
            TestCase testElement)
            //TcmInformation tcmInformation)
            : base(context, testElement)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.
        }

        #endregion
    }

    /// <summary>
    /// Test Case End event arguments.
    /// </summary>
#if NET451
        [Serializable] 
#endif
    public sealed class TestCaseEndEventArgs : TestCaseEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case
        /// </param>
        internal TestCaseEndEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase)
            : this(context, testCaseId, testCaseName, isChildTestCase, TestOutcome.Failed)
        {
        }

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case
        /// </param>
        /// <param name="testOutcome">The outcome of the test case.</param>
        internal TestCaseEndEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase,
            TestOutcome testOutcome)
            : base(context, testCaseId, testCaseName, isChildTestCase)
        {
            Debug.Assert(context.HasTestCase, "Context is not for a test case");
            this.TestOutcome = testOutcome;
        }

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testElement">The test element of the test that this event is for.</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testOutcome">The outcome of the test case.</param>
        public TestCaseEndEventArgs(
            DataCollectionContext context,
            TestCase testElement,
            //TcmInformation tcmInformation,
            TestOutcome testOutcome)
            : base(context, testElement)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.

            this.TestOutcome = testOutcome;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The outcome of the test.
        /// </summary>
        public TestOutcome TestOutcome
        {
            get;
            private set;
        }
        #endregion
    }

    /// <summary>
    /// Test Case Pause Event arguments.
    /// </summary>
#if NET451
    [Serializable]
#endif
    public sealed class TestCasePauseEventArgs : TestCaseEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case
        /// </param>
        internal TestCasePauseEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase)
            : base(context, testCaseId, testCaseName, isChildTestCase)
        {
            Debug.Assert(context.HasTestCase, "Context is not for a test case");
        }

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testElement">The test element of the test that this event is for.</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        public TestCasePauseEventArgs(
            DataCollectionContext context,
            TestCase testElement)
            //TcmInformation tcmInformation)
            : base(context, testElement)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.
        }

        #endregion
    }

    /// <summary>
    /// Test Case Resume Event arguments.
    /// </summary>
#if NET451
    [Serializable]
#endif
    public sealed class TestCaseResumeEventArgs : TestCaseEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case
        /// </param>
        internal TestCaseResumeEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase)
            : base(context, testCaseId, testCaseName, isChildTestCase)
        {
            Debug.Assert(context.HasTestCase, "Context is not for a test case");
        }

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testElement">The test element of the test that this event is for.</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        public TestCaseResumeEventArgs(
            DataCollectionContext context,
            TestCase testElement)
            //TcmInformation tcmInformation)
            : base(context, testElement)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.
        }

        #endregion
    }

    /// <summary>
    /// Test Case Reset Event arguments.
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class TestCaseResetEventArgs : TestCaseEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case
        /// </param>
        internal TestCaseResetEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase)
            : base(context, testCaseId, testCaseName, isChildTestCase)
        {
            Debug.Assert(context.HasTestCase, "Context is not for a test case");
        }

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testElement">The test element of the test that this event is for.</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        public TestCaseResetEventArgs(
            DataCollectionContext context,
            TestCase testElement)
            //TcmInformation tcmInformation)
            : base(context, testElement)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.
        }

        #endregion
    }

    /// <summary>
    /// Test Case Failed Event arguments.
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class TestCaseFailedEventArgs : TestCaseEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testCaseId">The test case ID</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="testCaseName">The test case name</param>
        /// <param name="isChildTestCase">
        /// True if this is a child test case, false if this is a top-level test case
        /// </param>
        /// <param name="failureType">The type of failure which has occured.</param>
        internal TestCaseFailedEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase,
            TestCaseFailureType failureType)
            : base(context, testCaseId, testCaseName, isChildTestCase)
        {
            Debug.Assert(context.HasTestCase, "Context is not for a test case");

            if (failureType < TestCaseFailureType.None || failureType > TestCaseFailureType.Other)
            {
                throw new ArgumentOutOfRangeException("failureType");
            }

            FailureType = failureType;
        }

        /// <summary>
        /// Initializes the instance by storing the given information.
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="testElement">The test element of the test that this event is for.</param>
        /// <param name="tcmInformation">
        /// Information used to obtain further data about the test from the Test Case Management (TCM) server,
        /// or null if the test did not originate from TCM.
        /// </param>
        /// <param name="failureType">The type of failure which has occured.</param>
        public TestCaseFailedEventArgs(
            DataCollectionContext context,
            TestCase testElement,
            //TcmInformation tcmInformation,
            TestCaseFailureType failureType)
            : base(context, testElement)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.

            FailureType = failureType;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The type of failure which occured.
        /// </summary>
        public TestCaseFailureType FailureType { get; private set; }

        #endregion
    }
}
