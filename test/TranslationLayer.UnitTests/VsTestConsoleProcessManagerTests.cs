// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VsTestConsoleProcessManagerTests
    {
        private const string InheritedVariable = "VSTEST_PROCESS_MANAGER_TEST_VARIABLE";
        private string originalInheritedValue;
        private VsTestConsoleProcessManager processManager;

        [TestInitialize]
        public void TestInitialize()
        {
            this.originalInheritedValue = Environment.GetEnvironmentVariable(InheritedVariable);
            Environment.SetEnvironmentVariable(InheritedVariable, "inherited");
            this.processManager = new VsTestConsoleProcessManager(typeof(VsTestConsoleProcessManagerTests).Assembly.Location, "dotnet");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Environment.SetEnvironmentVariable(InheritedVariable, this.originalInheritedValue);
        }

        [TestMethod]
        public void CreateProcessStartInfoShouldPreserveDefaultInheritance()
        {
            var parameters = new ConsoleParameters();

            var startInfo = this.processManager.CreateProcessStartInfo(parameters);

            Assert.AreEqual("inherited", startInfo.EnvironmentVariables[InheritedVariable]);
            Assert.IsFalse(startInfo.UseShellExecute);
#if NETFRAMEWORK
            Assert.IsNull(parameters.EnvironmentVariables);
#endif
        }

#if NETFRAMEWORK
        [TestMethod]
        public void CreateProcessStartInfoShouldPreserveExplicitEnvironmentReplacementWithoutChangingTheCaller()
        {
            var parameters = new ConsoleParameters
            {
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "CUSTOM_VARIABLE", "custom" },
                    { "REMOVED_VARIABLE", null },
                },
            };

            var startInfo = this.processManager.CreateProcessStartInfo(parameters);

            Assert.IsFalse(startInfo.EnvironmentVariables.ContainsKey(InheritedVariable));
            Assert.IsFalse(startInfo.EnvironmentVariables.ContainsKey("REMOVED_VARIABLE"));
            Assert.AreEqual("custom", startInfo.EnvironmentVariables["CUSTOM_VARIABLE"]);
            Assert.AreEqual(2, parameters.EnvironmentVariables.Count);
        }

        [TestMethod]
        public void CreateProcessStartInfoShouldPreserveAnExplicitlyEmptyEnvironment()
        {
            var parameters = new ConsoleParameters { EnvironmentVariables = new Dictionary<string, string>() };

            var startInfo = this.processManager.CreateProcessStartInfo(parameters);

            Assert.AreEqual(0, startInfo.EnvironmentVariables.Count);
            Assert.AreEqual(0, parameters.EnvironmentVariables.Count);
        }
#endif
    }
}
