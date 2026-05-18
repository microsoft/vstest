// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThreadingTasksExtensionsTestProject;

[TestClass]
public class ValueTaskTests
{
    [TestMethod]
    public async Task TestUsingValueTask()
    {
        // This test exercises System.Threading.Tasks.Extensions (ValueTask<T>)
        // on net462. Without the binding redirect and DLL in the testhost,
        // this would throw FileLoadException at runtime.
        var result = await GetValueAsync();
        Assert.AreEqual(42, result);
    }

    private static ValueTask<int> GetValueAsync()
    {
        return new ValueTask<int>(42);
    }
}
