// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DotnetDataCollectionLauncherTests
    {
        private Mock<IFileHelper> mockFileHelper;

        private Mock<IProcessHelper> mockProcessHelper;

        private Mock<IMessageLogger> mockMessageLogger;

        private DotnetDataCollectionLauncher dataCollectionLauncher;

        public DotnetDataCollectionLauncherTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockMessageLogger = new Mock<IMessageLogger>();

            this.dataCollectionLauncher = new DotnetDataCollectionLauncher(this.mockProcessHelper.Object, this.mockFileHelper.Object, this.mockMessageLogger.Object);
        }

        [TestMethod]
        public void LaunchDataCollectorShouldLaunchDataCollectorProcess()
        {
            List<string> arguments = new List<string>();
            this.dataCollectionLauncher.LaunchDataCollector(null, arguments);

            this.mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<Action<object, string>>(), It.IsAny<Action<Object>>()), Times.Once());
        }

        [TestMethod]
        public void LaunchDataCollectorShouldAppendDoubleQuoteForDataCollectorDllPath()
        {
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DefaultDataCollectionLauncher).GetTypeInfo().Assembly.GetAssemblyLocation());
            var dataCollectorAssemblyPath = Path.Combine(currentWorkingDirectory, "datacollector.dll");

            List<string> arguments = new List<string>();
            this.dataCollectionLauncher.LaunchDataCollector(null, arguments);

            this.mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), string.Format("{0} \"{1}\" {2} ", "exec", dataCollectorAssemblyPath, string.Join(" ", arguments)), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<Action<object, string>>(), It.IsAny<Action<Object>>()), Times.Once());
        }
    }
}
