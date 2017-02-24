// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Data Request event arguments
    /// </summary>
#if NET46
    [Serializable]
#endif
    public sealed class DataRequestEventArgs : TestCaseEventArgs
    {
        #region Constants

        /// <summary>
        /// Default test case ID used when sending the event for a session data request
        /// </summary>
        private static readonly Guid DefaultTestCaseId = Guid.Empty;

        /// <summary>
        /// Default test case name used when sending the event for a session data request
        /// </summary>
        private static readonly string DefaultTestCaseName = string.Empty;

        /// <summary>
        /// Default value for flag indicating whether this is a child test case
        /// </summary>
        private const bool DefaultIsChildTestCase = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        /// <param name="requestedDuration">How much of the previously collected data the requestor is interested in.</param>
        internal DataRequestEventArgs(DataCollectionContext context, TimeSpan requestedDuration)
            : this(context, DefaultTestCaseId, DefaultTestCaseName, DefaultIsChildTestCase, requestedDuration)
        {
            Debug.Assert(
                    !context.HasTestCase,
                    "This constructor overload is to be used only for a session data request"
                );
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
        /// <param name="requestedDuration">How much of the previously collected data the requestor is interested in.</param>
        internal DataRequestEventArgs(
            DataCollectionContext context,
            Guid testCaseId,
            //TcmInformation tcmInformation,
            string testCaseName,
            bool isChildTestCase,
            TimeSpan requestedDuration)
            : base(context, testCaseId, testCaseName, isChildTestCase)
        {
            RequestId = new RequestId();
            RequestedDuration = requestedDuration;
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
        /// <param name="requestedDuration">How much of the previously collected data the requestor is interested in.</param>
        public DataRequestEventArgs(
            DataCollectionContext context,
            TestCase testElement,
            //TcmInformation tcmInformation,
            TimeSpan requestedDuration)
            : base(context, testElement)
        {
            // NOTE: ONLY USE FOR UNIT TESTING!
            //  This overload is only here for 3rd parties to use for unit testing
            //  their data collectors.  Internally we should not be passing the test element
            //  around in the events as this is extra information that needs to be seralized
            //  and the Execution Plugin Manager will fill this in for us before the event
            //  is sent to the data collector when running in a production environment.

            RequestId = new RequestId();
            RequestedDuration = requestedDuration;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the request ID that uniquely identifies this request context
        /// </summary>
        public RequestId RequestId
        {
            get;
            private set;
        }

        /// <summary>
        /// How much of the previously collected data the requestor is interested in.  A value
        /// of <see cref="Timespan.MaxValue"/> is used to request all data.
        /// </summary>
        /// <remarks>
        /// It is up to each individual data collector to respect this value when returning data.
        /// Some collectors may not be able to break up their data and will return the full
        /// set of data instead of just the requested portion.
        /// </remarks>
        public TimeSpan RequestedDuration
        {
            get;
            private set;
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for the event that tells the plugin manager to flush any remaining data
    /// collectors have sent into the result sink, and then append a token at the end, to
    /// indicate that all the data has been flushed into the result sink.
    /// </summary>
#if NET46
    [Serializable] 
#endif
    internal sealed class FlushDataEventArgs : DataCollectionEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the test case</param>
        internal FlushDataEventArgs(DataCollectionContext context)
            : base(context)
        {
        }

        #endregion
    }
}
