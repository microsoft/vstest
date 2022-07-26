// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.Common.UnitTests.Utilities;

[TestClass]
public class AssemblyPropertiesTests : IntegrationTestBase
{
    private readonly IAssemblyProperties _assemblyProperties;

    public AssemblyPropertiesTests()
    {
        _assemblyProperties = new AssemblyProperties();
    }

    [TestMethod]
    [DataRow("net462")]
    [DataRow("netcoreapp3.1")]
    public void GetAssemblyTypeForManagedDll(string framework)
    {
        var assemblyPath = _testEnvironment.GetTestAsset("SimpleTestProject3.dll", framework);
        var assemblyType = _assemblyProperties.GetAssemblyType(assemblyPath);

        Assert.AreEqual(AssemblyType.Managed, assemblyType);
    }

    [TestMethod]
    public void GetAssemblyTypeForNativeDll()
    {
        var assemblyPath = $@"{_testEnvironment.PackageDirectory}\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
        var assemblyType = _assemblyProperties.GetAssemblyType(assemblyPath);

        Assert.AreEqual(AssemblyType.Native, assemblyType);
    }

    [TestMethod]
    public void GetAssemblyTypeForManagedExe()
    {
        var assemblyPath = _testEnvironment.GetTestAsset("ConsoleManagedApp.exe", "net462");
        var assemblyType = _assemblyProperties.GetAssemblyType(assemblyPath);

        Assert.AreEqual(AssemblyType.Managed, assemblyType);
    }

    [TestMethod]
    [DataRow("netcoreapp3.1")]
    public void GetAssemblyTypeForNetCoreManagedExe(string framework)
    {
        var assemblyPath = _testEnvironment.GetTestAsset("ConsoleManagedApp.dll", framework);
        var assemblyType = _assemblyProperties.GetAssemblyType(assemblyPath);

        Assert.AreEqual(AssemblyType.Managed, assemblyType);
    }

    [TestMethod]
    public void GetAssemblyTypeForNativeExe()
    {
        var assemblyPath = $@"{_testEnvironment.PackageDirectory}\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.ConsoleNativeApp.exe";
        var assemblyType = _assemblyProperties.GetAssemblyType(assemblyPath);

        Assert.AreEqual(AssemblyType.Native, assemblyType);
    }

    [TestMethod]
    public void GetAssemblyTypeShouldReturnNoneInCaseOfError()
    {
        var assemblyType = _assemblyProperties.GetAssemblyType("invalidFile.dll");

        Assert.AreEqual(AssemblyType.None, assemblyType);
    }
}
