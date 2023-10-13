// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest2;

[TestClass]
public class UnitTest2
{
    [TestMethod]
    public void TestMethod1()
    {
        throw new System.InvalidOperationException("oh no");
    }

    [TestMethod(displayName: "AAAAAAAAAAAAAAAAAAAA")]
    public void TestMethod2()
    {
        throw new System.ArgumentException("hello mister eisn\nzwai policajt");
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
