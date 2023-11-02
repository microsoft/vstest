// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

namespace MSTest2;

[TestClass]
public class UnitTest2
{
    [TestMethod]
    public void TestMethod1()
    {
        Thread.Sleep(10_000);
    }
}
