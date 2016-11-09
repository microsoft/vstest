// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;

    using Microsoft.TestPlatform.TestUtilities;

    public class AcceptanceTestBase : IntegrationTestBase
    {
        private const string DesktopRunnerFramework = "net46";
        private const string CoreRunnerFramework = "netcoreapp1.0";
        private const string DesktopTargetFramework = "net46";
        private const string CoreTargetFramework = "netcoreapp1.0";

        private const string CoreFrameworkArgValue = ".NETCoreApp,Version=v1.0";
        private const string DesktopFrameworkArgValue = ".NETFramework,Version=v4.6";
        private const string DesktopRunnerTargetRuntime = "win7-x64";
        private const string CoreRunnerTargetRuntime = "";

        protected string FrameworkArgValue => DeriveFrameworkArgValue(this.testEnvironment);

        protected static void SetupRunnerCoreTargetDesktopEnvironment(
            IntegrationTestEnvironment testEnvironment)
        {
            testEnvironment.RunnerFramework = CoreRunnerFramework;
            testEnvironment.TargetFramework = DesktopTargetFramework;
            testEnvironment.TargetRuntime = CoreRunnerTargetRuntime;
        }

        protected static void SetupRunnerDesktopTargetDesktopEnvironment(
            IntegrationTestEnvironment testEnvironment)
        {
            testEnvironment.RunnerFramework = DesktopRunnerFramework;
            testEnvironment.TargetFramework = DesktopTargetFramework;
            testEnvironment.TargetRuntime = DesktopRunnerTargetRuntime;
        }

        protected static string DeriveFrameworkArgValue(IntegrationTestEnvironment testEnvironment)
        {
            string framworkArgValue = string.Empty;
            if (string.Equals(testEnvironment.TargetFramework, CoreTargetFramework, StringComparison.Ordinal))
            {
                framworkArgValue = CoreFrameworkArgValue;
            }
            else if (string.Equals(testEnvironment.TargetFramework, DesktopTargetFramework, StringComparison.Ordinal))
            {
                framworkArgValue = DesktopFrameworkArgValue;
            }

            return framworkArgValue;
        }
    }
}
