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
        private readonly Mock<IFileHelper> mockFileHelper;

        private readonly Mock<IProcessHelper> mockProcessHelper;

        private readonly Mock<IMessageLogger> mockMessageLogger;

        private readonly DotnetDataCollectionLauncher dataCollectionLauncher;

        public DotnetDataCollectionLauncherTests()
        {
            mockFileHelper = new Mock<IFileHelper>();
            mockProcessHelper = new Mock<IProcessHelper>();
            mockMessageLogger = new Mock<IMessageLogger>();

            dataCollectionLauncher = new DotnetDataCollectionLauncher(mockProcessHelper.Object, mockFileHelper.Object, mockMessageLogger.Object);
        }

        [TestMethod]
        public void LaunchDataCollectorShouldLaunchDataCollectorProcess()
        {
            List<string> arguments = new();
            dataCollectionLauncher.LaunchDataCollector(null, arguments);

            mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<Action<object, string>>(), It.IsAny<Action<Object>>(), It.IsAny<Action<object, string>>()), Times.Once());
        }

        [TestMethod]
        public void LaunchDataCollectorShouldAppendDoubleQuoteForDataCollectorDllPath()
        {
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DefaultDataCollectionLauncher).GetTypeInfo().Assembly.GetAssemblyLocation());
            var dataCollectorAssemblyPath = Path.Combine(currentWorkingDirectory, "datacollector.dll");

            List<string> arguments = new();
            dataCollectionLauncher.LaunchDataCollector(null, arguments);

            mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), string.Format("{0} \"{1}\" {2} ", "exec", dataCollectorAssemblyPath, string.Join(" ", arguments)), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<Action<object, string>>(), It.IsAny<Action<Object>>(), It.IsAny<Action<object, string>>()), Times.Once());
        }

        [TestMethod]
        public void LaunchDataCollectorShouldLaunchDataCollectorProcessWithCurrecntWorkingDirectory()
        {
            List<string> arguments = new();
            dataCollectionLauncher.LaunchDataCollector(null, arguments);

            string currentWorkingDirectory = Directory.GetCurrentDirectory();

            mockProcessHelper.Verify(x => x.LaunchProcess(It.IsAny<string>(), It.IsAny<string>(), currentWorkingDirectory, It.IsAny<IDictionary<string, string>>(), It.IsAny<Action<object, string>>(), It.IsAny<Action<Object>>(), It.IsAny<Action<object, string>>()), Times.Once());
        }
    }
}
