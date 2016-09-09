namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests
{
    using Implementations;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Processors;
    using System;
    using System.Collections.Generic;
    using TestPlatform.CommandLine.Processors;
    using TestPlatform.Utilities.Helpers.Interfaces;
    using Utilities;
    using System.Linq;
    using System.Reflection;
    using CoreUtilities.Tracing;

    using Moq;
    using CoreUtilities.Tracing.Interfaces;

    [TestClass]
    public class ExecutorUnitTests
    {
        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
        }

        /// <summary>
        /// Executor should Print splash screen first
        /// </summary>
        [TestMethod]
        public void ExecutorPrintsSplashScreenTest()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute("/?");

            Assert.AreEqual(0, exitCode, "Exit code must be One for bad arguments");

            // Verify that messages exist
            Assert.IsTrue(mockOutput.Messages.Count > 0, "Executor must print atleast copyright info");
            Assert.IsNotNull(mockOutput.Messages.First().Message, "First Printed Message cannot be null or empty");
            
            // Just check first 20 characters - don't need to check whole thing as assembly version is variable
            Assert.IsTrue(mockOutput.Messages.First().Message.Contains(
                Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.MicrosoftCommandLineTitle.Substring(0, 20)), 
                "First Printed message must be Microsoft Copyright");
        }


        /// <summary>
        /// Executor should try find "project.json" if empty args given
        /// </summary>
        [TestMethod]
        public void ExecutorEmptyArgsCallRunTestsProcessor()
        {            
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(null);

            // Since no projectjsons exist in current folder it should fail
            Assert.AreEqual(1, exitCode, "Exit code must be One for bad arguments");

            //// Verify that messages exist
            //Assert.IsTrue(mockOutput.Messages.Count > 0, "Executor must print atleast copyright info");
            //Assert.IsNotNull(mockOutput.Messages.First().Message, "First Printed Message cannot be null or empty");

            //// Just check first 20 characters - don't need to check whole thing as assembly version is variable
            //Assert.IsTrue(mockOutput.Messages.First().Message.Contains(
            //    Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.MicrosoftCommandLineTitle.Substring(0, 20)),
            //    "First Printed message must be Microsoft Copyright");
        }

        [TestMethod]
        public void ExecuteShouldInstrumentVsTestConsoleStart()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(It.IsAny<string[]>());

            this.mockTestPlatformEventSource.Verify(x => x.VsTestConsoleStart(), Times.Once);
        }

        [TestMethod]
        public void ExecuteShouldInstrumentVsTestConsoleStop()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(It.IsAny<string[]>());

            this.mockTestPlatformEventSource.Verify(x => x.VsTestConsoleStop(), Times.Once);
        }


        private class MockOutput : IOutput
        {
            public List<OutputMessage> Messages { get; set; } = new List<OutputMessage>();

            public void Write(string message, OutputLevel level)
            {
                Messages.Add(new OutputMessage() { Message = message, Level = level });
            }

            public void WriteLine(string message, OutputLevel level)
            {
                Messages.Add(new OutputMessage() { Message = message, Level = level });
            }
        }

        private class OutputMessage
        {
            public string Message { get; set; }
            public OutputLevel Level { get; set; }
        }
    }

        
}
