// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Processors;
    using System;
    using TestPlatform.CommandLine.Processors;
    using TestPlatform.Utilities.Helpers.Interfaces;

    [TestClass]
    public class BuildBasePathArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnBuildBasePathArgumentProcessorCapabilities()
        {
            BuildBasePathArgumentProcessor processor = new BuildBasePathArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is BuildBasePathArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnBuildBasePathArgumentProcessorCapabilities()
        {
            BuildBasePathArgumentProcessor processor = new BuildBasePathArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is BuildBasePathArgumentExecutor);
        }

        #region BuildBasePathArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            BuildBasePathArgumentProcessorCapabilities capabilities = new BuildBasePathArgumentProcessorCapabilities();
            Assert.AreEqual("/BuildBasePath", capabilities.CommandName);
            Assert.AreEqual("--BuildBasePath|/BuildBasePath:<BuildBasePath>\n      The directory containing the temporary outputs.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.BuildBasePathArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        [TestMethod]
        public void ExecuterInitializeWithNullOrEmptyBuildBasePathShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            BuildBasePathArgumentExecutor executor = new BuildBasePathArgumentExecutor(options);

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(@"The BuildBasePath was not found, provide a valid path and try again.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithInvalidBuildBasePathShouldThrowCommandLineException()
        {
            var options = CommandLineOptions.Instance;
            BuildBasePathArgumentExecutor executor = new BuildBasePathArgumentExecutor(options);

            try
            {
                executor.Initialize(@"C:\Foo.txt");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(@"The BuildBasePath was not found, provide a valid path and try again.", ex.Message);
            }
        }

        [TestMethod]
        public void ExecuterInitializeWithValidBuildBasePathShouldAddBuildBasePathToCommandLineOptions()
        {
            var options = CommandLineOptions.Instance;
            BuildBasePathArgumentExecutor executor = new BuildBasePathArgumentExecutor(options);
            string testBuildBasePath = @"C:\BuildDir";
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(testBuildBasePath)).Returns(true);
            executor.FileHelper = mockFileHelper.Object;
            
            executor.Initialize(testBuildBasePath);
            Assert.AreEqual(testBuildBasePath, options.BuildBasePath);
        }

        [TestMethod]
        public void ExecutorExecuteReturnArgumentProcessorResultSuccess()
        {
            var executor = new BuildBasePathArgumentExecutor(CommandLineOptions.Instance);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }
}
