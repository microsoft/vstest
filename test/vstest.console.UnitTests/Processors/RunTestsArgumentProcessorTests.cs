// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Implementations;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// Tests for RunTestsArgumentProcessor
    /// </summary>
    [TestClass]
    public class RunTestsArgumentProcessorTests
    {
        MockFileHelper mockFileHelper;
        string dummyTestFilePath = "DummyTest.dll";

        public RunTestsArgumentProcessorTests()
        {
            this.mockFileHelper = new MockFileHelper();
            this.mockFileHelper.ExistsInvoker = (path) =>
            {
                if (string.Equals(path, this.dummyTestFilePath))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            };
            SetupMockExtensions();
        }

        [TestMethod]
        public void GetMetadataShouldReturnRunTestsArgumentProcessorCapabilities()
        {
            RunTestsArgumentProcessor processor = new RunTestsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is RunTestsArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnRunTestsArgumentProcessorCapabilities()
        {
            RunTestsArgumentProcessor processor = new RunTestsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is RunTestsArgumentExecutor);
        }

        #region RunTestsArgumentProcessorCapabilitiesTests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            RunTestsArgumentProcessorCapabilities capabilities = new RunTestsArgumentProcessorCapabilities();
            Assert.AreEqual("/RunTests", capabilities.CommandName);
            Assert.AreEqual("[TestFileNames]\n      Run tests from the specified files. Separate multiple test file names\n      by spaces.\n      Examples: mytestproject.dll\n                mytestproject.dll myothertestproject.exe", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.RunTestsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(true, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(true, capabilities.IsSpecialCommand);
        }
        #endregion

        #region RunTestsArgumentExecutorTests

        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldReturnFail()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestLoggerManager.Instance, TestRunResultAggregator.Instance);
          
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);
           
            ArgumentProcessorResult argumentProcessorResult = executor.Execute();

            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchTestPlatformExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            
            this.ResetAndAddSourceToCommandLineOptions();
            
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance);
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchSettingsExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            
            this.ResetAndAddSourceToCommandLineOptions();
            
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance);
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchInvalidOperationExceptionAndReturnFail()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new InvalidOperationException("DummyInvalidOperationException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            
            this.ResetAndAddSourceToCommandLineOptions();
            
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance);
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowOtherExceptions()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new Exception("DummyException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            
            this.ResetAndAddSourceToCommandLineOptions();
            
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance);
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

            Assert.ThrowsException<Exception>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldForListOfTestsReturnSuccess()
        {
            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();
            var mockConsoleOutput = new Mock<IOutput>();

            List<TestCase> list = new List<TestCase>();
            list.Add(new TestCase("Test1", new Uri("http://FooTestUri1"), "Source1"));
            list.Add(new TestCase("Test2", new Uri("http://FooTestUri2"), "Source2"));
            var mockTestRunStats = new Mock<ITestRunStatistics>();

            var args = new TestRunCompleteEventArgs(mockTestRunStats.Object, false, false, null, null, new TimeSpan());

            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);
            
            this.ResetAndAddSourceToCommandLineOptions();
            
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestLoggerManager.Instance, TestRunResultAggregator.Instance);
            var executor = new RunTestsArgumentExecutor(CommandLineOptions.Instance, null, testRequestManager);

            var result = executor.Execute();
            // Assert
            mockTestRunRequest.Verify(tr => tr.ExecuteAsync(), Times.Once);
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }

        #endregion

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper;
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
        }

        public static void SetupMockExtensions()
        {
            SetupMockExtensions(() => { });
        }

        public static void SetupMockExtensions(Action callback)
        {
            SetupMockExtensions(new string[] { typeof(RunTestsArgumentProcessorTests).GetTypeInfo().Assembly.Location, typeof(ConsoleLogger).GetTypeInfo().Assembly.Location }, callback);
        }

        public static void SetupMockExtensions(string[] extensions, Action callback)
        {
            // Setup mocks.
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = true;

            testableTestPluginCache.FilesInDirectory = (path, pattern) =>
            {
                if (pattern.Equals("*.dll"))
                {
                    callback.Invoke();
                    return extensions;
                }
                return new string[] { };
            };

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;
        }

        [ExtensionUri("testlogger://logger")]
        [FriendlyName("TestLoggerExtension")]
        private class ValidLogger3 : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                events.TestRunMessage += TestMessageHandler;
                events.TestRunComplete += Events_TestRunComplete;
                events.TestResult += Events_TestResult;
            }

            private void Events_TestResult(object sender, TestResultEventArgs e)
            {
            }

            private void Events_TestRunComplete(object sender, TestRunCompleteEventArgs e)
            {

            }

            private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
            {
            }
        }
    }

    #region Testable implementation

    public class TestableTestPluginCache : TestPluginCache
    {
        public TestableTestPluginCache(IPathUtilities pathUtilities)
            : base(pathUtilities)
        {
        }

        internal Func<string, string, string[]> FilesInDirectory
        {
            get;
            set;
        }

        public bool DoesDirectoryExistSetter
        {
            get;
            set;
        }

        public Func<IEnumerable<string>, TestExtensions> TestExtensionsSetter { get; set; }

        internal override bool DoesDirectoryExist(string path)
        {
            return this.DoesDirectoryExistSetter;
        }

        internal override string[] GetFilesInDirectory(string path, string searchPattern)
        {
            return this.FilesInDirectory.Invoke(path, searchPattern);
        }

        internal override TestExtensions GetTestExtensions(IEnumerable<string> extensions)
        {
            if (this.TestExtensionsSetter == null)
            {
                return base.GetTestExtensions(extensions);
            }
            else
            {
                return this.TestExtensionsSetter.Invoke(extensions);
            }
        }
    }

    #endregion 

}
