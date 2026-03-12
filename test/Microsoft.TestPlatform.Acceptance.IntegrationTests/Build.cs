// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Acceptance.IntegrationTests;

[TestClass]
public class Build : IntegrationTestBase
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        IntegrationTestBuild.BuildTestAssetsForIntegrationTests(testContext);
    }
}
