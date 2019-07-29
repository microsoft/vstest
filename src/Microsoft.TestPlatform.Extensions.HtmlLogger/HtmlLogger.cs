// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using HtmlLoggerConstants = Microsoft.TestPlatform.Extensions.HtmlLogger.Utility.Constants;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System.Xml.Serialization;
    using System.Xml;
    using System.Runtime.Serialization;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger;
    using System.Collections.Concurrent;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System.Globalization;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;


    /// <summary>
    /// Logger for generating Html log
    /// </summary>
    [FriendlyName(HtmlLoggerConstants.FriendlyName)]
    [ExtensionUri(HtmlLoggerConstants.ExtensionUri)]
    public class Htmllogger : ITestLoggerWithParameters
    {
        // private string htmlFilePath;
        private Dictionary<string, string> parametersDictionary;
        private ConcurrentDictionary<Guid, TestResult> Results;
        //private TestOutcome testRunOutcome;
        internal int totalTests;
        internal int failTests;
        internal int passTests { get; set; }
        // private string htmlFilePath;
        private TestResults testResults;
        internal string htmlFileName;
        internal string xmlFileName;
        internal IFileHelper filehelper;

        DataContractSerializer xmlSerializer;
        IHtmlTransformer htmlTransformer;

        public Htmllogger()
            : this(new FileHelper(), new HtmlTransformer(), new DataContractSerializer(typeof(TestResults)))
            {
            }

        public Htmllogger(IFileHelper filehelper, IHtmlTransformer htmlTransformer, DataContractSerializer dataContractSerializer)
        {
            this.filehelper = filehelper;
            this.htmlTransformer = htmlTransformer;
            this.xmlSerializer = dataContractSerializer;
        }
    
        /// <summary>
        /// Gets the directory under which default trx file and test results attachements should be saved.
        /// </summary>
        public string TestResultsDirPath { get; private set; }
        public ConcurrentDictionary<Guid, TestResult> GetResults() { return this.Results; }

        internal TestResults GetTestResults()
        {
            return this.testResults;
        }

        internal void SetTestResults(TestResults testresults)
        {
            this.testResults = testresults;
        }
              
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
            events.TestRunMessage += this.TestMessageHandler;
            events.TestResult += this.TestResultHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;

            this.TestResultsDirPath = testResultsDirPath;
            this.testResults = new TestResults();
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
            this.Initialize(events, this.parametersDictionary[DefaultLoggerParameterNames.TestRunDirectory]);
        }

        internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunMessageEventArgs>(e, "e");
            
            // TrxLoggerObjectModel.RunInfo runMessage;
            switch (e.Level)
            {
                case TestMessageLevel.Informational:
                    testResults.RunLevelMessageInformational.Add(e.Message);
                    break;
                case TestMessageLevel.Warning:
                    testResults.RunLevelMessageErrorAndWarning.Add(e.Message);
                    break;
                case TestMessageLevel.Error:
                    testResults.RunLevelMessageErrorAndWarning.Add(e.Message);
                    break;
                default:
                    Debug.Fail("htmlLogger.TestMessageHandler: The test message level is unrecognized: {0}", e.Level.ToString());
                    break;
            }
        }

        internal void TestResultHandler(object sender, TestResultEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestResultEventArgs>(e, "e");

            Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.TestResult testResult = new Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.TestResult() ;
            if(e.Result.DisplayName!=null)
                testResult.DisplayName = e.Result.DisplayName;
            else
                testResult.DisplayName = e.Result.TestCase.DisplayName;

            testResult.FullyQualifiedName = e.Result.TestCase.FullyQualifiedName;
            //yet to test...
            testResult.Duration = e.Result.Duration;
            testResult.ErrorStackTrace = e.Result.ErrorStackTrace;
            testResult.ErrorMessage = e.Result.ErrorMessage;

            //yet to test..
            var executionId = Converter.GetExecutionId(e.Result);
            var parentExecutionId = Converter.GetParentExecutionId(e.Result);
            
            this.totalTests++;


            testResult.resultOutcome = e.Result.Outcome;
            if (e.Result.Outcome == TestOutcome.Failed)
            {
                this.failTests++;
            }
            else if (e.Result.Outcome == TestOutcome.Passed)
            {
                this.passTests++;
            }

            if(parentExecutionId==Guid.Empty)
                testResults.Results.Add(testResult);

            Results.TryAdd(executionId, testResult);
            if(parentExecutionId!= Guid.Empty)
            {
                this.AddToParentResult(executionId,parentExecutionId,testResult);
            }
        }

        internal void AddToParentResult(Guid executionId,Guid parentExecutionId ,TestResult testResult)
        {
            TestResult ParentTestResult;
            this.Results.TryGetValue(parentExecutionId, out ParentTestResult);
            if (ParentTestResult.innerTestResults == null)
                ParentTestResult.innerTestResults = new List<TestResult>();
            ParentTestResult.innerTestResults.Add(testResult);

        }


        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            testResults.Summary = new TestRunSummary
            {
                FailedTests = this.failTests,
                PassedTests = this.passTests,
                TotalTests = this.totalTests
            };
            //var list = new List<TestResult>();
            //list.Add(new TestResult(new TestCase("abc", new Uri("executor://dummy"), "abc.dll")));
            this.PopulateHtmlFile();
        }

        private void PopulateHtmlFile()
        {
            var xmlFilePath = GetXmlFilePath();
            Stream xmlStream = this.filehelper.GetStream(xmlFilePath, FileMode.Create);

            var htmlFilePath = GetHtmlFilePath();
            xmlSerializer.WriteObject(xmlStream, testResults);
            xmlStream.Close();

            htmlTransformer.Transform(xmlFilePath, htmlFilePath);
        }

        private string GetHtmlFilePath()
        {
            this.htmlFileName = GetFileName("html");
            var htmlFilePath = Path.Combine(this.TestResultsDirPath, htmlFileName);
            return htmlFilePath;
        }

        private string GetXmlFilePath()
        {
            this.xmlFileName = GetFileName("xml");
            var xmlFilePath = Path.Combine(this.TestResultsDirPath, xmlFileName);
            return xmlFilePath;
        }

        private string GetFileName(string fileFormat)
        {
            var fullfileformat = string.Concat("." +fileFormat);
            return string.Concat("TestResult_", DateTime.Now.ToLongDateString(), fullfileformat);
        }
    }
}