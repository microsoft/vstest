// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;
    using TrxObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;

    /// <summary>
    /// The converter class.
    /// </summary>
    internal class Converter
    {
        private readonly TrxFileHelper trxFileHelper;
        private IFileHelper fileHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Converter"/> class.
        /// </summary>
        public Converter(IFileHelper fileHelper, TrxFileHelper trxFileHelper)
        {
            this.trxFileHelper = trxFileHelper;
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Converts platform test case to trx test element.
        /// </summary>
        /// <param name="testId"></param>
        /// <param name="executionId"></param>
        /// <param name="parentExecutionId"></param>
        /// <param name="name"></param>
        /// <param name="testType"></param>
        /// <param name="rockSteadyTestCase"></param>
        /// <returns>Trx test element</returns>
        public ITestElement ToTestElement(
            Guid testId,
            Guid executionId,
            Guid parentExecutionId,
            String testName,
            TestType testType,
            ObjectModel.TestCase rockSteadyTestCase)
        {
            var testElement = CreateTestElement(testId, testName, rockSteadyTestCase.FullyQualifiedName, rockSteadyTestCase.ExecutorUri.ToString(), rockSteadyTestCase.Source, testType);

            testElement.Storage = rockSteadyTestCase.Source;
            testElement.Priority = GetPriority(rockSteadyTestCase);
            testElement.Owner = GetOwner(rockSteadyTestCase);
            testElement.ExecutionId = new TestExecId(executionId);
            testElement.ParentExecutionId = new TestExecId(parentExecutionId);

            var testCategories = GetCustomPropertyValueFromTestCase(rockSteadyTestCase, "MSTestDiscoverer.TestCategory");
            foreach (string testCategory in testCategories)
            {
                testElement.TestCategories.Add(testCategory);
            }

            return testElement;
        }



        /// <summary>
        /// Converts the rockSteady result to unit test result
        /// </summary>
        /// <param name="testId"></param>
        /// <param name="executionId"></param>
        /// <param name="parentExecutionId"></param>
        /// <param name="testName"></param>
        /// <param name="trxFileDirectory"></param>
        /// <param name="testType"></param>
        /// <param name="testCategoryId"></param>
        /// <param name="testOutcome"></param>
        /// <param name="testRun"></param>
        /// <param name="rockSteadyTestResult"></param>
        /// <returns>Trx test result object</returns>
        public ITestResult ToTestResult(
            Guid testId,
            Guid executionId,
            Guid parentExecutionId,
            string testName,
            string trxFileDirectory,
            TestType testType,
            TestListCategoryId testCategoryId,
            TrxObjectModel.TestOutcome testOutcome,
            TestRun testRun,
            ObjectModel.TestResult rockSteadyTestResult)
        {
            var resultName = !string.IsNullOrEmpty(rockSteadyTestResult.DisplayName) ? rockSteadyTestResult.DisplayName : testName;
            var testResult = CreateTestResult(testRun.Id, testId, executionId, parentExecutionId, resultName, Environment.MachineName, testOutcome, testType, testCategoryId);

            if (rockSteadyTestResult.ErrorMessage != null)
                testResult.ErrorMessage = rockSteadyTestResult.ErrorMessage;

            if (rockSteadyTestResult.ErrorStackTrace != null)
                testResult.ErrorStackTrace = rockSteadyTestResult.ErrorStackTrace;

            if (rockSteadyTestResult.EndTime != null)
                testResult.EndTime = rockSteadyTestResult.EndTime.UtcDateTime;

            if (rockSteadyTestResult.StartTime != null)
                testResult.StartTime = rockSteadyTestResult.StartTime.UtcDateTime;

            if (rockSteadyTestResult.Duration != null)
                testResult.Duration = rockSteadyTestResult.Duration;

            // Clear existing messages and store rocksteady result messages.
            testResult.TextMessages = null;
            UpdateResultMessages(testResult, rockSteadyTestResult);

            // Save result attachments to target location.
            UpdateTestResultAttachments(rockSteadyTestResult, testResult, testRun, trxFileDirectory, true);

            return testResult;
        }

        /// <summary>
        /// converts ObjectModel.TestOutcome type to TrxLogger.TestOutcome type
        /// </summary>
        /// <param name="rockSteadyOutcome">
        /// The rockSteady Outcome.
        /// </param>
        /// <returns>
        /// The <see cref="TestOutcome"/>.
        /// </returns>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        public TrxObjectModel.TestOutcome ToOutcome(ObjectModel.TestOutcome rockSteadyOutcome)
        {
            TrxObjectModel.TestOutcome outcome = TrxObjectModel.TestOutcome.Failed;

            switch (rockSteadyOutcome)
            {
                case ObjectModel.TestOutcome.Failed:
                    outcome = TrxObjectModel.TestOutcome.Failed;
                    break;
                case ObjectModel.TestOutcome.Passed:
                    outcome = TrxObjectModel.TestOutcome.Passed;
                    break;
                case ObjectModel.TestOutcome.Skipped:
                case ObjectModel.TestOutcome.None:
                case ObjectModel.TestOutcome.NotFound:
                    outcome = TrxObjectModel.TestOutcome.NotExecuted;
                    break;
                default:
                    Debug.Fail("Unexpected Outcome.");
                    break;
            }

            return outcome;
        }

        public List<CollectorDataEntry> ToCollectionEntries(IEnumerable<ObjectModel.AttachmentSet> attachmentSets, TestRun testRun, string trxFileDirectory)
        {

            List<CollectorDataEntry> collectorEntries = new List<CollectorDataEntry>();
            if (attachmentSets == null)
            {
                EqtTrace.Info($"Converter.ToCollectionEntries: Received {nameof(attachmentSets)} as null returning empty collection entries.");
                return collectorEntries;
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"Converter.ToCollectionEntries: Converting attachmentSets {string.Join(",", attachmentSets)} to collection entries.");
            }

            foreach (var attachmentSet in attachmentSets)
            {
                if (attachmentSet.Uri.AbsoluteUri.StartsWith(Constants.DataCollectorUriPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    CollectorDataEntry collectorEntry = ToCollectorEntry(attachmentSet, Guid.Empty, testRun, trxFileDirectory);
                    collectorEntries.Add(collectorEntry);
                }
            }

            return collectorEntries;
        }


        public IList<String> ToResultFiles(IEnumerable<ObjectModel.AttachmentSet> attachmentSets, TestRun testRun, string trxFileDirectory, 
            List<string> errorMessages)
        {
            List<String> resultFiles = new List<string>();
            if (attachmentSets == null)
            {
                return resultFiles;
            }

            foreach (var attachmentSet in attachmentSets)
            {
                if (!attachmentSet.Uri.AbsoluteUri.StartsWith(Constants.DataCollectorUriPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        IList<String> testResultFiles = ToResultFiles(attachmentSet, Guid.Empty, testRun, trxFileDirectory);
                        resultFiles.AddRange(testResultFiles);
                    }
                    catch (Exception e)
                    {
                        string errorMsg = string.Format(
                            CultureInfo.CurrentCulture,
                            TrxLoggerResources.FailureToAttach,
                            attachmentSet.DisplayName, 
                            e.GetType().ToString(), 
                            e);

                        if (ObjectModel.EqtTrace.IsErrorEnabled)
                        {
                            ObjectModel.EqtTrace.Error("Converter: ToResultFiles: " + errorMsg);
                        }

                        errorMessages.Add(errorMsg);
                    }
                }
            }
            return resultFiles;
        }

        /// <summary>
        /// Copies the result messages to unitTestResult
        /// </summary>
        /// <param name="unitTestResult">TRX TestResult</param>
        /// <param name="testResult"> rock steady test result</param>
        private void UpdateResultMessages(TrxObjectModel.TestResult unitTestResult, ObjectModel.TestResult testResult)
        {
            StringBuilder debugTrace = new StringBuilder();
            StringBuilder stdErr = new StringBuilder();
            StringBuilder stdOut = new StringBuilder();

            foreach (Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResultMessage message in testResult.Messages)
            {
                if (ObjectModel.TestResultMessage.AdditionalInfoCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                {
                    unitTestResult.AddTextMessage(message.Text);
                }
                else if (ObjectModel.TestResultMessage.DebugTraceCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                {
                    debugTrace.AppendLine(message.Text);
                }
                else if (ObjectModel.TestResultMessage.StandardErrorCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                {
                    stdErr.AppendLine(message.Text);
                }
                else if (ObjectModel.TestResultMessage.StandardOutCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                {
                    stdOut.AppendLine(message.Text);
                }
                else
                {
                    ObjectModel.EqtTrace.Warning("The message category " + message.Category + " does not match any predefined category.");
                }
            }

            unitTestResult.DebugTrace = debugTrace.ToString();
            unitTestResult.StdErr = stdErr.ToString();
            unitTestResult.StdOut = stdOut.ToString();
        }

        /// <summary>
        ///  Get Custom property values from test cases.
        /// </summary>
        /// <param name="testCase">TestCase object extracted from the TestResult</param>
        /// <param name="categoryID">Property Name from the list of properties in TestCase</param>
        /// <returns> list of properties</returns>
        public List<string> GetCustomPropertyValueFromTestCase(ObjectModel.TestCase testCase, string categoryID)
        {
            var customProperty = testCase.Properties.FirstOrDefault(t => t.Id.Equals(categoryID));

            if (customProperty != null)
            {
                var cateogryValues = (string[])testCase.GetPropertyValue(customProperty);
                if (cateogryValues != null)
                {
                    return cateogryValues.ToList();
                }
                else
                {
                    return Enumerable.Empty<String>().ToList();
                }
            }

            return Enumerable.Empty<String>().ToList();
        }

        /// <summary>
        /// Gets test id.
        /// Return TMI Test id when available for TestPlatform test case.
        /// </summary>
        /// <param name="rockSteadyTestCase"></param>
        /// <returns>Test id</returns>
        public Guid GetTestId(ObjectModel.TestCase rockSteadyTestCase)
        {
            Guid testId = Guid.Empty;

            // Setting test id to tmi test id.
            ObjectModel.TestProperty tmiTestIdProperty = rockSteadyTestCase.Properties.FirstOrDefault(
                property => property.Id.Equals(Constants.TmiTestIdPropertyIdentifier));

            if (null != tmiTestIdProperty)
                testId = rockSteadyTestCase.GetPropertyValue(tmiTestIdProperty, Guid.Empty);

            // If tmi test id not present, picking from platform test id.
            if (Guid.Empty.Equals(testId))
                testId = rockSteadyTestCase.Id;

            return testId;
        }

        /// <summary>
        /// Gets parent execution id of test result.
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns>Parent execution id.</returns>
        public Guid GetParentExecutionId(ObjectModel.TestResult testResult)
        {
            TestProperty parentExecutionIdProperty = testResult.Properties.FirstOrDefault(
                property => property.Id.Equals(Constants.ParentExecutionIdPropertyIdentifier));

            return parentExecutionIdProperty == null ?
                Guid.Empty :
                testResult.GetPropertyValue(parentExecutionIdProperty, Guid.Empty);
        }

        /// <summary>
        /// Gets execution Id of test result. Creates new id if not present in test result properties.
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns>Execution id.</returns>
        public Guid GetExecutionId(ObjectModel.TestResult testResult)
        {
            TestProperty executionIdProperty = testResult.Properties.FirstOrDefault(
                property => property.Id.Equals(Constants.ExecutionIdPropertyIdentifier));

            var executionId = Guid.Empty;
            if (executionIdProperty != null)
                executionId = testResult.GetPropertyValue(executionIdProperty, Guid.Empty);

            return executionId.Equals(Guid.Empty) ? Guid.NewGuid() : executionId;
        }

        /// <summary>
        /// Gets test type of test result.
        /// Currently trx supports ordered test and unit test. All tests except ordered test are modified as unit test type.
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns>Test type</returns>
        public TestType GetTestType(ObjectModel.TestResult testResult)
        {
            var testTypeGuid = Constants.UnitTestTypeGuid;

            // Get test type from property. default to unit test type.
            TestProperty testTypeProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(Constants.TestTypePropertyIdentifier));
            testTypeGuid = (testTypeProperty == null) ? testTypeGuid : testResult.GetPropertyValue(testTypeProperty, testTypeGuid);

            // Currently trx supports ordered test and unit test. All tests except ordered test are modified as unit test type.
            return (testTypeGuid.Equals(Constants.OrderedTestTypeGuid)) ?
                Constants.OrderedTestType :
                Constants.UnitTestType;
        }

        /// <summary>
        /// Updates test result attachments.
        /// </summary>
        /// <param name="rockSteadyTestResult"></param>
        /// <param name="testResult"></param>
        /// <param name="testRun"></param>
        /// <param name="trxFileDirectory"></param>
        /// <param name="addAttachments"></param>
        private void UpdateTestResultAttachments(ObjectModel.TestResult rockSteadyTestResult, TrxObjectModel.TestResult testResult, TestRun testRun, string trxFileDirectory, bool addAttachments)
        {
            if (rockSteadyTestResult.Attachments == null || rockSteadyTestResult.Attachments.Count == 0)
            {
                return;
            }

            // the testResult needs to have the testRun property set. Otherwise Data Collector entries can't be added.
            testResult.SetTestRun(testRun);

            // result files
            List<string> resultFiles = new List<string>();

            // data collection files
            List<CollectorDataEntry> collectorEntries = new List<CollectorDataEntry>();

            foreach (ObjectModel.AttachmentSet attachmentSet in rockSteadyTestResult.Attachments)
            {
                try
                {
                    // If the attachment is from data collector
                    if (attachmentSet.Uri.AbsoluteUri.StartsWith(Constants.DataCollectorUriPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        CollectorDataEntry collectorEntry = ToCollectorEntry(attachmentSet, testResult.Id.ExecutionId, testRun, trxFileDirectory);
                        collectorEntries.Add(collectorEntry);
                    }
                    else
                    {
                        IList<string> testResultFiles = ToResultFiles(attachmentSet, testResult.Id.ExecutionId, testRun, trxFileDirectory);
                        resultFiles.AddRange(testResultFiles);
                    }
                }
                catch (Exception e)
                {
                    string errorMsg = string.Format(
                        CultureInfo.CurrentCulture,
                        TrxLoggerResources.FailureToAttach,
                        attachmentSet.DisplayName, 
                        e.GetType().ToString(), 
                        e);

                    if (ObjectModel.EqtTrace.IsErrorEnabled)
                    {
                        ObjectModel.EqtTrace.Error("Converter: UpdateTestResultAttachments: " + errorMsg);
                    }

                    StringBuilder stdErr = new StringBuilder(testResult.StdErr);
                    stdErr.AppendLine(errorMsg);

                    testResult.StdErr = stdErr.ToString();
                    testResult.Outcome = TrxObjectModel.TestOutcome.Error;
                }
            }

            if (addAttachments)
            {
                if (resultFiles.Count > 0)
                {
                    testResult.AddResultFiles(resultFiles);
                }

                if (collectorEntries.Count > 0)
                {
                    testResult.AddCollectorDataEntries(collectorEntries);
                }
            }
        }

        // Returns a list of collector entry
        private CollectorDataEntry ToCollectorEntry(ObjectModel.AttachmentSet attachmentSet, Guid testResultExecutionId, TestRun testRun, string trxFileDirectory)
        {
            string runDirectoryName = Path.Combine(trxFileDirectory, testRun.RunConfiguration.RunDeploymentRootDirectory);
            string inDirectory = Path.Combine(runDirectoryName, "In");

            string targetDirectory = inDirectory;
            if (!testResultExecutionId.Equals(Guid.Empty))
            {
                targetDirectory = Path.Combine(inDirectory, testResultExecutionId.ToString());
            }

            targetDirectory = Path.Combine(targetDirectory, Environment.MachineName);

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            List<IDataAttachment> uriDataAttachments = new List<IDataAttachment>();
            foreach (ObjectModel.UriDataAttachment uriDataAttachment in attachmentSet.Attachments)
            {
                if (ObjectModel.EqtTrace.IsVerboseEnabled)
                {
                    ObjectModel.EqtTrace.Verbose("TrxLogger: ToCollectorEntry: Got attachment " + uriDataAttachment.Uri + " with description " + uriDataAttachment.Description);
                }

                string sourceFile = uriDataAttachment.Uri.LocalPath;
                Debug.Assert(Path.IsPathRooted(sourceFile), "Source file is not rooted");

                // copy the source file to the target location
                string targetFileName = trxFileHelper.GetNextIterationFileName(targetDirectory, Path.GetFileName(sourceFile), false);

                try
                {
                    CopyFile(sourceFile, targetFileName);

                    // Add the target file name to the collector files list.
                    // (Trx viewer automatically adds In\ to the collected file.
                    string fileName = Path.Combine(Environment.MachineName, Path.GetFileName(targetFileName));
                    Uri sourceFileUri = new Uri(fileName, UriKind.Relative);
                    TrxObjectModel.UriDataAttachment dataAttachment = new TrxObjectModel.UriDataAttachment(uriDataAttachment.Description, sourceFileUri, trxFileHelper);

                    uriDataAttachments.Add(dataAttachment);
                }
                catch(Exception ex)
                {
                    if (ObjectModel.EqtTrace.IsErrorEnabled)
                    {
                        ObjectModel.EqtTrace.Error("Trxlogger: ToCollectorEntry: " + ex);
                    }
                }
            }

            return new CollectorDataEntry(
                attachmentSet.Uri,
                attachmentSet.DisplayName,
                Environment.MachineName,
                Environment.MachineName,
                false,
                uriDataAttachments);
        }

        // Get the path to the result files
        private IList<string> ToResultFiles(ObjectModel.AttachmentSet attachmentSet, Guid testResultExecutionId, TestRun testRun, string trxFileDirectory)
        {
            string runDirectoryName = Path.Combine(trxFileDirectory, testRun.RunConfiguration.RunDeploymentRootDirectory);
            string testResultDirectory = Path.Combine(runDirectoryName, "In");

            if (!Guid.Equals(testResultExecutionId, Guid.Empty))
            {
                testResultDirectory = Path.Combine(testResultDirectory, testResultExecutionId.ToString());
            }

            testResultDirectory = Path.Combine(testResultDirectory, Environment.MachineName);

            if (!Directory.Exists(testResultDirectory))
            {
                Directory.CreateDirectory(testResultDirectory);
            }

            List<string> resultFiles = new List<string>();
            foreach (ObjectModel.UriDataAttachment uriDataAttachment in attachmentSet.Attachments)
            {
                string sourceFile = uriDataAttachment.Uri.IsAbsoluteUri ? uriDataAttachment.Uri.LocalPath : uriDataAttachment.Uri.ToString();

                if (ObjectModel.EqtTrace.IsVerboseEnabled)
                {
                    ObjectModel.EqtTrace.Verbose("TrxLogger: ToResultFiles: Got attachment " + uriDataAttachment.Uri + " with local path " + sourceFile);
                }

                Debug.Assert(Path.IsPathRooted(sourceFile), "Source file is not rooted");
                // copy the source file to the target location
                string targetFileName = trxFileHelper.GetNextIterationFileName(testResultDirectory, Path.GetFileName(sourceFile), false);

                try
                {
                    CopyFile(sourceFile, targetFileName);

                    // Add the target file name to the result files list.
                    // (Trx viewer automatically adds In\<Guid> to the result file.
                    string fileName = Path.Combine(Environment.MachineName, Path.GetFileName(targetFileName));
                    resultFiles.Add(fileName);
                }
                catch(Exception ex)
                {
                    if (ObjectModel.EqtTrace.IsErrorEnabled)
                    {
                        ObjectModel.EqtTrace.Error("Trxlogger: ToResultFiles: " + ex);
                    }
                }
            }

            return resultFiles;
        }

        private void CopyFile(string sourceFile, string targetFile)
        {
            try
            {
                this.fileHelper.CopyFile(sourceFile, targetFile);
            }
            catch (Exception ex)
            {
                if (ObjectModel.EqtTrace.IsErrorEnabled)
                {
                    ObjectModel.EqtTrace.Error("Trxlogger: Failed to copy file {0} to {1}. Reason:{2}", sourceFile, targetFile, ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Gets priority of test.
        /// </summary>
        /// <param name="rockSteadyTestCase"></param>
        /// <returns>Priority</returns>
        private int GetPriority(ObjectModel.TestCase rockSteadyTestCase)
        {
            int priority = int.MaxValue;

            ObjectModel.Trait priorityTrait = rockSteadyTestCase.Traits?.FirstOrDefault(t => t.Name.Equals("Priority"));
            if (priorityTrait != null && Int32.TryParse(priorityTrait.Value, out int priorityValue))
                priority = priorityValue;

            return priority;
        }

        /// <summary>
        /// Gets owner of test.
        /// </summary>
        /// <param name="rockSteadyTestCase"></param>
        /// <returns>Owner</returns>
        private string GetOwner(ObjectModel.TestCase rockSteadyTestCase)
        {
            string owner = null;

            ObjectModel.Trait ownerTrait = rockSteadyTestCase.Traits?.FirstOrDefault(t => t.Name.Equals("Owner"));
            if (ownerTrait != null)
                owner = ownerTrait.Value;

            return owner ?? string.Empty;
        }

        /// <summary>
        /// Gets test class name.
        /// </summary>
        /// <param name="testName">Test name.</param>
        /// <param name="fullyQualifiedName">Fully qualified name.</param>
        /// <param name="source">Source.</param>
        /// <returns>Test class name.</returns>
        private string GetTestClassName(string testName, string fullyQualifiedName, string source)
        {
            var className = "DefaultClassName";

            // In case, fullyQualifiedName ends with testName, className is checked within remaining value of fullyQualifiedName.
            // Example: In case, testName = TestMethod1(2, 3, 4.0d) and fullyQualifiedName = TestProject1.Class1.TestMethod1(2, 3, 4.0d), className will be checked within 'TestProject1.Class1.' only
            var nameToCheck = !fullyQualifiedName.Equals(testName, StringComparison.OrdinalIgnoreCase) && fullyQualifiedName.EndsWith(testName) ?
                fullyQualifiedName.Substring(0, fullyQualifiedName.Length - testName.Length) :
                fullyQualifiedName;

            // C# test case scenario.
            if (nameToCheck.Contains("."))
            {
                return nameToCheck.Substring(0, nameToCheck.LastIndexOf('.'));
            }

            // C++ test case scenario (we would have a "::" instead of a '.')
            if (nameToCheck.Contains("::"))
            {
                className = nameToCheck.Substring(0, nameToCheck.LastIndexOf("::"));

                // rename for a consistent behavior for all tests.
                return className.Replace("::", ".");
            }

            // Ordered test, web test scenario (Setting class name as source name if FQDn doesn't have . or ::)
            try
            {
                string testCaseSource = Path.GetFileNameWithoutExtension(source);
                if (!String.IsNullOrEmpty(testCaseSource))
                {
                    return testCaseSource;
                }
            }
            catch (ArgumentException ex)
            {
                // If source is not valid file path, then className will continue to point default value.
                if (ObjectModel.EqtTrace.IsVerboseEnabled)
                {
                    ObjectModel.EqtTrace.Verbose("Converter: GetTestClassName: " + ex);
                }
            }

            return className;
        }

        /// <summary>
        /// Create test element.
        /// Currently trx supports only UnitTest and OrderedTest. All tests except OrderedTest all converted to UnitTest.
        /// </summary>
        /// <param name="testId"></param>
        /// <param name="name"></param>
        /// <param name="fullyQualifiedName"></param>
        /// <param name="adapter"></param>
        /// <param name="source"></param>
        /// <param name="testType"></param>
        /// <returns>Trx test element</returns>
        private TestElement CreateTestElement(Guid testId, string name, string fullyQualifiedName, string adapter, string source, TestType testType)
        {
            TestElement testElement = null;

            if (testType.Equals(Constants.OrderedTestType))
            {
                testElement = new OrderedTestElement(testId, name, adapter);
            }
            else
            {
                var codeBase = source;
                var className = GetTestClassName(name, fullyQualifiedName, source);
                var testMethodName = fullyQualifiedName.StartsWith($"{className}.") ? fullyQualifiedName.Remove(0, $"{className}.".Length) : fullyQualifiedName;
                var testMethod = new TestMethod(testMethodName, className);

                testElement = new UnitTestElement(testId, name, adapter, testMethod);
                (testElement as UnitTestElement).CodeBase = codeBase;
            }

            return testElement;
        }

        /// <summary>
        /// Create test result.
        /// Currently trx supports only UnitTest and OrderedTest. All tests except OrderedTest all converted to unit test result.
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="testId"></param>
        /// <param name="executionId"></param>
        /// <param name="parentExecutionId"></param>
        /// <param name="resultName"></param>
        /// <param name="computerName"></param>
        /// <param name="outcome"></param>
        /// <param name="testType"></param>
        /// <param name="testCategoryId"></param>
        /// <returns>Trx test result</returns>
        private TrxObjectModel.TestResult CreateTestResult(
            Guid runId,
            Guid testId,
            Guid executionId,
            Guid parentExecutionId,
            string resultName,
            string computerName,
            TrxObjectModel.TestOutcome outcome,
            TestType testType,
            TestListCategoryId testCategoryId)
        {
            return testType.Equals(Constants.OrderedTestType) ?
                new TestResultAggregation(runId, testId, executionId, parentExecutionId, resultName, Environment.MachineName, outcome, testType, testCategoryId, trxFileHelper) :
                new UnitTestResult(runId, testId, executionId, parentExecutionId, resultName, Environment.MachineName, outcome, testType, testCategoryId, trxFileHelper);
        }

    }
}
