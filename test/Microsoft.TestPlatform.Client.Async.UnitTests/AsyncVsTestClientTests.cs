// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Client.Async.UnitTests;

[TestClass]
public class AsyncVsTestClientTests
{
    [TestMethod]
    public void Constructor_CreatesInstance()
    {
        var client = new AsyncVsTestClient();
        Assert.IsNotNull(client);
    }

    [TestMethod]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var client = new AsyncVsTestClient();
        await client.DisposeAsync();
        await client.DisposeAsync();
    }

    [TestMethod]
    public async Task StartSessionAsync_NullPath_ThrowsArgumentException()
    {
        var client = new AsyncVsTestClient();
        try
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => client.StartSessionAsync(null!));
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task StartSessionAsync_EmptyPath_ThrowsArgumentException()
    {
        var client = new AsyncVsTestClient();
        try
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => client.StartSessionAsync(string.Empty));
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task StartSessionAsync_WhitespacePath_ThrowsArgumentException()
    {
        var client = new AsyncVsTestClient();
        try
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => client.StartSessionAsync("   "));
        }
        finally
        {
            await client.DisposeAsync();
        }
    }
}
