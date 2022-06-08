// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETFRAMEWORK

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class DotnetArchitectureTests : AcceptanceTestBase
{
    [TestMethod]
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
    public void DotnetTestProjectLaunching32BitsProcess(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetTestDllForFramework("Net6Launches32BitsProcess.dll", "net6.0");
        InvokeDotnetTest(projectPath);

        ExitCodeEquals(0);
    }
}

#endif
