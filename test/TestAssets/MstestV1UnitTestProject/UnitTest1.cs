// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MstestV1UnitTestProject
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;


    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// The passing test.
        /// </summary>
        [Priority(2)]
        [TestMethod]
        public void PassingTest1()
        {
            Assert.AreEqual(2, 2);
        }

        [TestMethod]
        public void PassingTest2()
        {
            Assert.AreEqual(3, 3);
        }

        /// <summary>
        /// The failing test.
        /// </summary>
        [TestCategory("CategoryA")]
        [Priority(3)]
        [TestMethod]
        public void FailingTest1()
        {
            Assert.AreEqual(2, 3);
        }

        [TestMethod]
        public void FailingTest2()
        {
            Assert.AreEqual(2, 4);
        }

        /// <summary>
        /// The skipping test.
        /// </summary>
        [Ignore]
        [TestMethod]
        public void SkippingTest()
        {
        }
        
        /// <summary>
        /// Test for scripts
        /// </summary>
        [TestMethod]
        public void LegacySettingsScriptsTest()
        {
            // Setup script should create a dummyfile in temp directory
            var scriptPath = Path.Combine(Path.GetTempPath() + "ScriptTestingFile.txt");
            Assert.IsTrue(File.Exists(scriptPath));
        }

        /// <summary>
        /// Test for deployment item
        /// </summary>
        [TestMethod]
        public void LegacySettingsDeploymentItemTest()
        {
            // File exists check for deployment item and scripts
            var deploymentFullPath = Path.Combine(Path.GetDirectoryName(typeof(UnitTest1).GetTypeInfo().Assembly.Location), "DeploymentFile.xml");
            Assert.IsTrue(File.Exists(deploymentFullPath));
        }
    }
}
