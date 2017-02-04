// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestAdapterPathArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnTestAdapterPathArgumentProcessorCapabilities()
        {
            var processor = new TestAdapterPathArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is TestAdapterPathArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnTestAdapterPathArgumentProcessorCapabilities()
        {
            var processor = new TestAdapterPathArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is TestAdapterPathArgumentExecutor);
        }

        #region TestAdapterPathArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new TestAdapterPathArgumentProcessorCapabilities();
            Assert.AreEqual("/TestAdapterPath", capabilities.CommandName);
            Assert.AreEqual(("--TestAdapterPath|/TestAdapterPath" + Environment.NewLine + "      This makes vstest.console.exe process use custom test adapters" + Environment.NewLine + "      from a given path (if any) in the test run. " + Environment.NewLine + "      Example  /TestAdapterPath:<pathToCustomAdapters>").Replace("\r", string.Empty), capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.TestAdapterPathArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.TestAdapterPath, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region TestAdapterPathArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNull()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockTestPlatform.Object, mockOutput.Object);

            var message =
                @"The /TestAdapterPath parameter requires a value, which is path of a location containing custom test adapters. Example:  /TestAdapterPath:c:\MyCustomAdapters";

            var isExceptionThrown = false;

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsAWhiteSpace()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockTestPlatform.Object, mockOutput.Object);

            var message =
                @"The /TestAdapterPath parameter requires a value, which is path of a location containing custom test adapters. Example:  /TestAdapterPath:c:\MyCustomAdapters";

            var isExceptionThrown = false;

            try
            {
                executor.Initialize("  ");
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldThrowIfPathDoesNotExist()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockTestPlatform.Object, mockOutput.Object);

            var folder = "C:\\temp\\thisfolderdoesnotexist";

            var message = string.Format(
                @"The path '{0}' specified in the 'TestAdapterPath' is invalid. Error: {1}",
                folder,
                "The custom test adapter search path provided was not found, provide a valid path and try again.");

            var isExceptionThrown = false;

            try
            {
                executor.Initialize("\"" + folder + "\"");
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldUpdateAdditionalExtensionsWithTestAdapterPath()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestableTestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockTestPlatform.Object, mockOutput.Object);

            var currentAssemblyPath = typeof(TestAdapterPathArgumentExecutor).GetTypeInfo().Assembly.Location;
            var currentFolder = Path.GetDirectoryName(currentAssemblyPath);

            executor.TestAdapters = (directory) =>
                {
                    if (string.Equals(directory, currentFolder))
                    {
                        return new List<string>
                                   {
                                       typeof(TestAdapterPathArgumentExecutor).GetTypeInfo()
                                           .Assembly.Location
                                   };
                    }

                    return new List<string> { };
                };


            executor.Initialize(currentFolder);

            mockTestPlatform.Verify(tp => tp.UpdateExtensions(new List<string> { currentAssemblyPath }, false), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldReportIfNoTestAdaptersFoundInPath()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestableTestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockTestPlatform.Object, mockOutput.Object);

            var currentAssemblyPath = typeof(TestAdapterPathArgumentExecutor).GetTypeInfo().Assembly.Location;
            var currentFolder = Path.GetDirectoryName(currentAssemblyPath);

            executor.TestAdapters = (directory) =>
            {
                return new List<string> { };
            };

            executor.Initialize(currentFolder);

            mockOutput.Verify(
                o =>
                o.WriteLine(
                    string.Format(
                        "Warning: The path '{0}' specified in the 'TestAdapterPath' does not contain any test adapters, provide a valid path and try again.",
                        currentFolder),
                    OutputLevel.Warning));

        }

        #endregion

        #region Testable implementations

        private class TestableTestAdapterPathArgumentExecutor : TestAdapterPathArgumentExecutor
        {
            internal TestableTestAdapterPathArgumentExecutor(CommandLineOptions options, ITestPlatform testPlatform, IOutput output)
                : base(options, testPlatform, output)
            {
            }

            internal Func<string, IEnumerable<string>> TestAdapters { get; set; }

            internal override IEnumerable<string> GetTestAdaptersFromDirectory(string directory)
            {
                if (this.TestAdapters != null)
                {
                    return this.TestAdapters(directory);
                }
                return new List<string> { };
            }
        }

        #endregion
    }
}
