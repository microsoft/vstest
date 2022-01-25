// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.SmokeTests;

using TestUtilities;

using VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DiscoveryTests : IntegrationTestBase
{
    [TestMethod]
    public void DiscoverAllTests()
    {
        InvokeVsTestForDiscovery(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty, ".NETFramework,Version=v4.5.1");
        var listOfTests = new string[] { "SampleUnitTestProject.UnitTest1.PassingTest", "SampleUnitTestProject.UnitTest1.FailingTest", "SampleUnitTestProject.UnitTest1.SkippingTest" };
        ValidateDiscoveredTests(listOfTests);
    }
}