// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest1;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    [SleeperData]
    public void TestMethod1(int i)
    {
        var _ = i;
        // Thread.Sleep(1000);
    }

    [TestMethod]
    public void TestMethod2()
    {
        // Thread.Sleep(1000);
    }

    [TestMethod]
    public void TestMethod3()
    {
        // Thread.Sleep(1000);
    }

    [TestMethod]
    public void TestMethod4()
    {
        // Thread.Sleep(1000);
    }

    [TestMethod]
    public void TestMethod5()
    {
        // Thread.Sleep(1000);
    }

    [TestMethod]
    public void TestMethod6()
    {
        // Thread.Sleep(1000);
    }

    [TestMethod]
    public void TestMethod7()
    {
        // Thread.Sleep(1000);
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal class SleeperDataAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        Thread.Sleep(3000);
        return [
            [1]
            ];
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        return string.Join(", ", data?.Select(d => d?.ToString() ?? "<null>") ?? ["<null>"]);
    }
}
