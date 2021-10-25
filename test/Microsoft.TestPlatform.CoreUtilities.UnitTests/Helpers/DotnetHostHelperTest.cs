// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Helpers
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class DotnetHostHelperTest
    {
        [DataTestMethod]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X86, PlatformOperatingSystem.OSX, "DOTNET_ROOT_X64", false)]
        public void GetDotnetPathByArchitecture_EnvVars(PlatformArchitecture targetArchitecture,
            PlatformArchitecture platformArchitecture,
            PlatformOperatingSystem platformSystem,
            string envVar,
            bool throwNotFound)
        {
            string expectedMuxerLocation = platformSystem == PlatformOperatingSystem.OSX ? "/tmp/dotnet" : @"c:\dotnet.exe";
            Mock<IFileHelper> fileHelper = new Mock<IFileHelper>();
            Mock<IProcessHelper> processHelper = new Mock<IProcessHelper>();
            Mock<IEnvironment> environmentHelper = new Mock<IEnvironment>();
            environmentHelper.SetupGet(x => x.Architecture).Returns(platformArchitecture);
            environmentHelper.SetupGet(x => x.OperatingSystem).Returns(platformSystem);
            Mock<IWindowsRegistryHelper> windowsRegistrytHelper = new Mock<IWindowsRegistryHelper>();
            Mock<IEnvironmentVariableHelper> environmentVariableHelper = new Mock<IEnvironmentVariableHelper>();
            environmentVariableHelper.Setup(x => x.GetEnvironmentVariable(envVar)).Returns(expectedMuxerLocation);

            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
        }
    }
}
