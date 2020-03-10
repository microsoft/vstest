// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.SettingsMigrator.UnitTests
{
    using System;
    using System.Globalization;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PathResolverTests
    {
        private PathResolver pathResolver;

        [TestInitialize]
        public void TestInit()
        {
            this.pathResolver = new PathResolver();
        }

        [TestMethod]
        public void PathResolverShouldReturnNullForEmptyArguments()
        {
            var newFilePath = this.pathResolver.GetTargetPath(new string[] { });
            Assert.IsNull(newFilePath, "Empty arguments should return null");
        }

        [TestMethod]
        public void PathResolverShouldReturnNullForInvalidArguments()
        {
            var newFilePath = this.pathResolver.GetTargetPath(new string[] { "asd", "asd", "asd" });
            Assert.IsNull(newFilePath, "Invalid arguments should return null");
        }

        [TestMethod]
        public void PathResolverShouldReturnNullForRelativePaths()
        {
            var newFilePath = this.pathResolver.GetTargetPath(new string[] { "asd.testsettings" });
            Assert.IsNull(newFilePath, "Relative paths should return null");
        }

        [TestMethod]
        public void PathResolverShouldReturnNullForRelativePathsWithTwoArguments()
        {
            var newFilePath = this.pathResolver.GetTargetPath(new string[] { "asd.Testsettings", "C:\\asd.runsettings" });
            Assert.IsNull(newFilePath, "Relative paths should return null");
        }

        [TestMethod]
        public void PathResolverShouldNotReturnNullForPathsWithExtensionInCapitals()
        {
            var newFilePath = this.pathResolver.GetTargetPath(new string[] { "C:\\asd.TestSEettings", "C:\\asd.RuNSettings" });
            Assert.IsNotNull(newFilePath, "Relative paths should not return null");
        }

        [TestMethod]
        public void PathResolverShouldReturnNullForRelativePathsForRunsettings()
        {
            var newFilePath = this.pathResolver.GetTargetPath(new string[] { "C:\\asd.testsettings", "asd.runsettings" });
            Assert.IsNull(newFilePath, "Relative paths should return null");
        }

        [TestMethod]
        public void PathResolverShouldReturnRunsettingsPathOfSameLocationAsTestSettings()
        {
            var newFilePath = this.pathResolver.GetTargetPath(new string[] { "C:\\asd.testsettings" });
            Assert.IsNotNull(newFilePath, "File path should not be null.");
            Assert.IsTrue(string.Equals(Path.GetExtension(newFilePath), ".runsettings"), "File path should be .runsettings");
            Assert.IsTrue(newFilePath.Contains("C:\\asd_"), "File should be of same name as testsettings");
            var time = newFilePath.Substring(7, 19);
            Assert.IsTrue(DateTime.TryParseExact(time, "MM-dd-yyyy_hh-mm-ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime date), "File name should have datetime");
        }
    }
}