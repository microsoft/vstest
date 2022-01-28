// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BlameUnitTestProject;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
    }

    [TestMethod]
    public void TestMethod2()
    {
        Environment.FailFast("Crash the process on purpose.");
    }
}
