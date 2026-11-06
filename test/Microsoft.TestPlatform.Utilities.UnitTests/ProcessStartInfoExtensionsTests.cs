// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.Tests
{
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProcessStartInfoExtensionsTests
    {
        private const string Variable = "VSTEST_PROCESS_START_INFO_TEST_VARIABLE";

        [TestMethod]
        public void AddEnvironmentVariableShouldRemoveAnEntryWhenTheValueIsNull()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.EnvironmentVariables[Variable] = "inherited";

            startInfo.AddEnvironmentVariable(Variable, null);

            Assert.IsFalse(startInfo.EnvironmentVariables.ContainsKey(Variable));
        }

        [TestMethod]
        public void AddEnvironmentVariableShouldAllowRemovingAnAbsentEntry()
        {
            var startInfo = new ProcessStartInfo();
            startInfo.EnvironmentVariables.Remove(Variable);

            startInfo.AddEnvironmentVariable(Variable, null);

            Assert.IsFalse(startInfo.EnvironmentVariables.ContainsKey(Variable));
        }

        [TestMethod]
        [DataRow("replacement")]
        [DataRow("")]
        public void AddEnvironmentVariableShouldPreserveNonNullValues(string value)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.EnvironmentVariables[Variable] = "old";

            startInfo.AddEnvironmentVariable(Variable, value);

            Assert.IsTrue(startInfo.EnvironmentVariables.ContainsKey(Variable));
            Assert.AreEqual(value, startInfo.EnvironmentVariables[Variable]);
        }
    }
}
