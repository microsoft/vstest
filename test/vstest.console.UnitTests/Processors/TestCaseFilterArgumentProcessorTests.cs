// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestPlatform.CommandLine.Processors;

    [TestClass]
    public class TestCaseFilterArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnTestCaseFilterArgumentProcessorCapabilities()
        {
            TestCaseFilterArgumentProcessor processor = new TestCaseFilterArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is TestCaseFilterArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecutorShouldReturnTestCaseFilterArgumentProcessorCapabilities()
        {
            TestCaseFilterArgumentProcessor processor = new TestCaseFilterArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is TestCaseFilterArgumentExecutor);
        }

        #region TestCaseFilterArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            TestCaseFilterArgumentProcessorCapabilities capabilities = new TestCaseFilterArgumentProcessorCapabilities();
            Assert.AreEqual("/TestCaseFilter", capabilities.CommandName);
            StringAssert.Contains(capabilities.HelpContentResourceName, ("/TestCaseFilter:<Expression>" + Environment.NewLine + "      Run tests that match the given expression." + Environment.NewLine + "      <Expression> is of the format <property>Operator<value>[|&<Expression>]").Replace("\r", string.Empty));

            Assert.AreEqual(HelpContentPriority.TestCaseFilterArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        [TestMethod]
        public void ExecutorInitializeWithNullOrEmptyTestCaseFilterShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            TestCaseFilterArgumentExecutor executor = new TestCaseFilterArgumentExecutor(options);

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                StringAssert.Contains(ex.Message, @"The /TestCaseFilter argument requires the filter value.");
            }
        }

        [TestMethod]
        public void ExecutorInitializeWithInvalidTestCaseFilterShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            TestCaseFilterArgumentExecutor executor = new TestCaseFilterArgumentExecutor(options);

            try
            {
                executor.Initialize("Foo");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                StringAssert.Contains(ex.Message, @"The /TestCaseFilter argument requires the filter value.");
            }
        }

        [TestMethod]
        public void ExecutorInitializeWithValidTestCaseFilterShouldAddTestCaseFilterToCommandLineOptions()
        {
            var options = CommandLineOptions.Instance;
            TestCaseFilterArgumentExecutor executor = new TestCaseFilterArgumentExecutor(options);

            executor.Initialize("Debug");
            Assert.AreEqual("Debug", options.TestCaseFilterValue);
        }

        [TestMethod]
        public void ExecutorExecutoreturnArgumentProcessorResultSuccess()
        {
            var executor = new TestCaseFilterArgumentExecutor(CommandLineOptions.Instance);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }
}
