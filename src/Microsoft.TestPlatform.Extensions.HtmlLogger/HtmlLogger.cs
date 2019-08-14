// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Xml.Xsl;
    using HtmlResource = Resources.Resources;
    using HtmlLoggerConstants = Microsoft.TestPlatform.Extensions.HtmlLogger.Utility.Constants;

    /// <summary>
    /// Logger for generating Html.
    /// </summary>
    [FriendlyName(HtmlLoggerConstants.FriendlyName)]
    [ExtensionUri(HtmlLoggerConstants.ExtensionUri)]
    public class Htmllogger : ITestLoggerWithParameters
    {
        private IFileHelper filehelper;
        private XmlObjectSerializer xmlSerializer;
        private IHtmlTransformer htmlTransformer;
        private string fileName;
        private Dictionary<string, string> parametersDictionary;

        public Htmllogger()
        : this(new FileHelper(), new HtmlTransformer(), new DataContractSerializer(typeof(TestRunDetails)))
        {
        }

        public Htmllogger(IFileHelper filehelper, IHtmlTransformer htmlTransformer, XmlObjectSerializer dataContractSerializer)
        {
            this.filehelper = filehelper;
            this.htmlTransformer = htmlTransformer;
            this.xmlSerializer = dataContractSerializer;
        }

        /// <summary>
        /// Gets the directory under which default html file and test results attachements should be saved.
        /// </summary>
        public string TestResultsDirPath { get; private set; }

        /// <summary>
        /// Total results are stored in sequential order
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, TestResult> Results { get; private set; }

        /// <summary>
        /// Test results stores all the summary and the details of evrey results in hiearachial order.
        /// </summary>
        public TestRunDetails TestRunDetails { get; private set; }

        /// <summary>
        /// Total passed tests in the test results.
        /// </summary>
        public int PassedTests { get; private set; }

        /// <summary>
        /// Total failed tests in the test results.
        /// </summary>
        public int FailedTests { get; private set; }

        /// <summary>
        /// Total tests in the results.
        /// </summary>
        public int TotalTests { get; private set; }

        /// <summary>
        /// Total skipped tests in the results.
        /// </summary>
        public int SkippedTests { get; private set; }
        public string XmlFilePath { get; private set; }
        public string HtmlFilePath { get; private set; }

        public void Initialize(TestLoggerEvents events, string TestResultsDirPath)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (string.IsNullOrEmpty(TestResultsDirPath))
            {
                throw new ArgumentNullException(nameof(TestResultsDirPath));
            }

            // Register for the events.
            events.TestRunMessage += this.TestMessageHandler;
            events.TestResult += this.TestResultHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;

            this.TestResultsDirPath = TestResultsDirPath;
            this.TestRunDetails = new TestRunDetails();
            this.Results = new ConcurrentDictionary<Guid, TestResult>();
        }

        /// <inheritdoc/>
        public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.Count == 0)
            {
                throw new ArgumentException("No default parameters added", nameof(parameters));
            }
            this.parametersDictionary = parameters;
            this.Initialize(events, parameters[DefaultLoggerParameterNames.TestRunDirectory]);
        }

        /// <summary>
        /// Handles the message level information like warnings, errors etc..
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunMessageEventArgs>(e, "e");

            switch (e.Level)
            {
                case TestMessageLevel.Informational:
                    if (TestRunDetails.RunLevelMessageInformational == null)
                    {
                        TestRunDetails.RunLevelMessageInformational = new List<string>();
                    }
                    TestRunDetails.RunLevelMessageInformational.Add(e.Message);
                    break;
                case TestMessageLevel.Warning:
                    TestRunDetails.RunLevelMessageErrorAndWarning.Add(e.Message);
                    break;
                case TestMessageLevel.Error:
                    TestRunDetails.RunLevelMessageErrorAndWarning.Add(e.Message);
                    break;
                default:
                    EqtTrace.Info("htmlLogger.TestMessageHandler: The test message level is unrecognized: {0}", e.Level.ToString());
                    break;
            }
        }

        /// <summary>
        /// Handles the result coming from vstest and store it in test results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void TestResultHandler(object sender, TestResultEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestResultEventArgs>(e, "e");

            TestResult testResult = new TestResult();
            testResult.DisplayName = e.Result.DisplayName ?? e.Result.TestCase.DisplayName;

            testResult.FullyQualifiedName = e.Result.TestCase.FullyQualifiedName;
            testResult.Duration = GetFormattedDurationString(e.Result.Duration);
            testResult.ErrorStackTrace = e.Result.ErrorStackTrace;
            testResult.ErrorMessage = e.Result.ErrorMessage;

            var executionId = this.GetExecutionId(e.Result);
            var parentExecutionId = this.GetParentExecutionId(e.Result);

            this.TotalTests++;
            testResult.resultOutcome = e.Result.Outcome;
            if (e.Result.Outcome == TestOutcome.Failed)
            {
                this.FailedTests++;
            }
            else if (e.Result.Outcome == TestOutcome.Passed)
            {
                this.PassedTests++;
            }
            else if (e.Result.Outcome == TestOutcome.Skipped)
            {
                this.SkippedTests++;
            }

            if (parentExecutionId == Guid.Empty)
            {
                TestRunDetails.Results.Add(testResult);
            }

            Results.TryAdd(executionId, testResult);
            if (parentExecutionId != Guid.Empty)
            {
                this.AddToParentResult(parentExecutionId, testResult);
            }
        }

        private void AddToParentResult( Guid parentExecutionId, TestResult testResult)
        {
            TestResult parentTestResult;
            this.Results.TryGetValue(parentExecutionId, out parentTestResult);

            if (parentTestResult.innerTestResults == null)
                parentTestResult.innerTestResults = new List<TestResult>();

            parentTestResult.innerTestResults.Add(testResult);
        }

        /// <summary>
        /// Creates a summary of tests and popultes the html file by transforming the xml file with help of xslt file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            TestRunDetails.Summary = new TestRunSummary
            {
                FailedTests = this.FailedTests,
                PassedTests = this.PassedTests,
                TotalTests = this.TotalTests,
                SkippedTests = this.SkippedTests,
                PassPercentage = (PassedTests * 100)/ TotalTests ,
            };
            TestRunDetails.Summary.TotalRunTime = GetFormattedDurationString(e.ElapsedTimeInRunningTests); 

            if (this.parametersDictionary != null)
            {
                var isLogFileNameParameterExists = this.parametersDictionary.TryGetValue(HtmlLoggerConstants.LogFileNameKey, out string logFileNameValue);
                if (isLogFileNameParameterExists && !string.IsNullOrWhiteSpace(logFileNameValue))
                {
                    HtmlFilePath = Path.Combine(this.TestResultsDirPath, logFileNameValue);
                }
            }

            this.PopulateHtmlFile();
        }

        private void PopulateHtmlFile()
        {
            this.fileName = String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}", Environment.GetEnvironmentVariable("UserName"), Environment.MachineName, FormatDateTimeForRunName(DateTime.Now));
            XmlFilePath = this.GetFilePath(HtmlLoggerConstants.Xml,this.fileName);

            try
            {
                using (Stream xmlStream = this.filehelper.GetStream(XmlFilePath, FileMode.Create))
                {
                    xmlSerializer.WriteObject(xmlStream, TestRunDetails);
                }

                if (string.IsNullOrEmpty(HtmlFilePath))
                {
                    HtmlFilePath = this.GetFilePath(HtmlLoggerConstants.Html, this.fileName);
                }
                htmlTransformer.Transform(XmlFilePath, HtmlFilePath);
            }
            catch (IOException ioEx)
            {
                EqtTrace.Error(string.Format("HtmlLogger : Failed to create a xml file. Exception : {0}", ioEx.ToString()));
                return;
            }
            catch (XsltCompileException xslte)
            {
                EqtTrace.Error(string.Format("HtmlLogger : Failed to convert xml file to html file. Exception : {0}", xslte.ToString()));
                return;
            }

            string htmlfilePathMessage = string.Format(CultureInfo.CurrentCulture, HtmlResource.HtmlFilePath, HtmlFilePath);
            EqtTrace.Info(htmlfilePathMessage);
            ConsoleOutput.Instance.Information(false, htmlfilePathMessage);
        }

        private string GetFilePath(string fileFormat,string FileName)
        {  
            var fullfileformat = string.Concat("." + fileFormat);
            return Path.Combine(this.TestResultsDirPath, string.Concat("TestResult_", FileName, fullfileformat));
        }

        private static string FormatDateTimeForRunName(DateTime timeStamp)
        {
            return timeStamp.ToString("yyyyMMdd_HHmmss", DateTimeFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Gives the parent execution id of a TestResult.
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        private Guid GetParentExecutionId(ObjectModel.TestResult testResult)
        {
            TestProperty parentExecutionIdProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(HtmlLoggerConstants.ParentExecutionIdPropertyIdentifier));
            return parentExecutionIdProperty == null ? Guid.Empty : testResult.GetPropertyValue(parentExecutionIdProperty, Guid.Empty);
        }

        /// <summary>
        /// Gives the execution id of a TestResult.
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        private Guid GetExecutionId(ObjectModel.TestResult testResult)
        {
            TestProperty executionIdProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(HtmlLoggerConstants.ExecutionIdPropertyIdentifier));
            var executionId = Guid.Empty;

            if (executionIdProperty != null)
            {
                executionId = testResult.GetPropertyValue(executionIdProperty, Guid.Empty);
            }

            return executionId.Equals(Guid.Empty) ? Guid.NewGuid() : executionId;
        }

        /// <summary>
        /// Converts the time span format to readable string.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        private string GetFormattedDurationString(TimeSpan duration)
        {
            if (duration == default(TimeSpan))
            {
                return null;
            }

            var time = new List<string>();
            if (duration.Hours > 0)
            {
                time.Add(duration.Hours + "h");
            }

            if (duration.Minutes > 0)
            {
                time.Add(duration.Minutes + "m");
            }

            if (duration.Hours == 0)
            {
                if (duration.Seconds > 0)
                {
                    time.Add(duration.Seconds + "s");
                }

                if (duration.Milliseconds > 0 && duration.Minutes == 0)
                {
                    time.Add(duration.Milliseconds + "ms");
                }
            }

            return time.Count == 0 ? "< 1ms" : string.Join(" ", time);
        }
    }
}