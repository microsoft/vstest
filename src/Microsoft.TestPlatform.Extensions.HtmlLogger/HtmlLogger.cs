// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;

    using ObjectModel;
    using Utilities;
    using Utilities.Helpers;
    using Utilities.Helpers.Interfaces;

    using HtmlResource = Resources.Resources;
    using HtmlLoggerConstants = Constants;

    /// <summary>
    /// Logger for generating Html.
    /// </summary>
    [FriendlyName(HtmlLoggerConstants.FriendlyName)]
    [ExtensionUri(HtmlLoggerConstants.ExtensionUri)]
    public class HtmlLogger : ITestLoggerWithParameters
    {
        private readonly IFileHelper fileHelper;
        private readonly XmlObjectSerializer xmlSerializer;
        private readonly IHtmlTransformer htmlTransformer;
        private Dictionary<string, string> parametersDictionary;

        public HtmlLogger()
            : this(new FileHelper(), new HtmlTransformer(), new DataContractSerializer(typeof(TestRunDetails)))
        {
        }

        public HtmlLogger(IFileHelper fileHelper, IHtmlTransformer htmlTransformer,
            XmlObjectSerializer dataContractSerializer)
        {
            this.fileHelper = fileHelper;
            this.htmlTransformer = htmlTransformer;
            this.xmlSerializer = dataContractSerializer;
        }

        /// <summary>
        /// Gets the directory under which default html file and test results attachments should be saved.
        /// </summary>
        public string TestResultsDirPath { get; private set; }

        /// <summary>
        /// Total results are stored in sequential order
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, ObjectModel.TestResult> Results { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public ConcurrentDictionary<string, TestResultCollection> ResultCollectionDictionary { get; private set; }

        /// <summary>
        /// Test results stores all the summary and the details of every results in hierarchical order.
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

        /// <summary>
        /// Path to the xml file.
        /// </summary>
        public string XmlFilePath { get; private set; }

        /// <summary>
        /// path to html file.
        /// </summary>
        public string HtmlFilePath { get; private set; }

        /// <inheritdoc/>
        public void Initialize(TestLoggerEvents events, string testResultsDirPath)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (string.IsNullOrEmpty(testResultsDirPath))
            {
                throw new ArgumentNullException(nameof(testResultsDirPath));
            }

            // Register for the events.
            events.TestRunMessage += TestMessageHandler;
            events.TestResult += TestResultHandler;
            events.TestRunComplete += TestRunCompleteHandler;

            TestResultsDirPath = testResultsDirPath;
            TestRunDetails = new TestRunDetails();
            Results = new ConcurrentDictionary<Guid, ObjectModel.TestResult>();
            ResultCollectionDictionary = new ConcurrentDictionary<string, TestResultCollection>();

            // Ensure test results directory exists.
            Directory.CreateDirectory(testResultsDirPath);
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

            parametersDictionary = parameters;

            if (parameters.TryGetValue(HtmlLoggerConstants.LogFilePrefixKey, out string logFilePrefixValue) && parameters.TryGetValue(HtmlLoggerConstants.LogFileNameKey, out string logFileNameValue))
            {
                var htmlParameterErrorMsg = string.Format(CultureInfo.CurrentCulture, HtmlResource.PrefixAndNameProvidedError);
                EqtTrace.Error(htmlParameterErrorMsg);
                throw new ArgumentException(htmlParameterErrorMsg);
            }

            this.Initialize(events, parameters[DefaultLoggerParameterNames.TestRunDirectory]);
        }

        /// <summary>
        /// Handles the message level information like warnings, errors etc..
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            ValidateArg.NotNull(sender, "sender");
            ValidateArg.NotNull(e, "e");

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
                case TestMessageLevel.Error:
                    if (TestRunDetails.RunLevelMessageErrorAndWarning == null)
                    {
                        TestRunDetails.RunLevelMessageErrorAndWarning = new List<string>();
                    }

                    TestRunDetails.RunLevelMessageErrorAndWarning.Add(e.Message);
                    break;
                default:
                    EqtTrace.Info("htmlLogger.TestMessageHandler: The test message level is unrecognized: {0}",
                        e.Level.ToString());
                    break;
            }
        }

        /// <summary>
        /// Handles the result coming from vs test and store it in test results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TestResultHandler(object sender, TestResultEventArgs e)
        {
            ValidateArg.NotNull(sender, "sender");
            ValidateArg.NotNull(e, "e");

            var testResult = new ObjectModel.TestResult
            {
                DisplayName = e.Result.DisplayName ?? e.Result.TestCase.FullyQualifiedName,
                FullyQualifiedName = e.Result.TestCase.FullyQualifiedName,
                ErrorStackTrace = e.Result.ErrorStackTrace,
                ErrorMessage = e.Result.ErrorMessage,
                TestResultId = e.Result.TestCase.Id,
                Duration = GetFormattedDurationString(e.Result.Duration),
                ResultOutcome = e.Result.Outcome
            };

            var executionId = GetExecutionId(e.Result);
            var parentExecutionId = GetParentExecutionId(e.Result);

            ResultCollectionDictionary.TryGetValue(e.Result.TestCase.Source, out var testResultCollection);
            if (testResultCollection == null)
            {
                testResultCollection = new TestResultCollection(e.Result.TestCase.Source)
                {
                    ResultList = new List<ObjectModel.TestResult>(),
                    FailedResultList = new List<ObjectModel.TestResult>(),
                };
                ResultCollectionDictionary.TryAdd(e.Result.TestCase.Source, testResultCollection);
                TestRunDetails.ResultCollectionList.Add(testResultCollection);
            }

            TotalTests++;
            switch (e.Result.Outcome)
            {
                case TestOutcome.Failed:
                    FailedTests++;
                    break;
                case TestOutcome.Passed:
                    PassedTests++;
                    break;
                case TestOutcome.Skipped:
                    SkippedTests++;
                    break;
                default:
                    break;
            }

            Results.TryAdd(executionId, testResult);

            // Check for parent execution id to store the test results in hierarchical way 
            if (parentExecutionId == Guid.Empty)
            {
                if (e.Result.Outcome == TestOutcome.Failed)
                {
                    testResultCollection.FailedResultList.Add(testResult);
                }

                testResultCollection.ResultList.Add(testResult);
            }
            else
            {
                AddToParentResult(parentExecutionId, testResult);
            }
        }

        private void AddToParentResult(Guid parentExecutionId, ObjectModel.TestResult testResult)
        {
            if (Results.TryGetValue(parentExecutionId, out var parentTestResult))
            {
                if (parentTestResult.InnerTestResults == null)
                    parentTestResult.InnerTestResults = new List<ObjectModel.TestResult>();

                parentTestResult.InnerTestResults.Add(testResult);
            }
        }

        /// <summary>
        /// Creates a summary of tests and populates the html file by transforming the xml file with help of xslt file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            TestRunDetails.Summary = new TestRunSummary
            {
                FailedTests = FailedTests,
                PassedTests = PassedTests,
                TotalTests = TotalTests,
                SkippedTests = SkippedTests,
                PassPercentage = (PassedTests * 100) / TotalTests,
                TotalRunTime = GetFormattedDurationString(e.ElapsedTimeInRunningTests),
            };
            if (this.parametersDictionary.TryGetValue(HtmlLoggerConstants.LogFilePrefixKey, out string logFilePrefixValue) && !string.IsNullOrWhiteSpace(logFilePrefixValue))
            {

                var framework = this.parametersDictionary[DefaultLoggerParameterNames.TargetFramework];
                if (framework != null)
                {
                    framework = Framework.GetShortFolderName(framework);
                    logFilePrefixValue = logFilePrefixValue + "_" + framework;
                }

                logFilePrefixValue = logFilePrefixValue + DateTime.Now.ToString("_yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo) + $".{HtmlLoggerConstants.HtmlFileExtension}";
                this.HtmlFilePath = Path.Combine(TestResultsDirPath, logFilePrefixValue);
            }
            else
            {
                if (parametersDictionary.TryGetValue(HtmlLoggerConstants.LogFileNameKey, out string logFileNameValue) && !string.IsNullOrWhiteSpace(logFileNameValue))
                {
                    this.HtmlFilePath = Path.Combine(TestResultsDirPath, logFileNameValue);
                }
            }

            PopulateHtmlFile();
        }

        private void PopulateHtmlFile()
        {
            try
            {
                var fileName = string.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}",
                    Environment.GetEnvironmentVariable("UserName"), Environment.MachineName,
                    FormatDateTimeForRunName(DateTime.Now));

                XmlFilePath = GetFilePath(HtmlLoggerConstants.XmlFileExtension, fileName);

                using (var xmlStream = fileHelper.GetStream(XmlFilePath, FileMode.Create))
                {
                    xmlSerializer.WriteObject(xmlStream, TestRunDetails);
                }

                if (string.IsNullOrEmpty(HtmlFilePath))
                {
                    HtmlFilePath = GetFilePath(HtmlLoggerConstants.HtmlFileExtension, fileName);
                }

                htmlTransformer.Transform(XmlFilePath, HtmlFilePath);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("HtmlLogger : Failed to populate html file. Exception : {0}",
                    ex.ToString());
                ConsoleOutput.Instance.Error(false, string.Concat(HtmlResource.HtmlLoggerError), ex.Message);
                return;
            }
            finally
            {
                if (XmlFilePath != null)
                {
                    this.fileHelper.Delete(XmlFilePath);
                }
            }

            var htmlFilePathMessage = string.Format(CultureInfo.CurrentCulture, HtmlResource.HtmlFilePath, HtmlFilePath);
            EqtTrace.Info(htmlFilePathMessage);
            ConsoleOutput.Instance.Information(false, htmlFilePathMessage);
        }

        private string GetFilePath(string fileExtension, string fileName)
        {
            var fullFileFormat = $".{fileExtension}";
            return Path.Combine(TestResultsDirPath, string.Concat("TestResult_", fileName, fullFileFormat));
        }

        private string FormatDateTimeForRunName(DateTime timeStamp)
        {
            return timeStamp.ToString("yyyyMMdd_HHmmss", DateTimeFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Gives the parent execution id of a TestResult.
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        private Guid GetParentExecutionId(TestPlatform.ObjectModel.TestResult testResult)
        {
            var parentExecutionIdProperty = testResult.Properties.FirstOrDefault(property =>
                property.Id.Equals(HtmlLoggerConstants.ParentExecutionIdPropertyIdentifier));
            return parentExecutionIdProperty == null
                ? Guid.Empty
                : testResult.GetPropertyValue(parentExecutionIdProperty, Guid.Empty);
        }

        /// <summary>
        /// Gives the execution id of a TestResult.
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        private Guid GetExecutionId(TestPlatform.ObjectModel.TestResult testResult)
        {
            var executionIdProperty = testResult.Properties.FirstOrDefault(property =>
                property.Id.Equals(HtmlLoggerConstants.ExecutionIdPropertyIdentifier));
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
        internal string GetFormattedDurationString(TimeSpan duration)
        {
            if (duration == default(TimeSpan))
            {
                return null;
            }

            var time = new List<string>();
            if (duration.Days > 0)
            {
                time.Add("> 1d");
            }
            else
            {
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
            }

            return time.Count == 0 ? "< 1ms" : string.Join(" ", time);
        }
    }
}