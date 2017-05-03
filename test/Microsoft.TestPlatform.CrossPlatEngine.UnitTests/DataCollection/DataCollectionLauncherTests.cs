// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionLauncherTests
    {
        private DummyDataCollectionLauncher dummyDataCollectionLauncher;

        private Mock<IMessageLogger> mockMessageLogger;

        private Mock<IProcessHelper> mockProcessHelper;

        private bool isDataCollectorExitedInvoked;

        private bool isDataCollectorLaunchedInvoked;

        private int dataCollectorLaunchedInvokedCount;

        private HostProviderEventArgs hostProviderEventArgs;

        public DataCollectionLauncherTests()
        {
            this.mockMessageLogger = new Mock<IMessageLogger>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.hostProviderEventArgs = new HostProviderEventArgs("DummyMessage");
            this.dummyDataCollectionLauncher = new DummyDataCollectionLauncher(this.mockProcessHelper.Object, this.mockMessageLogger.Object);
            this.dummyDataCollectionLauncher.DataCollectorLaunched += DummyDataCollectionLauncher_DataCollectorLaunched;
            this.dummyDataCollectionLauncher.DataCollectorExited += DummyDataCollectionLauncher_DataCollectorExited;
        }

        [TestMethod]
        public void OnHostLaunchedShouldInvokeDataCollectorLaunchedEvent()
        {
            this.dummyDataCollectionLauncher.OnDataCollectorLaunched(hostProviderEventArgs);
            Assert.IsTrue(this.isDataCollectorLaunchedInvoked);
        }

        [TestMethod]
        public void OnDataCollectorExitedShouldInvokeDataCollectorExitedEvent()
        {
            this.dummyDataCollectionLauncher.OnDataCollectorExited(hostProviderEventArgs);
            Assert.IsTrue(this.isDataCollectorExitedInvoked);
            Assert.AreEqual(1, this.dataCollectorLaunchedInvokedCount);
        }

        public void OnDataCollectorExitedShouldNotInvokeDataCollectorExitedEventMoreThanOnce()
        {
            this.dummyDataCollectionLauncher.OnDataCollectorExited(hostProviderEventArgs);
            this.dummyDataCollectionLauncher.OnDataCollectorExited(hostProviderEventArgs);
            Assert.IsTrue(this.isDataCollectorExitedInvoked);
            Assert.AreEqual(1, this.dataCollectorLaunchedInvokedCount);
        }


        private void DummyDataCollectionLauncher_DataCollectorExited(object sender, VisualStudio.TestPlatform.ObjectModel.Host.HostProviderEventArgs e)
        {
            this.isDataCollectorExitedInvoked = true;
            this.dataCollectorLaunchedInvokedCount++;
        }

        private void DummyDataCollectionLauncher_DataCollectorLaunched(object sender, VisualStudio.TestPlatform.ObjectModel.Host.HostProviderEventArgs e)
        {
            this.isDataCollectorLaunchedInvoked = true;
        }
    }

    public class DummyDataCollectionLauncher : DataCollectionLauncher
    {
        public DummyDataCollectionLauncher(IProcessHelper processHelper, IMessageLogger messageLogger)
            : base(processHelper, messageLogger)
        {
        }

        public override int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            throw new System.NotImplementedException();
        }
    }
}
