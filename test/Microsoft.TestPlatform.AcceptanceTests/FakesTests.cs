// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FakesTests : AcceptanceTestBase
    {
        public FakesTests()
        {
            this.testEnvironment.portableRunner = true;
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void RunSimpleFakeTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Fakes not supported for .NET core runner");
            }
            var platforms = new string[] {"", "x64"};
            var configs = new string[] {"debug", "release"};
            foreach (var platform in platforms)
            {
                foreach (var config in configs)
                {
                    string assemblyRelativePathFormat =
                        @"microsoft.testPlatform.testassets.fakes\1.0.0\contentFiles\any\any\{0}\{1}\FakesTestProject.dll";
                    var assemblyRelativePath = platform.Equals("x64", StringComparison.OrdinalIgnoreCase)
                        ? string.Format(assemblyRelativePathFormat, platform, config)
                        : string.Format(assemblyRelativePathFormat, "", config);
                    var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);

                    this.InvokeVsTest(assemblyAbsolutePath);
                    this.ValidateSummaryStatus(1, 0, 0);
                }
            }
        }
    }
}
