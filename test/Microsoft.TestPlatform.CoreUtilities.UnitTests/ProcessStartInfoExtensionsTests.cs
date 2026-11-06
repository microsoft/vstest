// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests;

[TestClass]
public class ProcessStartInfoExtensionsTests
{
    private const string Variable = "VSTEST_TEST_ENVIRONMENT_VARIABLE";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void NullValueShouldRemoveEnvironmentVariable(bool initiallyPresent)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.EnvironmentVariables.Remove(Variable);
        if (initiallyPresent)
        {
            startInfo.EnvironmentVariables[Variable] = "previous-value";
        }

        startInfo.AddEnvironmentVariable(Variable, null);

        Assert.IsFalse(startInfo.EnvironmentVariables.ContainsKey(Variable));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("replacement-value")]
    public void NonNullValueShouldSetEnvironmentVariable(string value)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.EnvironmentVariables[Variable] = "previous-value";

        startInfo.AddEnvironmentVariable(Variable, value);

        Assert.IsTrue(startInfo.EnvironmentVariables.ContainsKey(Variable));
        Assert.AreEqual(value, startInfo.EnvironmentVariables[Variable]);
    }
}
