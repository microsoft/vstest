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
    using Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger;
    using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;
    using TrxObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;

    /// <summary>
    /// The converter class.
    /// </summary>
    internal class Converter
    {
        /// <summary>
        /// Property Id storing the TMITestId.
        /// </summary>
        private const string TmiTestIdPropertyIdentifier = "MSTestDiscoverer.TmiTestId";

        /// <summary>
        /// Converts the parameter rockSteady test case to test element
        /// </summary>
        /// <param name="rockSteadyTestResult">
        /// The rockSteady Test Result.
        /// </param>
        /// <returns>
        /// The <see cref="TrxObjectModel.UnitTestElement"/>.
        /// </returns>
        internal static TrxObjectModel.UnitTestElement ToUnitTestElement(ObjectModel.TestResult rockSteadyTestResult)
        {
            return GetQToolsTestElementFromTestCase(rockSteadyTestResult);
        }

        /// <summary>
        /// Returns QToolsCommon.TestElement from rockSteady TestCase.
        /// </summary>
        /// <param name="rockSteadyTestResult">
        /// The rockSteady Test Result.
        /// </param>
        /// <returns>
        /// The <see cref="UnitTestElement"/>.
        /// </returns>
        internal static TrxObjectModel.UnitTestElement GetQToolsTestElementFromTestCase(ObjectModel.TestResult rockSteadyTestResult)
        {
            ObjectModel.TestCase rockSteadyTestCase = rockSteadyTestResult.TestCase;

            // Fix for bug# 868033
            // Use TMI Test id when available. This is needed to ensure that test id in trx files is same as specified in 
            // .vsmdi files. 
            // (This is required for test explorer: It removes all test nodes where test id is not in expected test id when merging 
            // trx files from different batches).
            Guid testId = GetTmiTestId(rockSteadyTestCase);

#if NET451
            if (Guid.Empty.Equals(testId))
            {
                testId = rockSteadyTestCase.Id;
            }
#else
            testId = Guid.NewGuid();
#endif

            string testDisplayName = rockSteadyTestCase.DisplayName;

            // If it is an inner test case name
            if (!string.IsNullOrEmpty(rockSteadyTestResult.DisplayName))
            {
                testId = Guid.NewGuid(); // Changing of guid is done so that VS can load trx otherwise it fails with duplicate id error. 
                testDisplayName = rockSteadyTestResult.DisplayName;
            }

            TrxObjectModel.TestMethod testMethod = GetTestMethod(testDisplayName, rockSteadyTestCase);

            // convert the rocksteady tests to TestElement.
            TrxObjectModel.UnitTestElement testElement = new TrxObjectModel.UnitTestElement(testId, testDisplayName, rockSteadyTestCase.ExecutorUri.ToString(), testMethod);
            testElement.ExecutionId = new TrxObjectModel.TestExecId(Guid.NewGuid());
            testElement.AssignCodeBase(rockSteadyTestCase.Source);
            testElement.Storage = rockSteadyTestCase.Source;

            if (rockSteadyTestCase.Traits != null)
            {
                ObjectModel.Trait priorityTrait = rockSteadyTestCase.Traits.FirstOrDefault(t => t.Name.Equals("Priority"));
                if (priorityTrait != null)
                {
                    int priorityValue;
                    if (Int32.TryParse(priorityTrait.Value, out priorityValue))
                    {
                        testElement.Priority = priorityValue;
                    }
                }

                ObjectModel.Trait ownerTrait = rockSteadyTestCase.Traits.FirstOrDefault(t => t.Name.Equals("Owner"));
                if (ownerTrait != null)
                {
                    testElement.Owner = ownerTrait.Value;
                }
            }

            // reading TestCategories from the testcase
            var testCategories = GetCustomPropertyValueFromTestCase(rockSteadyTestCase, "MSTestDiscoverer.TestCategory");
            foreach (string testCategory in testCategories)
            {
                testElement.TestCategories.Add(testCategory);
            }

            return testElement;
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
        internal static TrxObjectModel.TestOutcome ToOutcome(ObjectModel.TestOutcome rockSteadyOutcome)
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

        /// <summary>
        /// Converts the rockSteady result to unit test result
        /// </summary>
        /// <param name="rockSteadyTestResult"> rock steady test result</param>
        /// <param name="testElement"> testElement of that test</param>
        /// <param name="testOutcome"> Test outcome </param>
        /// <param name="testRun"> test run object </param>
        /// <param name="trxFileDirectory"> TRX file directory</param>
        /// <returns> TestResult object </returns>
        internal static TrxObjectModel.UnitTestResult ToUnitTestResult(
            ObjectModel.TestResult rockSteadyTestResult,
            TrxObjectModel.UnitTestElement testElement,
            TrxObjectModel.TestOutcome testOutcome,
            TrxObjectModel.TestRun testRun,
            string trxFileDirectory)
        {
            TrxObjectModel.UnitTestResult qtoolsResult = GetQToolsTestResultFromTestResult(rockSteadyTestResult, testElement, testOutcome, testRun);

            // Clear exsting messages and store rocksteady result messages.
            qtoolsResult.TextMessages = null;
            UpdateResultMessages(qtoolsResult, rockSteadyTestResult);

            // Save result attachments to target location.
            UpdateTestResultAttachments(rockSteadyTestResult, qtoolsResult, testRun, trxFileDirectory, true);

            return qtoolsResult;
        }

        internal static List<CollectorDataEntry> ToCollectionEntries(IEnumerable<ObjectModel.AttachmentSet> attachmentSets, TestRun testRun, string trxFileDirectory)
        {
            List<CollectorDataEntry> collectorEntries = new List<CollectorDataEntry>();
            if (attachmentSets == null)
            {
                return collectorEntries;
            }

            foreach (var attachmentSet in attachmentSets)
            {
                if (attachmentSet.Uri.AbsoluteUri.StartsWith(TrxLogger.DataCollectorUriPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    CollectorDataEntry collectorEntry = ToCollectorEntry(attachmentSet, Guid.Empty, testRun, trxFileDirectory);
                    collectorEntries.Add(collectorEntry);
                }
            }

            return collectorEntries;
        }

        internal static IList<String> ToResultFiles(IEnumerable<ObjectModel.AttachmentSet> attachmentSets, TestRun testRun, string trxFileDirectory, List<string> errorMessages)
        {
            List<String> resultFiles = new List<string>();
            if (attachmentSets == null)
            {
                return resultFiles;
            }

            foreach (var attachmentSet in attachmentSets)
            {
                if (!attachmentSet.Uri.AbsoluteUri.StartsWith(TrxLogger.DataCollectorUriPrefix, StringComparison.OrdinalIgnoreCase))
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
                            e.Message);
                        errorMessages.Add(errorMsg);
                    }
                }
            }
            return resultFiles;
        }

        /// <summary>
        /// Returns the QToolsCommon.TestResult object created from rockSteady TestResult.
        /// </summary>
        /// <param name="rockSteadyTestResult"> rock steady test result</param>
        /// <param name="testElement"> testElement of that test</param>
        /// <param name="testOutcome"> Test outcome </param>
        /// <param name="testRun"> test run object </param>
        /// <returns> TestResult object </returns>
        private static TrxObjectModel.UnitTestResult GetQToolsTestResultFromTestResult(
            ObjectModel.TestResult rockSteadyTestResult,
            TrxObjectModel.UnitTestElement testElement,
            TrxObjectModel.TestOutcome testOutcome,
            TrxObjectModel.TestRun testRun)
        {
            UnitTestResult testResult = new UnitTestResult(Environment.MachineName, testRun.Id, testElement, testOutcome);
            if (rockSteadyTestResult.ErrorMessage != null)
            {
                testResult.ErrorMessage = rockSteadyTestResult.ErrorMessage;
            }

            if (rockSteadyTestResult.ErrorStackTrace != null)
            {
                testResult.ErrorStackTrace = rockSteadyTestResult.ErrorStackTrace;
            }

            // set start and end times
            if (rockSteadyTestResult.EndTime != null)
            {
                testResult.EndTime = rockSteadyTestResult.EndTime.UtcDateTime;
            }
            if (rockSteadyTestResult.StartTime != null)
            {
                testResult.StartTime = rockSteadyTestResult.StartTime.UtcDateTime;
            }

            if (rockSteadyTestResult.Duration != null)
            {
                testResult.Duration = rockSteadyTestResult.Duration;
            }

            return testResult;
        }

        /// <summary>
        /// Copies the result messages to unitTestResult
        /// </summary>
        /// <param name="unitTestResult">TRX TestResult</param>
        /// <param name="testResult"> rock steady test result</param>
        private static void UpdateResultMessages(TrxObjectModel.UnitTestResult unitTestResult, ObjectModel.TestResult testResult)
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
        internal static List<string> GetCustomPropertyValueFromTestCase(ObjectModel.TestCase testCase, string categoryID)
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
        /// Return TMI Test id when available for TestPlatform TestCase.
        /// </summary>
        /// <param name="rockSteadyTestCase">
        /// The rock Steady Test Case.
        /// </param>
        /// <returns>
        /// The <see cref="Guid"/>.
        /// </returns>
        private static Guid GetTmiTestId(ObjectModel.TestCase rockSteadyTestCase)
        {
            Guid tmiTestId = Guid.Empty;
            ObjectModel.TestProperty tmiTestIdProperty = rockSteadyTestCase.Properties.FirstOrDefault(property => property.Id.Equals(TmiTestIdPropertyIdentifier));
            if (null != tmiTestIdProperty)
            {
                tmiTestId = rockSteadyTestCase.GetPropertyValue(tmiTestIdProperty, Guid.Empty);
            }
            return tmiTestId;
        }

        /// <summary>
        /// Returns TestMethod for given testCase name and its class name.
        /// </summary>
        /// <param name="testDisplayName">test case display name</param>
        /// <param name="rockSteadyTestCase">rockSteady Test Case</param>
        /// <returns>The <see cref="TestMethod"/></returns>
        private static TrxObjectModel.TestMethod GetTestMethod(string testDisplayName, ObjectModel.TestCase rockSteadyTestCase)
        {
            string className = "DefaultClassName";
            string testCaseName = rockSteadyTestCase.FullyQualifiedName;
            if (testCaseName.Contains("."))
            {
                className = testCaseName.Substring(0, testCaseName.LastIndexOf('.'));
            }
            else if (testCaseName.Contains("::"))
            {
                // if this is a C++ test case then we would have a "::" instaed of a '.'
                className = testCaseName.Substring(0, testCaseName.LastIndexOf("::"));

                // rename for a consistent behaviour for all tests.
                className = className.Replace("::", ".");
            }

            return new TrxObjectModel.TestMethod(testDisplayName, className);
        }

        private static void UpdateTestResultAttachments(ObjectModel.TestResult rockSteadyTestResult, TrxObjectModel.UnitTestResult testResult, TestRun testRun, string trxFileDirectory, bool addAttachments)
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
                    // If the attachement is from data collector
                    if (attachmentSet.Uri.AbsoluteUri.StartsWith(TrxLogger.DataCollectorUriPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        CollectorDataEntry collectorEntry = ToCollectorEntry(attachmentSet, testResult.Id.ExecutionId.Id, testRun, trxFileDirectory);
                        collectorEntries.Add(collectorEntry);
                    }
                    else
                    {
                        IList<string> testResultFiles = ToResultFiles(attachmentSet, testResult.Id.ExecutionId.Id, testRun, trxFileDirectory);
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
                        e.Message);

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
        private static CollectorDataEntry ToCollectorEntry(ObjectModel.AttachmentSet attachmentSet, Guid testResultExecutionId, TestRun testRun, string trxFileDirectory)
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
                string targetFileName = FileHelper.GetNextIterationFileName(targetDirectory, Path.GetFileName(sourceFile), false);
                CopyFile(sourceFile, targetFileName);

                // Add the source file name to the collector files list. 
                // (Trx viewer automatically adds In\ to the collected file. 
                string fileName = Path.Combine(Environment.MachineName, Path.GetFileName(sourceFile));
                Uri sourceFileUri = new Uri(fileName, UriKind.Relative);
                UriDataAttachment dataAttachment = new UriDataAttachment(uriDataAttachment.Description, sourceFileUri);

                uriDataAttachments.Add(dataAttachment);
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
        private static IList<string> ToResultFiles(ObjectModel.AttachmentSet attachmentSet, Guid testResultExecutionId, TestRun testRun, string trxFileDirectory)
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
                if (ObjectModel.EqtTrace.IsVerboseEnabled)
                {
                    ObjectModel.EqtTrace.Verbose("TrxLogger: ToResultFiles: Got attachment " + uriDataAttachment.Uri + " with local path " + uriDataAttachment.Uri.LocalPath);
                }

                string sourceFile = uriDataAttachment.Uri.LocalPath;
                Debug.Assert(Path.IsPathRooted(sourceFile), "Source file is not rooted");

                // copy the source file to the target location
                string targetFileName = FileHelper.GetNextIterationFileName(testResultDirectory, Path.GetFileName(sourceFile), false);
                CopyFile(sourceFile, targetFileName);

                // Add the source file name to the result files list. 
                // (Trx viewer automatically adds In\<Guid> to the result file. 
                string fileName = Path.Combine(Environment.MachineName, Path.GetFileName(targetFileName));
                resultFiles.Add(fileName);
            }

            return resultFiles;
        }

        private static void CopyFile(string sourceFile, string targetFile)
        {
            try
            {
                File.Copy(sourceFile, targetFile, true);
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
    }
}
