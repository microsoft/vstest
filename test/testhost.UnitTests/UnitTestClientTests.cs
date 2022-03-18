﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.TestExecutor.Tests;

[TestClass]
public class UnitTestClientTests
{
    [TestMethod]
    public void SplitArgumentsShouldHonorDoubleQuotes()
    {
        var expected = new string[] { "--port", "8080", "--endpoint", "127.0.0.1:8020", "--diag", "\"abc txt\"" };
        var argument = "--port 8080 --endpoint 127.0.0.1:8020 --diag \"abc txt\"";
        string[] argsArr = UnitTestClient.SplitArguments(argument);

        Assert.AreEqual(6, argsArr.Length);
        CollectionAssert.AreEqual(argsArr, expected);
    }

    [TestMethod]
    public void SplitArgumentsShouldHonorSingleQuotes()
    {
        var expected = new string[] { "--port", "8080", "--endpoint", "127.0.0.1:8020", "--diag", "\'abc txt\'" };
        var argument = "--port 8080 --endpoint 127.0.0.1:8020 --diag \'abc txt\'";
        string[] argsArr = UnitTestClient.SplitArguments(argument);

        Assert.AreEqual(6, argsArr.Length);
        CollectionAssert.AreEqual(expected, argsArr);
    }

    [TestMethod]
    public void SplitArgumentsShouldSplitAtSpacesOutsideOfQuotes()
    {
        var expected = new string[] { "--port", "8080", "--endpoint", "127.0.0.1:8020", "--diag", "abc", "txt" };
        var argument = "--port 8080 --endpoint 127.0.0.1:8020 --diag abc txt";
        string[] argsArr = UnitTestClient.SplitArguments(argument);

        Assert.AreEqual(7, argsArr.Length);
        CollectionAssert.AreEqual(expected, argsArr);
    }
}
