// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Hosting
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestRunnerConnectionInfoExtensionsTests
    {
        [TestMethod]
        public void ToCommandLineOptionsShouldIncludePort()
        {
            var connectionInfo = new TestRunnerConnectionInfo { Port = 123 };

            var options = connectionInfo.ToCommandLineOptions();

            StringAssert.StartsWith(options, "--port 123");
        }

        [TestMethod]
        public void ToCommandLineOptionsShouldIncludeParentProcessId()
        {
            var connectionInfo = new TestRunnerConnectionInfo { RunnerProcessId = 123 };

            var options = connectionInfo.ToCommandLineOptions();

            Assert.IsTrue(options.IndexOf("--parentprocessid 123", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [TestMethod]
        public void ToCommandLineOptionsShouldIncludeDiagnosticsOptionIfEnabled()
        {
            var connectionInfo = new TestRunnerConnectionInfo();

            var options = connectionInfo.ToCommandLineOptions();

            Assert.IsFalse(options.IndexOf("--diag", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [TestMethod]
        public void ToCommandLineOptionsShouldNotIncludeDiagnosticsOptionIfNotEnabled()
        {
            var connectionInfo = new TestRunnerConnectionInfo { LogFile = "log.txt" };

            var options = connectionInfo.ToCommandLineOptions();

            StringAssert.EndsWith(options, "--diag log.txt");
        }
    }
}