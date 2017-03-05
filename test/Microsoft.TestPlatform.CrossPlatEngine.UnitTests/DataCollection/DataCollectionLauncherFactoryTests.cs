// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionLauncherFactoryTests
    {
        private Mock<IProcessHelper> mockProcessHelper;

        public DataCollectionLauncherFactoryTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
        }

        [TestMethod]
        public void GetDataCollectorLauncherShouldReturnDefaultDataCollectionLauncherWithFullCLRRunner()
        {
            mockProcessHelper.Setup(x => x.GetCurrentProcessFileName()).Returns("vstest.console.exe");
            var dataCollectorLauncher = DataCollectionLauncherFactory.GetDataCollectorLauncher(mockProcessHelper.Object);
            Assert.IsInstanceOfType(dataCollectorLauncher, typeof(DefaultDataCollectionLauncher));
        }

        [TestMethod]
        public void GetDataCollectorLauncherShouldReturnDotnetDataCollectionLauncherWithDotnetCore()
        {
            mockProcessHelper.Setup(x => x.GetCurrentProcessFileName()).Returns("dotnet");
            var dataCollectorLauncher = DataCollectionLauncherFactory.GetDataCollectorLauncher(mockProcessHelper.Object);
            Assert.IsInstanceOfType(dataCollectorLauncher, typeof(DotnetDataCollectionLauncher));
        }

        [TestMethod]
        public void GetDataCollectorLauncherShouldBeInsensitiveToCaseOfCurrentProcess()
        {
            mockProcessHelper.Setup(x => x.GetCurrentProcessFileName()).Returns("DOTNET");
            var dataCollectorLauncher = DataCollectionLauncherFactory.GetDataCollectorLauncher(mockProcessHelper.Object);
            Assert.IsInstanceOfType(dataCollectorLauncher, typeof(DotnetDataCollectionLauncher));
        }
    }
}
