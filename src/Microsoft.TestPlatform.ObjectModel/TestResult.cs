// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
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
        /// <remarks>This constructor doesn't perform any parameter validation, it is meant to be used for serialization."/></remarks>
        public TestResult()
        {
            // Default constructor for Serialization.
        }

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
        [IgnoreDataMember]
        public TestOutcome Outcome
        {
            get
            {
                return this.GetPropertyValue(TestResultProperties.Outcome, TestOutcome.None);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.Outcome, value);
            }
        }

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        [IgnoreDataMember]
        public string ErrorMessage
        {
            get
            {
                return this.GetPropertyValue<string>(TestResultProperties.ErrorMessage, null);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.ErrorMessage, value);
            }
        }

        /// <summary>
        /// Gets or sets the exception stack trace.
        /// </summary>
        [IgnoreDataMember]
        public string ErrorStackTrace
        {
            get
            {
                return this.GetPropertyValue<string>(TestResultProperties.ErrorStackTrace, null);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.ErrorStackTrace, value);
            }
        }

        /// <summary>
        /// Gets or sets the TestResult Display name. Used for Data Driven Test (i.e. Data Driven Test. E.g. InlineData in xUnit)
        /// </summary>
        [IgnoreDataMember]
        public string DisplayName
        {
            get
            {
                return this.GetPropertyValue<string>(TestResultProperties.DisplayName, null);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.DisplayName, value);
            }
        }

        /// <summary>
        /// Gets the test messages.
        /// </summary>
        [DataMember]
        public Collection<TestResultMessage> Messages
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets test result ComputerName.
        /// </summary>
        [IgnoreDataMember]
        public string ComputerName
        {
            get
            {
                return this.GetPropertyValue(TestResultProperties.ComputerName, string.Empty);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.ComputerName, value);
            }
        }

        /// <summary>
        /// Gets or sets the test result Duration.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan Duration
        {
            get
            {
                return this.GetPropertyValue(TestResultProperties.Duration, TimeSpan.Zero);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.Duration, value);
            }
        }

        /// <summary>
        /// Gets or sets the test result StartTime.
        /// </summary>
        [IgnoreDataMember]
        public DateTimeOffset StartTime
        {
            get
            {
                return this.GetPropertyValue(TestResultProperties.StartTime, DateTimeOffset.Now);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.StartTime, value);
            }
        }

        /// <summary>
        /// Gets or sets test result EndTime.
        /// </summary>
        [IgnoreDataMember]
        public DateTimeOffset EndTime
        {
            get
            {
                return this.GetPropertyValue(TestResultProperties.EndTime, DateTimeOffset.Now);
            }

            set
            {
                this.SetPropertyValue(TestResultProperties.EndTime, value);
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
        public static readonly TestProperty ComputerName = TestProperty.Register("TestResult.ComputerName", "Computer Name", string.Empty, string.Empty, typeof(string), ValidateComputerName, TestPropertyAttributes.None, typeof(TestResult));

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
        public static readonly TestProperty ComputerName = TestProperty.Register("TestResult.ComputerName", Resources.Resources.TestResultPropertyComputerNameLabel, string.Empty, string.Empty, typeof(string), ValidateComputerName, TestPropertyAttributes.None, typeof(TestResult));

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

        private static bool ValidateComputerName(object value)
        {
            return !string.IsNullOrWhiteSpace((string)value);
        }

        private static bool ValidateOutcome(object value)
        {
            return (TestOutcome)value <= TestOutcome.NotFound && (TestOutcome)value >= TestOutcome.None;
        }
    }
}
