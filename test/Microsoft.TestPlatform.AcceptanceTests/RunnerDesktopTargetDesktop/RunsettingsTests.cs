// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.RunnerDesktopTargetDesktop
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RunsettingsTests : AcceptanceTests.RunsettingsTests
    {
        [TestInitialize]
        public void SetupEnvironment()
        {
            AcceptanceTestBase.SetupRunnerDesktopTargetDesktopEnvironment(this.testEnvironment);
        }
    }
}
