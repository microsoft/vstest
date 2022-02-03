// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace MSTest1;

[TestClass]
public class UnitTest1
{
    public TestContext TestContext { get; set; }



    [TestMethod]
    public void TestMethod1()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod2()
    {
        Thread.Sleep(2000);
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod3()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod4()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod5()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod6()
    {
        TestContext.WriteLine("io");
    }

    [TestMethod]
    public void TestMethod7()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod8()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod9()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod10()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod11()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod12()
    {
        TestContext.WriteLine("io");
    }

    [TestMethod]
    public void TestMethod13()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod14()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod15()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod16()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod17()
    {
        TestContext.WriteLine("io");
    }
    [TestMethod]
    public void TestMethod18()
    {
        TestContext.WriteLine("io");
    }
}
