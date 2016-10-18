// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Logging
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using ObjectModelCommonResources = Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources.CommonResources;
    
    /// <summary>
    /// The test session message logger.
    /// </summary>
    internal class TestSessionMessageLogger : IMessageLogger
    {
        private static TestSessionMessageLogger instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSessionMessageLogger"/> class.
        /// </summary>
        protected TestSessionMessageLogger()
        {
            this.TreatTestAdapterErrorsAsWarnings = Constants.DefaultTreatTestAdapterErrorsAsWarnings;
        }

        /// <summary>
        /// Raised when a discovery message is received.
        /// </summary>
        internal event EventHandler<TestRunMessageEventArgs> TestRunMessage;

        /// <summary>
        /// Gets the instance of the singleton.
        /// </summary>
        internal static TestSessionMessageLogger Instance
        {
            get
            {
                return instance ?? (instance = new TestSessionMessageLogger());
            }
            set
            {
                instance = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to treat test adapter errors as warnings.
        /// </summary>
        internal bool TreatTestAdapterErrorsAsWarnings
        {
            get;
            set;
        }

        /// <summary>
        /// Sends a message to all listeners.
        /// </summary>
        /// <param name="testMessageLevel">Level of the message.</param>
        /// <param name="message">The message to be sent.</param>
        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(ObjectModelCommonResources.CannotBeNullOrEmpty, "message");
            }

            if (this.TreatTestAdapterErrorsAsWarnings
                    && testMessageLevel == TestMessageLevel.Error)
            {
                // Downgrade the message severity to Warning...
                testMessageLevel = TestMessageLevel.Warning;
            }

            if (this.TestRunMessage != null)
            {
                var args = new TestRunMessageEventArgs(testMessageLevel, message);
                this.TestRunMessage.SafeInvoke(this, args, "TestRunMessageLoggerProxy.SendMessage");
            }
        }

    }
}
