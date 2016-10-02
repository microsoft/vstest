// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    
    // <summary>
    //   Tests for TestSourceArgumentProcessor
    // </summary>
    [TestClass]
    public class TestSourceArgumentProcessorTests
    {
        /// <summary>
        /// The help argument processor get metadata should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetMetadataShouldReturnTestSourceArgumentProcessorCapabilities()
        {
            TestSourceArgumentProcessor processor = new TestSourceArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is TestSourceArgumentProcessorCapabilities);
        }

        /// <summary>
        /// The help argument processor get executer should return help argument processor capabilities.
        /// </summary>
        [TestMethod]
        public void GetExecuterShouldReturnTestSourceArgumentProcessorCapabilities()
        {
            TestSourceArgumentProcessor processor = new TestSourceArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is TestSourceArgumentExecutor);
        }

        #region TestSourceArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            TestSourceArgumentProcessorCapabilities capabilities = new TestSourceArgumentProcessorCapabilities();
            Assert.AreEqual("TestSource", capabilities.CommandName);
            Assert.IsNull(capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.None, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(true, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(true, capabilities.IsSpecialCommand);
        }
        
        #endregion

        #region TestSourceArgumentExecutorTests

        [TestMethod]
        public void ExecuterInitializeWithInvalidSourceShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            TestSourceArgumentExecutor executor = new TestSourceArgumentExecutor(options);
            
            // This path is invalid
            string testFilePath = "TestFile.txt";

            try
            {
                executor.Initialize(testFilePath);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual("The test source file \"" + testFilePath + "\" provided was not found.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithValidSourceShouldAddItToTestSources()
        {
            var testFilePath = "DummyTestFile.txt";
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);
            var options = CommandLineOptions.Instance;
            options.Reset();
            options.FileHelper = mockFileHelper.Object;
            var executor = new TestSourceArgumentExecutor(options);

            executor.Initialize(testFilePath);
            
            // Check if the testsource is present in the TestSources
            Assert.IsTrue(options.Sources.Contains(testFilePath));
        }

        [TestMethod]
        public void ExecutorExecuteReturnArgumentProcessorResultSuccess()
        {
            var options = CommandLineOptions.Instance;
            var executor = new TestSourceArgumentExecutor(options);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
        
        #endregion
    }
}
