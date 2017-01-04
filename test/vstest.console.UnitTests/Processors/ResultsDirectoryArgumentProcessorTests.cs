// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CommandLine.Processors;
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;

    [TestClass]
    public class ResultsDirectoryArgumentProcessorTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
            RunSettingsManager.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnResultsDirectoryArgumentProcessorCapabilities()
        {
            var processor = new ResultsDirectoryArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ResultsDirectoryArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnResultsDirectoryArgumentExecutor()
        {
            var processor = new ResultsDirectoryArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is ResultsDirectoryArgumentExecutor);
        }

        #region ResultsDirectoryArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new ResultsDirectoryArgumentProcessorCapabilities();
            Assert.AreEqual("/ResultsDirectory", capabilities.CommandName);
            //StringAssert.Contains(capabilities.HelpContentResourceName, "Test results directory will be created from a given path");

            Assert.AreEqual(HelpContentPriority.ResultsDirectoryArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region ResultsDirectoryArgumentExecutor Initialize tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNull()
        {
            var executor = new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);

            /*var message =
                @"The /ResultsDirectory parameter requires a value, where the test results should be saved. Example:  /ResultsDirectory:c:\MyTestResultsDirectory";*/

            var isExceptionThrown = false;

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                //Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsAWhiteSpace()
        {
            var executor = new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);

            /*var message =
                @"The /ResultsDirectory parameter requires a value, where the test results should be saved. Example:  /ResultsDirectory:c:\MyTestResultsDirectory";*/

            var isExceptionThrown = false;

            try
            {
                executor.Initialize("  ");
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                //Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldThrowIfRelativePathIsInvalid()
        {
            var executor = new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);

            var folder = @".\path\to\in:valid";

            /*var message = string.Format(
                @"The path '{0}' specified in the 'ResultsDirectory' is invalid. Error: {1}",
                folder,
                "Please provide valid path");*/

            var isExceptionThrown = false;

            try
            {
                executor.Initialize(folder);
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                //Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldThrowIfPathIsInvalid()
        {
            var executor = new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);

            var folder = @"c:\path\to\in:valid";

            /*var message = string.Format(
                @"The path '{0}' specified in the 'ResultsDirectory' is invalid. Error: {1}",
                folder,
                "Please provide valid path");*/

            var isExceptionThrown = false;

            try
            {
                executor.Initialize(folder);
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                //Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsAndRunSettingsFrameworkForRelativePathValue()
        {
            var executor = new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);
            var relativePath = @".\relative\path";
            var absolutePath = Path.GetFullPath(relativePath);
            executor.Initialize(relativePath);
            Assert.AreEqual(absolutePath, CommandLineOptions.Instance.ResultsDirectory);
            Assert.AreEqual(absolutePath, RunSettingsUtilities.QueryRunSettingsNode(RunSettingsManager.Instance, ResultsDirectoryArgumentExecutor.RunSettingsPath));
        }

        [TestMethod]
        public void InitializeShouldSetCommandLineOptionsAndRunSettingsFrameworkForAbsolutePathValue()
        {
            var executor = new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);
            var absolutePath = @"c:\Users\someone\testresults";
            executor.Initialize(absolutePath);
            Assert.AreEqual(absolutePath, CommandLineOptions.Instance.ResultsDirectory);
            Assert.AreEqual(absolutePath, RunSettingsUtilities.QueryRunSettingsNode(RunSettingsManager.Instance, ResultsDirectoryArgumentExecutor.RunSettingsPath));
        }

        #endregion

        #region ResultsDirectoryArgumentExecutor Execute tests

        [TestMethod]
        public void ExecuteShouldReturnSuccess()
        {
            var executor = new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        #endregion
    }
}
