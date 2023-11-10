// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest2;

[TestClass]
public class UnitTest2
{
    [TestMethod(displayName: "AAAAAAAAAAAAAAAAAAAA")]
    public void TestMethod2()
    {
        throw new System.ArgumentException("hello mister eisn\nzwai policajt");
    }
}
