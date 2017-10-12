// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Utilities
{
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

        // public void TryGetVisualStudioDirectoryShouldReturnTrueIfVSIsFound
        // public void TryGetVisualStudioDirectoryShouldReturnFalseIfVSIsNotFound
        // public void GetVisualStudioPathShouldReturnPathToDevenvExecutable
        // public void GetVisualStudioCommonLocationShouldReturnWellKnownLocations
    }
}
