// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Output
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Moq;
    using System;

    [TestClass]
    public class OutputExtensionsTests
    {
        private Mock<IOutput> mockOutput;
        private ConsoleColor color;
        private ConsoleColor previousColor;
        private ConsoleColor newColor;

        public OutputExtensionsTests()
        {
            // Setting Console.ForegroundColor to newColor which will be used to determine whether
            // test command output is redirecting to file or writting to console.
            // If command output is redirecting to file, then Console.ForegroundColor can't be modified.
            // So that tests which assert Console.ForegroundColor should not run.
            this.previousColor = Console.ForegroundColor;
            this.newColor = previousColor == ConsoleColor.Gray
                ? ConsoleColor.Black
                : ConsoleColor.Blue;
            Console.ForegroundColor = this.newColor;

            this.mockOutput = new Mock<IOutput>();
            this.color = Console.ForegroundColor;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback(() =>
            {
                this.color = Console.ForegroundColor;
            });
        }

        [TestCleanup]
        public void CleanUp()
        {
            Console.ForegroundColor = previousColor;
        }

        [TestMethod]
        public void OutputErrorForSimpleMessageShouldOutputTheMessageString()
        {
            this.mockOutput.Object.Error(false, "HelloError", null);
            this.mockOutput.Verify(o => o.WriteLine("HelloError", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void OutputErrorForSimpleMessageShouldOutputTheMessageStringWithPrefixIfSet()
        {
            this.mockOutput.Object.Error(true, "HelloError", null);
            this.mockOutput.Verify(o => o.WriteLine("Error: HelloError", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void OutputErrorForSimpleMessageShouldSetConsoleColorToRed()
        {
            if (CanNotSetConsoleForegroundColor())
            {
                return;
            }

            this.mockOutput.Object.Error(false, "HelloError", null);
            Assert.IsTrue(this.color == ConsoleColor.Red, "Console color not set.");
        }

        [TestMethod]
        public void OutputErrorForMessageWithParamsShouldOutputFormattedMessage()
        {
            this.mockOutput.Object.Error(false, "HelloError {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloError Foo Bar", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void OutputWarningForSimpleMessageShouldOutputTheMessageString()
        {
            this.mockOutput.Object.Warning(false, "HelloWarning", null);
            this.mockOutput.Verify(o => o.WriteLine("HelloWarning", OutputLevel.Warning), Times.Once());
        }

        [TestMethod]
        public void OutputWarningForSimpleMessageShouldSetConsoleColorToYellow()
        {
            if (CanNotSetConsoleForegroundColor())
            {
                return;
            }

            this.mockOutput.Object.Warning(false, "HelloWarning", null);
            Assert.IsTrue(this.color == ConsoleColor.Yellow);
        }

        [TestMethod]
        public void OutputWarningForMessageWithParamsShouldOutputFormattedMessage()
        {
            this.mockOutput.Object.Warning(false, "HelloWarning {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloWarning Foo Bar", OutputLevel.Warning), Times.Once());
        }

        [TestMethod]
        public void OutputInformationForSimpleMessageShouldOutputTheMessageString()
        {
            this.mockOutput.Object.Information(false, ConsoleColor.Green, "HelloInformation", null);
            this.mockOutput.Verify(o => o.WriteLine("HelloInformation", OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void OutputInformationForSimpleMessageShouldSetConsoleColorToGivenColor()
        {
            if (CanNotSetConsoleForegroundColor())
            {
                return;
            }

            this.mockOutput.Object.Information(false, ConsoleColor.Green, "HelloInformation", null);
            Assert.IsTrue(this.color == ConsoleColor.Green);
        }

        [TestMethod]
        public void OutputInformationForMessageWithParamsShouldOutputFormattedMessage()
        {
            this.mockOutput.Object.Information(false, "HelloInformation {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloInformation Foo Bar", OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void OutputInformationShouldNotChangeConsoleOutputColor()
        {
            if (CanNotSetConsoleForegroundColor())
            {
                return;
            }

            ConsoleColor color1 = Console.ForegroundColor, color2 = Console.ForegroundColor == ConsoleColor.Red ? ConsoleColor.Black : ConsoleColor.Red;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback(() =>
            {
                color2 = Console.ForegroundColor;
            });

            this.mockOutput.Object.Information(false, "HelloInformation {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloInformation Foo Bar", OutputLevel.Information), Times.Once());
            Assert.IsTrue(color1 == color2);
        }

        private bool CanNotSetConsoleForegroundColor()
        {
            if (Console.ForegroundColor != this.newColor)
            {
                Assert.Inconclusive("Can't set Console foreground color. Might be because process output redirect to file.");
                return true;
            }
            return false;
        }
    }
}
