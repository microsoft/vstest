// Copyright (c) Microsoft Corporation. All rights reserved.	
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Internal
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ProgressIndicatorTests
    {
        ProgressIndicator indicator;
        Mock<IOutput> consoleOutput;
        Mock<IConsoleHelper> consoleHelper;

        [TestInitialize]
        public void TestInit()
        {
            consoleOutput = new Mock<IOutput>();
            consoleHelper = new Mock<IConsoleHelper>();
            consoleHelper.Setup(c => c.WindowWidth).Returns(100);
            consoleHelper.Setup(c => c.CursorTop).Returns(20);
            indicator = new ProgressIndicator(consoleOutput.Object, consoleHelper.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            indicator.Stop();
        }

        [TestMethod]
        public void StartShouldStartPrintingProgressMessage()
        {
            indicator.Start();
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
            Assert.IsTrue(indicator.IsRunning);
        }

        [TestMethod]
        public void StartShouldShowProgressMessage()
        {
            indicator.Start();

            consoleHelper.Setup(c => c.CursorLeft).Returns(30);
            System.Threading.Thread.Sleep(1500);

            Assert.IsTrue(indicator.IsRunning);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
            consoleOutput.Verify(m => m.Write(".", OutputLevel.Information), Times.Once);
        }

        [TestMethod]
        public void PauseShouldClearTheStdOutMessage()
        {
            indicator.Start();
            indicator.Pause();

            Assert.IsFalse(indicator.IsRunning);
            string clearMessage = new string(' ', consoleHelper.Object.WindowWidth);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
            consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Once);

            consoleHelper.Verify(ch => ch.SetCursorPosition(0, 20), Times.Exactly(2));
        }

        [TestMethod]
        public void PauseStartAndStopShouldClearPrintProgressAndThenClearTheStdOutMessage()
        {
            indicator.Start();
            indicator.Pause();
            indicator.Start();
            indicator.Stop();

            Assert.IsFalse(indicator.IsRunning);
            string clearMessage = new string(' ', consoleHelper.Object.WindowWidth);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Exactly(2));
            consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Exactly(2));
            consoleHelper.Verify(ch => ch.SetCursorPosition(0, 20), Times.Exactly(4));
        }

        [TestMethod]
        public void StopShouldClearTheStdOutMessage()
        {
            indicator.Start();
            indicator.Stop();

            Assert.IsFalse(indicator.IsRunning);
            string clearMessage = new string(' ', consoleHelper.Object.WindowWidth);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
            consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Once);
            consoleHelper.Verify(ch => ch.SetCursorPosition(0, 20), Times.Exactly(2));
        }
    }
}