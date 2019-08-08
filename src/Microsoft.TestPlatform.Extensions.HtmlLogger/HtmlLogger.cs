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
    /// Logger for generating Html log
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
        : this(new FileHelper(), new HtmlTransformer(), new DataContractSerializer(typeof(TestResults)))
        {
        }

        public Htmllogger(IFileHelper filehelper, IHtmlTransformer htmlTransformer, XmlObjectSerializer dataContractSerializer)
        {
            this.filehelper = filehelper;
            this.htmlTransformer = htmlTransformer;
            this.xmlSerializer = dataContractSerializer;
        }

        public string FilePath { get; private set; }
        /// <summary>
        /// Gets the directory under which default html file and test results attachements should be saved.
        /// </summary>
        public string TestResultsDirPath { get; private set; }

        /// <summary>
        /// Total Results are stored in sequential order
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, TestResult> Results { get; private set; }

        /// <summary>
        /// TestResults Stores all the Summary and the details of evrey Results in Hiearachial order.
        /// </summary>
        public TestResults TestResults { get; private set; }

        /// <summary>
        /// Total Passed Tests in the TestResults
        /// </summary>
        public int PassedTests { get; private set; }

        /// <summary>
        /// Total FailedTests in the Results
        /// </summary>
        public int FailedTests { get; private set; }

        /// <summary>
        /// Total Tests in the Results
        /// </summary>
        public int TotalTests { get; private set; }

        /// <summary>
        /// Total SkippedTests in the Results
        /// </summary>
        public int SkippedTests { get; private set; }
        public string xmlFilePath { get; private set; }
        public string htmlFilePath { get; private set; }

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
            this.TestResults = new TestResults();
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
        /// Handles the Message level information like warnings errors etc..
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
                    TestResults.RunLevelMessageInformational.Add(e.Message);
                    break;
                case TestMessageLevel.Warning:
                    TestResults.RunLevelMessageErrorAndWarning.Add(e.Message);
                    break;
                case TestMessageLevel.Error:
                    TestResults.RunLevelMessageErrorAndWarning.Add(e.Message);
                    break;
                default:
                    Debug.Fail("htmlLogger.TestMessageHandler: The test message level is unrecognized: {0}", e.Level.ToString());
                    break;
            }
        }

        /// <summary>
        /// Handles the Result coming from vstest and store it in testresults 
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
                TestResults.Results.Add(testResult);
            }

            Results.TryAdd(executionId, testResult);
            if (parentExecutionId != Guid.Empty)
            {
                this.AddToParentResult(parentExecutionId, testResult);
            }
        }

        private void AddToParentResult( Guid parentExecutionId, TestResult testResult)
        {
            // Needs to be tested!
            TestResult ParentTestResult;
            this.Results.TryGetValue(parentExecutionId, out ParentTestResult);

            if (ParentTestResult.innerTestResults == null)
                ParentTestResult.innerTestResults = new List<TestResult>();

            ParentTestResult.innerTestResults.Add(testResult);
        }

        /// <summary>
        /// Creates a Summary of tests and Popultes the html file by transforming the xml file with help of xslt file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            TestResults.Summary = new TestRunSummary
            {
                FailedTests = this.FailedTests,
                PassedTests = this.PassedTests,
                TotalTests = this.TotalTests
            };

            if (this.parametersDictionary != null)
            {
                var isLogFileNameParameterExists = this.parametersDictionary.TryGetValue(HtmlLoggerConstants.LogFileNameKey, out string logFileNameValue);
                if (isLogFileNameParameterExists && !string.IsNullOrWhiteSpace(logFileNameValue))
                {
                    this.htmlFilePath = Path.Combine(this.TestResultsDirPath, logFileNameValue);
                }
            }

            this.PopulateHtmlFile();
        }

        private void PopulateHtmlFile()
        {
            fileName = String.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}", Environment.GetEnvironmentVariable("UserName"), Environment.MachineName, FormatDateTimeForRunName(DateTime.Now));
            xmlFilePath = this.GetFilePath("xml",this.fileName);
            Stream xmlStream = null;

            try
            {
                xmlStream = this.filehelper.GetStream(xmlFilePath, FileMode.Create);
                xmlSerializer.WriteObject(xmlStream, TestResults);
                xmlStream.Close();

                if (htmlFilePath == null)
                {
                    htmlFilePath = this.GetFilePath("html", this.fileName);
                }
                htmlTransformer.Transform(xmlFilePath, htmlFilePath);
            }
            catch (IOException ioEx)
            {
                string ioexMessage = string.Format("HtmlLogger : Failed to create a xml file. Exception : {0}", ioEx.ToString());
                EqtTrace.Error(ioexMessage);
            }
            catch (XsltCompileException xslte)
            {
                string xslteMessage = string.Format("HtmlLogger : Failed to convert xml file to html file. Exception : {0}", xslte.ToString());
                EqtTrace.Error(xslteMessage);
            }

            string htmlfilePathMessage = string.Format(CultureInfo.CurrentCulture, HtmlResource.HtmlFilePath, this.htmlFilePath);
            EqtTrace.Info(htmlfilePathMessage);
            ConsoleOutput.Instance.Information(false, this.htmlFilePath);
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
        /// Gives the parent execution id of a TestResult
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        private Guid GetParentExecutionId(ObjectModel.TestResult testResult)
        {
            TestProperty parentExecutionIdProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(HtmlLoggerConstants.ParentExecutionIdPropertyIdentifier));
            return parentExecutionIdProperty == null ? Guid.Empty : testResult.GetPropertyValue(parentExecutionIdProperty, Guid.Empty);
        }

        /// <summary>
        /// Gives the execution id of a TestResult
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
        /// Converts the timespan format to readable string 
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