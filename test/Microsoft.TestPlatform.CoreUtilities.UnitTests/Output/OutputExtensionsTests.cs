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

        public OutputExtensionsTests()
        {
            this.mockOutput = new Mock<IOutput>();
            this.color = Console.ForegroundColor;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback(() =>
            {
                this.color = Console.ForegroundColor;
            });
        }

        [TestMethod]
        public void OutputErrorForSimpleMessageShouldOutputTheMessageStringWithColor()
        {
            this.mockOutput.Object.Error("HelloError", null);
            this.mockOutput.Verify(o => o.WriteLine("HelloError", OutputLevel.Error), Times.Once());
        }

        [Ignore]
        // Should be removed once "removal of CreateNoWindow=true in VSTestForwardingApp.cs merged in dotnet cli repo"
        // https://github.com/Microsoft/vstest/pull/641
        [TestMethod]
        public void OutputErrorForSimpleMessageShouldSetConsoleColorToRed()
        {
            this.mockOutput.Object.Error("HelloError", null);
            Assert.IsTrue(this.color == ConsoleColor.Red, "Console color not set.");
        }

        [TestMethod]
        public void OutputErrorForMessageWithParamsShouldOutputFormattedMessage()
        {
            this.mockOutput.Object.Error("HelloError {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloError Foo Bar", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void OutputWarningForSimpleMessageShouldOutputTheMessageStringWithColor()
        {
            this.mockOutput.Object.Warning("HelloWarning", null);
            this.mockOutput.Verify(o => o.WriteLine("HelloWarning", OutputLevel.Warning), Times.Once());
        }

        [Ignore]
        // Should be removed once "removal of CreateNoWindow=true in VSTestForwardingApp.cs merged in dotnet cli repo"
        // https://github.com/Microsoft/vstest/pull/641
        [TestMethod]
        public void OutputWarningForSimpleMessageShouldSetConsoleColorToYellow()
        {
            this.mockOutput.Object.Warning("HelloWarning", null);
            Assert.IsTrue(this.color == ConsoleColor.Yellow);
        }

        [TestMethod]
        public void OutputWarningForMessageWithParamsShouldOutputFormattedMessage()
        {
            this.mockOutput.Object.Warning("HelloWarning {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloWarning Foo Bar", OutputLevel.Warning), Times.Once());
        }

        [TestMethod]
        public void OutputInformationForSimpleMessageShouldOutputTheMessageStringWithColor()
        {
            this.mockOutput.Object.Information(ConsoleColor.Green, "HelloInformation", null);
            this.mockOutput.Verify(o => o.WriteLine("HelloInformation", OutputLevel.Information), Times.Once());
        }

        [Ignore]
        // Should be removed once "removal of CreateNoWindow=true in VSTestForwardingApp.cs merged in dotnet cli repo"
        // https://github.com/Microsoft/vstest/pull/641
        [TestMethod]
        public void OutputInformationForSimpleMessageShouldSetConsoleColorToGivenColor()
        {
            this.mockOutput.Object.Information(ConsoleColor.Green, "HelloInformation", null);
            Assert.IsTrue(this.color == ConsoleColor.Green);
        }

        [TestMethod]
        public void OutputInformationForMessageWithParamsShouldOutputFormattedMessage()
        {
            this.mockOutput.Object.Information("HelloInformation {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloInformation Foo Bar", OutputLevel.Information), Times.Once());
        }

        [Ignore]
        // Should be removed once "removal of CreateNoWindow=true in VSTestForwardingApp.cs merged in dotnet cli repo"
        // https://github.com/Microsoft/vstest/pull/641
        [TestMethod]
        public void OutputInformationShouldNotChangeConsoleOutputColor()
        {
            ConsoleColor color1 = Console.ForegroundColor, color2 = Console.ForegroundColor == ConsoleColor.Red ? ConsoleColor.Black : ConsoleColor.Red;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback(() =>
            {
                color2 = Console.ForegroundColor;
            });

            this.mockOutput.Object.Information("HelloInformation {0} {1}", "Foo", "Bar");
            this.mockOutput.Verify(o => o.WriteLine("HelloInformation Foo Bar", OutputLevel.Information), Times.Once());
            Assert.IsTrue(color1 == color2);
        }
    }
}
