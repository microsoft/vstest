// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestPlatform.CommandLine.Processors;

    [TestClass]
    public class ListTestsTargetPathArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnListTestsTargetPathArgumentProcessorCapabilities()
        {
            ListTestsTargetPathArgumentProcessor processor = new ListTestsTargetPathArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is ListTestsTargetPathArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecutorShouldReturnListTestsTargetPathArgumentProcessorCapabilities()
        {
            ListTestsTargetPathArgumentProcessor processor = new ListTestsTargetPathArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is ListTestsTargetPathArgumentExecutor);
        }

        #region TestCaseFilterArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            ListTestsTargetPathArgumentProcessorCapabilities capabilities = new ListTestsTargetPathArgumentProcessorCapabilities();
            Assert.AreEqual("/ListTestsTargetPath", capabilities.CommandName);
            
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        [TestMethod]
        public void ExecutorInitializeWithNullOrEmptyListTestsTargetPathShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            ListTestsTargetPathArgumentExecutor executor = new ListTestsTargetPathArgumentExecutor(options);

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                StringAssert.Contains(ex.Message, "ListTestsTargetPath is required with ListFullyQualifiedTests!");
            }
        }

        [TestMethod]
        public void ExecutorInitializeWithValidListTestsTargetPathShouldAddListTestsTargetPathToCommandLineOptions()
        {
            var options = CommandLineOptions.Instance;
            ListTestsTargetPathArgumentExecutor executor = new ListTestsTargetPathArgumentExecutor(options);

            executor.Initialize(@"C:\sample.txt");
            Assert.AreEqual(@"C:\sample.txt", options.ListTestsTargetPath);
        }

        [TestMethod]
        public void ExecutorListTestsTargetPathArgumentProcessorResultSuccess()
        {
            var executor = new ListTestsTargetPathArgumentExecutor(CommandLineOptions.Instance);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }
}
