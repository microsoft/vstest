// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// Represents the result of a test case.
    /// </summary>
    [DataContract]
    public sealed class TestResult : TestObject
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResult"/> class.
        /// </summary>
        /// <param name="testCase">The test case the result is for.</param>
        public TestResult(TestCase testCase)
        {
            if (testCase == null)
            {
                throw new ArgumentNullException(nameof(testCase));
            }

            this.TestCase = testCase;
            this.Messages = new Collection<TestResultMessage>();
            this.Attachments = new Collection<AttachmentSet>();

            // Default start and end time values for a test result are initialized to current timestamp
            // to maintain compatibility.
            this.StartTime = DateTimeOffset.UtcNow;
            this.EndTime = DateTimeOffset.UtcNow;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the test case that this result is for.
        /// </summary>
        [DataMember]
        public TestCase TestCase { get; private set; }

        /// <summary>
        /// Gets the list of attachment sets for this TestResult.
        /// </summary>
        [DataMember]
        public Collection<AttachmentSet> Attachments { get; private set; }

        /// <summary>
        /// Gets or sets the outcome of a test case.
        /// </summary>
        [DataMember]
        public TestOutcome Outcome { get; set; }

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception stack trace.
        /// </summary>
        [DataMember]
        public string ErrorStackTrace { get; set; }

        /// <summary>
        /// Gets or sets the TestResult Display name. Used for Data Driven Test (i.e. Data Driven Test. E.g. InlineData in xUnit)
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets the test messages.
        /// </summary>
        [DataMember]
        public Collection<TestResultMessage> Messages { get; private set; }

        /// <summary>
        /// Gets or sets test result ComputerName.
        /// </summary>
        [DataMember]
        public string ComputerName { get; set; }

        /// <summary>
        /// Gets or sets the test result Duration.
        /// </summary>
        [DataMember]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the test result StartTime.
        /// </summary>
        [DataMember]
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Gets or sets test result EndTime.
        /// </summary>
        [DataMember]
        public DateTimeOffset EndTime { get; set; }

        /// <summary>
        /// Returns the TestProperties currently specified in this TestObject.
        /// </summary>
        public override IEnumerable<TestProperty> Properties
        {
            get
            {
                return TestResultProperties.Properties.Concat(base.Properties);
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            // Add the outcome of the test and the name of the test.
            result.AppendFormat(
                CultureInfo.CurrentUICulture,
                Resources.Resources.BasicTestResultFormat,
                this.TestCase.DisplayName,
                TestOutcomeHelper.GetOutcomeString(this.Outcome));

            // Add the error message and stack trace if this is a test failure.
            if (this.Outcome == TestOutcome.Failed)
            {
                // Add Error message.
                result.AppendLine();
                result.AppendFormat(CultureInfo.CurrentUICulture, Resources.Resources.TestFailureMessageFormat, this.ErrorMessage);

                // Add stack trace if we have one.
                if (!string.IsNullOrWhiteSpace(this.ErrorStackTrace))
                {
                    result.AppendLine();
                    result.AppendFormat(
                        CultureInfo.CurrentUICulture,
                        Resources.Resources.TestFailureStackTraceFormat,
                        this.ErrorStackTrace);
                }
            }

            // Add any text messages we have.
            if (this.Messages.Count > 0)
            {
                StringBuilder testMessages = new StringBuilder();
                foreach (TestResultMessage message in this.Messages)
                {
                    if (!string.IsNullOrEmpty(message?.Category) && !string.IsNullOrEmpty(message.Text))
                    {
                        testMessages.AppendFormat(
                            CultureInfo.CurrentUICulture,
                            Resources.Resources.TestResultMessageFormat,
                            message.Category,
                            message.Text);
                    }
                }

                result.AppendLine();
                result.AppendFormat(
                    CultureInfo.CurrentUICulture,
                    Resources.Resources.TestResultTextMessagesFormat,
                    testMessages.ToString());
            }

            return result.ToString();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Return TestProperty's value
        /// </summary>
        /// <returns></returns>
        protected override object ProtectedGetPropertyValue(TestProperty property, object defaultValue)
        {
            ValidateArg.NotNull(property, "property");

            switch (property.Id)
            {
                case "TestResult.ComputerName":
                    return this.ComputerName;
                case "TestResult.DisplayName":
                    return this.DisplayName;
                case "TestResult.Duration":
                    return this.Duration;
                case "TestResult.EndTime":
                    return this.EndTime;
                case "TestResult.ErrorMessage":
                    return this.ErrorMessage;
                case "TestResult.ErrorStackTrace":
                    return this.ErrorStackTrace;
                case "TestResult.Outcome":
                    return this.Outcome;
                case "TestResult.StartTime":
                    return this.StartTime;
            }

            return base.ProtectedGetPropertyValue(property, defaultValue);
        }

        /// <summary>
        /// Set TestProperty's value
        /// </summary>
        protected override void ProtectedSetPropertyValue(TestProperty property, object value)
        {
            ValidateArg.NotNull(property, "property");

            switch (property.Id)
            {
                case "TestResult.ComputerName":
                    this.ComputerName = (string)value; return;
                case "TestResult.DisplayName":
                    this.DisplayName = (string)value; return;
                case "TestResult.Duration":
                    this.Duration = (TimeSpan)value; return;
                case "TestResult.EndTime":
                    this.EndTime = (DateTimeOffset)value; return;
                case "TestResult.ErrorMessage":
                    this.ErrorMessage = (string)value; return;
                case "TestResult.ErrorStackTrace":
                    this.ErrorStackTrace = (string)value; return;
                case "TestResult.Outcome":
                    this.Outcome = (TestOutcome)value; return;
                case "TestResult.StartTime":
                    this.StartTime = (DateTimeOffset)value; return;
            }
            base.ProtectedSetPropertyValue(property, value);
        }

        #endregion
    }

    /// <summary>
    /// Represents the test result message.
    /// </summary>
    [DataContract]
    public class TestResultMessage
    {
        // Bugfix: 297759 Moving the category from the resources to the code
        // so that it works on machines which has eng OS & non-eng VS and vice versa.

        /// <summary>
        ///     Standard Output Message Category
        /// </summary>
        public static readonly string StandardOutCategory = "StdOutMsgs";

        /// <summary>
        ///     Standard Error Message Category
        /// </summary>
        public static readonly string StandardErrorCategory = "StdErrMsgs";

        /// <summary>
        ///     Debug Trace Message Category
        /// </summary>
        public static readonly string DebugTraceCategory = "DbgTrcMsgs";

        /// <summary>
        ///     Additional Information Message Category
        /// </summary>
        public static readonly string AdditionalInfoCategory = "AdtnlInfo";

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResultMessage"/> class.
        /// </summary>
        /// <param name="category">Category of the message.</param>
        /// <param name="text">Text of the message.</param>
        public TestResultMessage(string category, string text)
        {
            this.Category = category;
            this.Text = text;
        }

        /// <summary>
        /// Gets the message category
        /// </summary>
        [DataMember]
        public string Category
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the message text
        /// </summary>
        [DataMember]
        public string Text
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Well-known TestResult properties
    /// </summary>
    public static class TestResultProperties
    {
#if !FullCLR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty DisplayName = TestProperty.Register("TestResult.DisplayName", "TestResult Display Name", typeof(string), TestPropertyAttributes.Hidden, typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ComputerName = TestProperty.Register("TestResult.ComputerName", "Computer Name", typeof(string), TestPropertyAttributes.None, typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty Outcome = TestProperty.Register("TestResult.Outcome", "Outcome", string.Empty, string.Empty, typeof(TestOutcome), ValidateOutcome, TestPropertyAttributes.None, typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty Duration = TestProperty.Register("TestResult.Duration", "Duration", typeof(TimeSpan), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty StartTime = TestProperty.Register("TestResult.StartTime", "Start Time", typeof(DateTimeOffset), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty EndTime = TestProperty.Register("TestResult.EndTime", "End Time", typeof(DateTimeOffset), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ErrorMessage = TestProperty.Register("TestResult.ErrorMessage", "Error Message", typeof(string), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ErrorStackTrace = TestProperty.Register("TestResult.ErrorStackTrace", "Error Stack Trace", typeof(string), typeof(TestResult));
#else
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty DisplayName = TestProperty.Register("TestResult.DisplayName", Resources.Resources.TestResultPropertyDisplayNameLabel, typeof(string), TestPropertyAttributes.Hidden, typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ComputerName = TestProperty.Register("TestResult.ComputerName", Resources.Resources.TestResultPropertyComputerNameLabel, typeof(string), TestPropertyAttributes.None, typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty Outcome = TestProperty.Register("TestResult.Outcome", Resources.Resources.TestResultPropertyOutcomeLabel, string.Empty, string.Empty, typeof(TestOutcome), ValidateOutcome, TestPropertyAttributes.None, typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty Duration = TestProperty.Register("TestResult.Duration", Resources.Resources.TestResultPropertyDurationLabel, typeof(TimeSpan), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty StartTime = TestProperty.Register("TestResult.StartTime", Resources.Resources.TestResultPropertyStartTimeLabel, typeof(DateTimeOffset), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty EndTime = TestProperty.Register("TestResult.EndTime", Resources.Resources.TestResultPropertyEndTimeLabel, typeof(DateTimeOffset), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ErrorMessage = TestProperty.Register("TestResult.ErrorMessage", Resources.Resources.TestResultPropertyErrorMessageLabel, typeof(string), typeof(TestResult));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TestProperty ErrorStackTrace = TestProperty.Register("TestResult.ErrorStackTrace", Resources.Resources.TestResultPropertyErrorStackTraceLabel, typeof(string), typeof(TestResult));
#endif
        internal static TestProperty[] Properties { get; } =
        {
            ComputerName,
            DisplayName,
            Duration,
            EndTime,
            ErrorMessage,
            ErrorStackTrace,
            Outcome,
            StartTime
        };

        private static bool ValidateOutcome(object value)
        {
            return (TestOutcome)value <= TestOutcome.NotFound && (TestOutcome)value >= TestOutcome.None;
        }
    }
}
