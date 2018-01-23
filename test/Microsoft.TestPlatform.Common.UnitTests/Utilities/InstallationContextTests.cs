// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Utilities
{
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class InstallationContextTests
    {
        private Mock<IFileHelper> mockFileHelper;
        private InstallationContext installationContext;

        public InstallationContextTests()
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.installationContext = new InstallationContext(this.mockFileHelper.Object);
        }

        [TestMethod]
        public void TryGetVisualStudioDirectoryShouldReturnTrueIfVSIsFound()
        {
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(true);

            Assert.IsTrue(this.installationContext.TryGetVisualStudioDirectory(out string visualStudioDirectory), "VS Install Directory returned false");

            Assert.IsTrue(Directory.Exists(visualStudioDirectory), "VS Install Directory doesn't exist");
        }

        [TestMethod]
        public void TryGetVisualStudioDirectoryShouldReturnFalseIfVSIsNotFound()
        {
            this.mockFileHelper.Setup(m => m.Exists(It.IsAny<string>())).Returns(false);

            Assert.IsFalse(this.installationContext.TryGetVisualStudioDirectory(out string visualStudioDirectory), "VS Install Directory returned true");

            Assert.IsTrue(string.IsNullOrEmpty(visualStudioDirectory), "VS Install Directory is not empty");
        }

        [TestMethod]
        public void GetVisualStudioPathShouldReturnPathToDevenvExecutable()
        {
            var devenvPath = this.installationContext.GetVisualStudioPath(@"C:\temp");

            Assert.AreEqual(@"C:\temp\devenv.exe", devenvPath);
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
            var commonLocations = this.installationContext.GetVisualStudioCommonLocations(@"C:\temp");

            CollectionAssert.AreEquivalent(expectedLocations, commonLocations);
        }
    }
}
