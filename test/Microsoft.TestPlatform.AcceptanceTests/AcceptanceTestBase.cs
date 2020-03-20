// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;

    using Microsoft.TestPlatform.TestUtilities;

    public class AcceptanceTestBase : IntegrationTestBase
    {
        public const string DesktopTargetFramework = "net451";
        public const string Core21TargetFramework = "netcoreapp2.1";
        public const string Core31TargetFramework = "netcoreapp3.1";

        public const string Core21FrameworkArgValue = ".NETCoreApp,Version=v2.1";
        public const string Core31FrameworkArgValue = ".NETCoreApp,Version=v3.1";
        public const string DesktopFrameworkArgValue = ".NETFramework,Version=v4.5.1";
        public const string DesktopRunnerTargetRuntime = "win7-x64";
        public const string CoreRunnerTargetRuntime = "";
        public const string InIsolation = "/InIsolation";

        protected string FrameworkArgValue => DeriveFrameworkArgValue(this.testEnvironment);

        protected static void SetTestEnvironment(IntegrationTestEnvironment testEnvironment, RunnerInfo runnerInfo)
        {
            testEnvironment.RunnerFramework = runnerInfo.RunnerFramework;
            testEnvironment.TargetFramework = runnerInfo.TargetFramework;
            testEnvironment.InIsolationValue = runnerInfo.InIsolationValue;
        }

        protected static string DeriveFrameworkArgValue(IntegrationTestEnvironment testEnvironment)
        {
            switch (testEnvironment.TargetFramework)
            {
                case Core21TargetFramework:
                    return Core21FrameworkArgValue;
                case Core31TargetFramework:
                    return Core31FrameworkArgValue;
                case DesktopTargetFramework:
                    return DesktopFrameworkArgValue;
                default:
                    throw new NotSupportedException($"{testEnvironment.TargetFramework} is not supported TargetFramework value.");
            }
        }

        protected bool IsDesktopTargetFramework()
        {
            return this.testEnvironment.TargetFramework == AcceptanceTestBase.DesktopTargetFramework;
        }

        protected string GetTargetFramworkForRunsettings()
        {
            var targetFramework = string.Empty;
            if (this.testEnvironment.TargetFramework == DesktopTargetFramework)
            {
                targetFramework = "Framework45";
            }
            else
            {
                targetFramework = "FrameworkCore10";
            }

            return targetFramework;
        }

        /// <summary>
        /// Default RunSettings
        /// </summary>
        /// <returns></returns>
        public string GetDefaultRunSettings()
        {
            string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                        </RunConfiguration>
                                    </RunSettings>";
            return runSettingsXml;
        }
    }
}
