// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using CommunicationUtilities.DataCollection.Interfaces;
    using CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using PlatformAbstractions.Interfaces;
    using TestPlatform.DataCollector;

    [TestClass]
    public class DataCollectorMainTests
    {
        private readonly string[] args = {"--port", "1025", "--parentprocessid", "100" };
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<IEnvironment> mockEnvironment;
        private Mock<IDataCollectionRequestHandler> mockDataCollectionRequestHandler;
        private DataCollectorMain dataCollectorMain;


        public DataCollectorMainTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockEnvironment = new Mock<IEnvironment>();
            this.mockDataCollectionRequestHandler = new Mock<IDataCollectionRequestHandler>();
            this.dataCollectorMain = new DataCollectorMain(this.mockProcessHelper.Object, this.mockEnvironment.Object, this.mockDataCollectionRequestHandler.Object);
            this.mockDataCollectionRequestHandler.Setup(rh => rh.WaitForRequestSenderConnection(It.IsAny<int>())).Returns(true);
        }

        [TestCleanup]
        public void CleanUp()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "");
        }

        [TestMethod]
        public void RunShouldTimeoutBasedOneEnvVariable()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "10");
            this.dataCollectorMain.Run(args);
            this.mockDataCollectionRequestHandler.Verify(rh => rh.WaitForRequestSenderConnection(10 * 1000));
        }

        [TestMethod]
        public void RunShouldTimeoutBasedDefaulValueIfEnvVariableNotSet()
        {
            this.dataCollectorMain.Run(args);

            this.mockDataCollectionRequestHandler.Verify(rh => rh.WaitForRequestSenderConnection(EnvironmentHelper.DefaultConnectionTimeout * 1000));
        }

        [TestMethod]
        public void RunShouldThrowIfTimeoutOccured()
        {
            this.mockDataCollectionRequestHandler.Setup(rh => rh.WaitForRequestSenderConnection(It.IsAny<int>())).Returns(false);
            var message = Assert.ThrowsException<TestPlatformException>(() => this.dataCollectorMain.Run(args)).Message;
            Assert.AreEqual(message,
                string.Format(
                    TestPlatform.DataCollector.Resources .Resources.DataCollectorFailedToConnetToVSTestConsole,
                    EnvironmentHelper.DefaultConnectionTimeout,
                    EnvironmentHelper.VstestConnectionTimeout)
                );
        }

    }
}
