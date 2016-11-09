// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;

    using Castle.DynamicProxy.Contributors;

    [TestClass]
    public class EnableLoggersArgumentProcessorTests
    {
        [TestInitialize]
        public void Initialize()
        {
            RunTestsArgumentProcessorTests.SetupMockExtensions();
        }

        [TestMethod]
        public void GetMetadataShouldReturnEnableLoggerArgumentProcessorCapabilities()
        {
            EnableLoggerArgumentProcessor processor = new EnableLoggerArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is EnableLoggerArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnEnableLoggerArgumentExecutor()
        {
            EnableLoggerArgumentProcessor processor = new EnableLoggerArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is EnableLoggerArgumentExecutor);
        }

        [TestMethod]
        public void CapabilitiesShouldAppropriateProperties()
        {
            EnableLoggerArgumentProcessorCapabilities capabilities = new EnableLoggerArgumentProcessorCapabilities();
            Assert.AreEqual("/Logger", capabilities.CommandName);
            Assert.AreEqual("--logger|/logger:<Logger Uri/FriendlyName>\n      Specify a logger for test results.  For example, to log results into a \n      Visual Studio Test Results File (TRX) use  /logger:trx.\n      To publish test results to Team Foundation Server, use TfsPublisher as shown below\n      Example: /logger:TfsPublisher;\n                Collection=<team project collection url>;\n                BuildName=<build name>;\n                TeamProject=<team project name>\n                [;Platform=<Defaults to \"Any CPU\">]\n                [;Flavor=<Defaults to \"Debug\">]\n                [;RunTitle=<title>]", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.EnableLoggerArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Logging, capabilities.Priority);

            Assert.AreEqual(true, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }


        [TestMethod]
        public void ExecutorInitializeWithNullOrEmptyArgumentsShouldThrowException()
        {
            var executor = new EnableLoggerArgumentExecutor(TestLoggerManager.Instance);
            Assert.ThrowsException<CommandLineException>(() =>
            {
                executor.Initialize(null);
            });
        }

        [TestMethod]
        public void ExecutorInitializeWithValidArgumentsShouldAddTestLoggerToTestLoggerManager()
        {
            RunTestsArgumentProcessorTests.SetupMockExtensions();
            var testloggerManager = new DummyTestLoggerManager();
            var executor = new EnableLoggerArgumentExecutor(testloggerManager);

            var countBefore = testloggerManager.GetInitializedLoggers.Count;

            executor.Initialize("TestLoggerExtension;Collection=http://localhost:8080/tfs/DefaultCollection;TeamProject=MyProject;BuildName=DailyBuild_20121130.1");
            var countAfter = testloggerManager.GetInitializedLoggers.Count;
            Assert.IsTrue(countBefore == 0);
            Assert.IsTrue(countAfter == 1);
        }

        [TestMethod]
        public void ExectorInitializeShouldThrowExceptionIfInvalidArgumentIsPassed()
        {
            var executor = new EnableLoggerArgumentExecutor(TestLoggerManager.Instance);
            Assert.ThrowsException<CommandLineException>(() =>
            {
                executor.Initialize("TestLoggerExtension;==;;;Collection=http://localhost:8080/tfs/DefaultCollection;TeamProject=MyProject;BuildName=DailyBuild_20121130.1");
            });
        }

        [TestMethod]
        public void ExecutorExecuteShouldReturnArgumentProcessorResultSuccess()
        {
            var executor = new EnableLoggerArgumentExecutor(null);
            var result = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Success, result);
        }
    }

    internal class DummyTestLoggerManager : TestLoggerManager
    {
        public DummyTestLoggerManager()
        {

        }

        public HashSet<String> GetInitializedLoggers
        {
            get
            {
                return InitializedLoggers;
            }
        }

        public static void Cleanup()
        {
            Instance = null;
        }
    }
}
