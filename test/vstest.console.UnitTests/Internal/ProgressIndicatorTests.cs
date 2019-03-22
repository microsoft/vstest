// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Internal
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;

    [TestClass]
    public class ProgressIndicatorTests
    {
        ProgressIndicator indicator;
        Mock<IOutput> consoleOutput;

        [TestInitialize]
        public void TestInit()
        {
            consoleOutput = new Mock<IOutput>();
            indicator = new ProgressIndicator(consoleOutput.Object);
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
        }

        [TestMethod]
        public void StartShouldShowProgressMessage()
        {
            indicator.Start();
            System.Threading.Thread.Sleep(1500);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
            consoleOutput.Verify(m => m.Write(".", OutputLevel.Information), Times.Once);
        }

        [TestMethod]
        public void PauseShouldClearTheStdOutMessage()
        {
            indicator.Start();
            indicator.Pause();

            string clearMessage = new string(' ', Console.WindowWidth);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
            consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Once);
        }

        [TestMethod]
        public void PauseStartAndStopShouldClearPrintProgressAndThenClearTheStdOutMessage()
        {
            indicator.Start();
            indicator.Pause();
            indicator.Start();
            indicator.Stop();

            string clearMessage = new string(' ', Console.WindowWidth);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Exactly(2));
            consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Exactly(2));
        }

        [TestMethod]
        public void StopShouldClearTheStdOutMessage()
        {
            indicator.Start();
            indicator.Stop();

            string clearMessage = new string(' ', Console.WindowWidth);
            consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
            consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Once);
        }
    }
}
