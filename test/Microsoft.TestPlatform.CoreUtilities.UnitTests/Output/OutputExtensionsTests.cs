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

        [TestInitialize]
        public void TestInit()
        {
            mockOutput = new Mock<IOutput>();
            color = Console.ForegroundColor;
            mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback(() =>
            {
                color = Console.ForegroundColor;
            });
        }

        [TestMethod]
        public void OutputErrorForSimpleMessageShouldOutputTheMessageStringWithColor()
        {
            mockOutput.Object.Error("HelloError", null);
            mockOutput.Verify(o => o.WriteLine("HelloError", OutputLevel.Error), Times.Once());
            Assert.IsTrue(this.color == ConsoleColor.Red, "Console color not set.");
        }

        [TestMethod]
        public void OutputErrorForMessageWithParamsShouldOutputFormattedMessage()
        {
            mockOutput.Object.Error("HelloError {0} {1}", "Foo", "Bar");
            mockOutput.Verify(o => o.WriteLine("HelloError Foo Bar", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void OutputWarningForSimpleMessageShouldOutputTheMessageStringWithColor()
        {
            mockOutput.Object.Warning("HelloWarning", null);
            mockOutput.Verify(o => o.WriteLine("HelloWarning", OutputLevel.Warning), Times.Once());
            Assert.IsTrue(color == ConsoleColor.Yellow);
        }

        [TestMethod]
        public void OutputWarningForMessageWithParamsShouldOutputFormattedMessage()
        {
            mockOutput.Object.Warning("HelloWarning {0} {1}", "Foo", "Bar");
            mockOutput.Verify(o => o.WriteLine("HelloWarning Foo Bar", OutputLevel.Warning), Times.Once());
        }

        [TestMethod]
        public void OutputInformationForSimpleMessageShouldOutputTheMessageStringWithColor()
        {
            mockOutput.Object.Information(ConsoleColor.Green, "HelloInformation");
            mockOutput.Verify(o => o.WriteLine("HelloInformation", OutputLevel.Information), Times.Once());
            Assert.IsTrue(color == ConsoleColor.Green);
        }

        [TestMethod]
        public void OutputInformationForMessageWithParamsShouldOutputFormattedMessage()
        {
            ConsoleColor color1 = Console.ForegroundColor, color2 = Console.ForegroundColor == ConsoleColor.Red? ConsoleColor.Black : ConsoleColor.Red;
            mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback(() =>
            {
                color2 = Console.ForegroundColor;
            });

            mockOutput.Object.Information("HelloInformation {0} {1}", "Foo", "Bar");
            mockOutput.Verify(o => o.WriteLine("HelloInformation Foo Bar", OutputLevel.Information), Times.Once());
            Assert.IsTrue(color1 == color2);
        }
    }
}
