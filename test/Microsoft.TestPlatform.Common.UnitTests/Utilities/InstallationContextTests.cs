// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Utilities
{
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.Linq;

    [TestClass]
    public class InstallationContextTests
    {
        private readonly Mock<IFileHelper> mockFileHelper;
        private readonly InstallationContext installationContext;

        public InstallationContextTests()
        {
            mockFileHelper = new Mock<IFileHelper>();
            installationContext = new InstallationContext(mockFileHelper.Object);
        }

        [TestMethod]
        public void TryGetVisualStudioDirectoryShouldReturnTrueIfVSIsFound()
        {
            mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);

            Assert.IsTrue(installationContext.TryGetVisualStudioDirectory(out string visualStudioDirectory), "VS Install Directory returned false");

            Assert.IsTrue(Directory.Exists(visualStudioDirectory), "VS Install Directory doesn't exist");
        }

        [TestMethod]
        public void TryGetVisualStudioDirectoryShouldReturnFalseIfVSIsNotFound()
        {
            mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(false);

            Assert.IsFalse(installationContext.TryGetVisualStudioDirectory(out string visualStudioDirectory), "VS Install Directory returned true");

            Assert.IsTrue(string.IsNullOrEmpty(visualStudioDirectory), "VS Install Directory is not empty");
        }

        [TestMethod]
        public void GetVisualStudioPathShouldReturnPathToDevenvExecutable()
        {
            var devenvPath = installationContext.GetVisualStudioPath(@"C:\temp");

            Assert.AreEqual(@"C:\temp\devenv.exe", devenvPath.Replace("/", "\\"));
        }

        [TestMethod]
        public void GetVisualStudioCommonLocationShouldReturnWellKnownLocations()
        {
            var expectedLocations = new[]
            {
                @"C:\temp\PrivateAssemblies",
                @"C:\temp\PublicAssemblies",
                @"C:\temp\CommonExtensions\Microsoft\TestWindow",
                @"C:\temp\CommonExtensions\Microsoft\TeamFoundation\Team Explorer",
                @"C:\temp"
            };
            var commonLocations = installationContext.GetVisualStudioCommonLocations(@"C:\temp").Select(p => p.Replace("/", "\\")).ToArray();

            CollectionAssert.AreEquivalent(expectedLocations, commonLocations);
        }
    }
}
