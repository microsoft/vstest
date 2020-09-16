// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class SelfContainedAppTests : AcceptanceTestBase
    {
        [TestMethod]
        // this is core 3.1 only, full framework and netcoreapp2.1 don't "publish" automatically during build
        // but if you run it on 2.1 it will pass because we execute the test normally
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false, useNetCore21Target: false, useNetCore31Target: true)]
        public void RunningApplicationThatIsBuiltAsSelfContainedWillNotFailToFindHostpolicyDll(RunnerInfo runnerInfo)
        {
            // when the application is self-contained which is dictated by the RuntimeIdentifier and OutputType project
            // properties, the testhost.exe executable is given a runtimeconfig that instructs it to find a hostpolicy.dll and hostfxr.dll next to it
            // that will fail if we run the testhost.exe from the .nuget location, but will work when we run it from the output folder
            // see https://github.com/dotnet/runtime/issues/3569#issuecomment-595820524 and below for description of how it works
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            // the app is published to win10-x64 because of the runtime identifier in the project
            var assemblyPath = this.BuildMultipleAssemblyPath(@"win10-x64\SelfContainedAppTestProject.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPath, null, null, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(passedTestsCount: 1, 0, 0);
        }
    }
}