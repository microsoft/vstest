// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceDataCollector.UnitTests
{
    using System.IO;
    using System.Reflection;
    using TestTools.UnitTesting;
    using TraceCollector;

    [TestClass]
    public class VanguardLocationProviderTests
    {
        private VanguardLocationProvider vanguardLocationProvider;

        public VanguardLocationProviderTests()
        {
            this.vanguardLocationProvider = new VanguardLocationProvider();
        }

        [TestMethod]
        public void GetVanguardPathShouldReturnRightPath()
        {
            var actualPath = this.vanguardLocationProvider.GetVanguardPath();
            var expectedPath = this.GetVanguardPath();

            Assert.AreEqual(expectedPath, actualPath);
        }

        [TestMethod]
        public void GetVanguardDirectoryShouldReturnRightDirectory()
        {
            var actualDir = this.vanguardLocationProvider.GetVanguardDirectory();
            var expectedDir = this.GetVanguardDirectory();

            Assert.AreEqual(expectedDir, actualDir);
        }

        private string GetVanguardPath()
        {
            var vanguardDir = this.GetVanguardDirectory();
            return Path.Combine(vanguardDir, "CodeCoverage.exe");
        }

        private string GetVanguardDirectory()
        {
            var currentAssemblyLocation =
                Path.GetDirectoryName(typeof(VanguardLocationProviderTests).GetTypeInfo().Assembly.Location);
            return Path.Combine(currentAssemblyLocation, "CodeCoverage");
        }
    }
}
