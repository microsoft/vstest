// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest1;

[TestClass]
public class UnitTest1
{
    [AssemblyInitialize]
    public static void Setup(TestContext _)
    {
        Debug.WriteLine("Setup");
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
        Debug.WriteLine("Cleanup");
    }


    [TestMethod]
    public void TestMethod1()
    {
        Debug.WriteLine("TestMethod1");
    }

    //[TestMethod]
    //public void TestMethod2()
    //{
    //    Debug.WriteLine("TestMethod2");
    //}
}
