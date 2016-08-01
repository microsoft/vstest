// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.Utilities
{
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    [TestClass]
    public class PathUtilitiesTests
    {
        private PathUtilities pathUtilities;

        [TestInitialize]
        public void TestInit()
        {
            this.pathUtilities = new PathUtilities();
        }

        [TestMethod]
        public void GetUniqueValidPathsShouldReturnEmptyWhenPathsIsNull()
        {
            var pathSet = this.pathUtilities.GetUniqueValidPaths(null);

            Assert.IsNotNull(pathSet);
            Assert.AreEqual(0, pathSet.Count);
        }

        [TestMethod]
        public void GetUniqueValidPathsShouldReturnEmptyWhenPathsIsEmpty()
        {
            var pathSet = this.pathUtilities.GetUniqueValidPaths(new List<string> { });

            Assert.IsNotNull(pathSet);
            Assert.AreEqual(0, pathSet.Count);
        }

        [TestMethod]
        public void GetUniqueValidPathsShouldReturnExtensionPaths()
        {
            var extensionsList = new List<string>
                                     {
                                         typeof(PathUtilitiesTests).GetTypeInfo().Assembly.Location,
                                         typeof(PathUtilities).GetTypeInfo().Assembly.Location
                                     };

            var pathSet =
                this.pathUtilities.GetUniqueValidPaths(extensionsList);

            Assert.IsNotNull(pathSet);
            CollectionAssert.AreEqual(extensionsList, pathSet.ToList());
        }

        [TestMethod]
        public void GetUniqueValidPathsShouldReturnUniquePaths()
        {
            var extensionsList = new List<string>
                                     {
                                         typeof(PathUtilitiesTests).GetTypeInfo().Assembly.Location,
                                         typeof(PathUtilitiesTests).GetTypeInfo().Assembly.Location
                                     };

            var pathSet =
                this.pathUtilities.GetUniqueValidPaths(extensionsList);

            Assert.IsNotNull(pathSet);
            CollectionAssert.AreEqual(new List<string> { extensionsList[0] }, pathSet.ToList());
        }

        [TestMethod]
        public void GetUniqueValidPathsShouldReturnValidPaths()
        {
            var extensionsList = new List<string>
                                     {
                                         typeof(PathUtilitiesTests).GetTypeInfo().Assembly.Location,
                                         "foo.dll",
                                         "fakeextension.dll"
                                     };

            var pathSet =
                this.pathUtilities.GetUniqueValidPaths(extensionsList);

            Assert.IsNotNull(pathSet);
            CollectionAssert.AreEqual(new List<string> { extensionsList[0] }, pathSet.ToList());
        }
    }
}
