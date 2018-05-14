// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceDataCollector.UnitTests
{
    using Coverage;
    using Coverage.Interfaces;
    using TestTools.UnitTesting;

    [TestClass]
    public class VanguardCommandBuilderTests
    {
        private VanguardCommandBuilder vanguardCommandBuilder;

        public VanguardCommandBuilderTests()
        {
            this.vanguardCommandBuilder = new VanguardCommandBuilder();
        }

        [TestMethod]
        public void GenerateCommandLineShouldReturnCommandForCollectCommand()
        {
            string sessionName = "session1", outputName = "output1", configFileName = "configFileName1";
            string expectedCommand = $"collect /session:{sessionName} /output:\"{outputName}\" /config:\"{configFileName}\"";

            var actualCommand = this.vanguardCommandBuilder.GenerateCommandLine(
                VanguardCommand.Collect,
                sessionName,
                outputName,
                configFileName);

            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [TestMethod]
        public void GenerateCommandLineShouldReturnCommandForShutdownCommand()
        {
            string sessionName = "session1";
            string expectedCommand = $"shutdown /session:{sessionName}";

            var actualCommand = this.vanguardCommandBuilder.GenerateCommandLine(
                VanguardCommand.Shutdown,
                sessionName,
                null,
                null);

            Assert.AreEqual(expectedCommand, actualCommand);
        }
    }
}
