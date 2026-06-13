// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BackslashParameterTestProject;

[TestClass]
public class BackslashTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void ParameterContainsBackslashes()
    {
        // The value passed via CLI run settings should preserve backslashes.
        // On Unix, MSBuild ITaskItem normalization would turn \ into / if the property
        // is typed as string[] (ITaskItem[]). The fix changes it to a plain string.
        var pattern = (string)TestContext.Properties["pattern"]!;
        // On Windows, command-line escaping may double backslashes before quotes,
        // so we just verify backslashes survive (aren't turned into forward slashes).
        Assert.IsFalse(pattern.Contains("/"),
            $"Backslashes were normalized to forward slashes. Got '{pattern}'");
        Assert.IsTrue(pattern.Contains("\\"),
            $"Expected backslashes in the value. Got '{pattern}'");
    }
}
