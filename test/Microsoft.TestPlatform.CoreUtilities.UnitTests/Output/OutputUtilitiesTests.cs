// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Output
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Moq;

    [TestClass]
    public class OutputUtilitiesTests
    {
        private Mock<IOutput> mockOutput;

        [TestInitialize]
        public void TestInit()
        {
            mockOutput = new Mock<IOutput>();
        }

        [TestMethod]
        public void OutputErrorForSimpleMessageShouldOutputTheMessageString()
        {
            mockOutput.Object.Error("HelloError", null);
            mockOutput.Verify(o => o.WriteLine("HelloError", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void OutputErrorForMessageWithParamsShouldOutputFormattedMessage()
        {
            mockOutput.Object.Error("HelloError {0} {1}", "Foo", "Bar");
            mockOutput.Verify(o => o.WriteLine("HelloError Foo Bar", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void OutputWarningForSimpleMessageShouldOutputTheMessageString()
        {
            mockOutput.Object.Warning("HelloWarning", null);
            mockOutput.Verify(o => o.WriteLine("HelloWarning", OutputLevel.Warning), Times.Once());
        }

        [TestMethod]
        public void OutputWarningForMessageWithParamsShouldOutputFormattedMessage()
        {
            mockOutput.Object.Warning("HelloWarning {0} {1}", "Foo", "Bar");
            mockOutput.Verify(o => o.WriteLine("HelloWarning Foo Bar", OutputLevel.Warning), Times.Once());
        }

        [TestMethod]
        public void OutputInformationForSimpleMessageShouldOutputTheMessageString()
        {
            mockOutput.Object.Information("HelloInformation", null);
            mockOutput.Verify(o => o.WriteLine("HelloInformation", OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void OutputInformationForMessageWithParamsShouldOutputFormattedMessage()
        {
            mockOutput.Object.Information("HelloInformation {0} {1}", "Foo", "Bar");
            mockOutput.Verify(o => o.WriteLine("HelloInformation Foo Bar", OutputLevel.Information), Times.Once());
        }
    }
}
