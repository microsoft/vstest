// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AdapterUtilities.ManagedNameUtilities.UnitTests;

[TestClass]
public class ManagedNameHierarchyTests
{
    [TestMethod]
    public void GetManagedNameShouldReturnTwoHierarchyValuesFromThreeOffsets()
    {
        var method = typeof(ManagedNameHierarchyTests).GetMethod(nameof(GetManagedNameShouldReturnTwoHierarchyValuesFromThreeOffsets))!;

        ManagedNameHelper.GetManagedName(method, out var typeName, out var methodName, out var hierarchy);

        Assert.AreEqual(typeof(ManagedNameHierarchyTests).FullName, typeName);
        Assert.AreEqual(method.Name, methodName);
        Assert.AreEqual(HierarchyConstants.Levels.TotalLevelCount, hierarchy.Length);
        CollectionAssert.AreEqual(
            new[] { typeof(ManagedNameHierarchyTests).Namespace!, nameof(ManagedNameHierarchyTests) },
            hierarchy);
    }
}
