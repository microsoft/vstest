// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.RunnerCoreTargetCore
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiscoveryTests : AcceptanceTests.DiscoveryTests
    {
        [TestInitialize]
        public void SetupEnvironment()
        {
            AcceptanceTestBase.SetupRunnerCoreTargetCoreEnvironment(this.testEnvironment);
        }
    }
}
