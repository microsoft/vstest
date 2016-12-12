// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.RunnerDesktopTargetCore
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AppDomainTests : AcceptanceTests.AppDomainTests
    {
        [TestInitialize]
        public void SetupEnvironment()
        {
            AcceptanceTestBase.SetupRunnerDesktopTargetCoreEnvironment(this.testEnvironment);
        }

        // Disable app domain doesn't support in dotnet core
        [Ignore]
        [TestMethod]
        public override void RunTestExecutionWithDisableAppDomain()
        {

        }
    }
}
