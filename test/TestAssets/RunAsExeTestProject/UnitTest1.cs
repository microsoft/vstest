// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Opt this test assembly into running as its own executable instead of being hosted by testhost.exe.
[assembly: RunAsExe]

namespace RunAsExeTestProject;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void PassingTest()
    {
        Assert.IsTrue(Environment.ProcessorCount > 0);
    }

    [TestMethod]
    public void FailingTest()
    {
        Assert.Fail("Intentionally failing test.");
    }
}
