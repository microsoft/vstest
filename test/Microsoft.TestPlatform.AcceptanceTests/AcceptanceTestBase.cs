// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;

    using Microsoft.TestPlatform.TestUtilities;

    public class AcceptanceTestBase : IntegrationTestBase
    {
        public const string DesktopTargetFramework = "net451";
        public const string CoreTargetFramework = "netcoreapp1.0";
        public const string Core11TargetFramework = "netcoreapp1.1";
        public const string Core20TargetFramework = "netcoreapp2.0";

        public const string CoreFrameworkArgValue = ".NETCoreApp,Version=v1.0";
        public const string Core11FrameworkArgValue = ".NETCoreApp,Version=v1.1";
        public const string Core20FrameworkArgValue = ".NETCoreApp,Version=v2.0";
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
            string framworkArgValue = string.Empty;
            if (string.Equals(testEnvironment.TargetFramework, CoreTargetFramework, StringComparison.Ordinal))
            {
                framworkArgValue = CoreFrameworkArgValue;
            }
            else if (string.Equals(testEnvironment.TargetFramework, Core11TargetFramework, StringComparison.Ordinal))
            {
                framworkArgValue = Core11FrameworkArgValue;
            }
            else if (string.Equals(testEnvironment.TargetFramework, Core20TargetFramework, StringComparison.Ordinal))
            {
                framworkArgValue = Core20FrameworkArgValue;
            }
            else if (string.Equals(testEnvironment.TargetFramework, DesktopTargetFramework, StringComparison.Ordinal))
            {
                framworkArgValue = DesktopFrameworkArgValue;
            }

            return framworkArgValue;
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

        protected string GetTestHostProcessName(string targetPlatform)
        {
            var testHostProcessName = string.Empty;
            if (this.IsDesktopTargetFramework())
            {
                if (string.Equals(targetPlatform, "x86", StringComparison.OrdinalIgnoreCase))
                {
                    testHostProcessName = "testhost.x86";
                }
                else
                {
                    testHostProcessName = "testhost";
                }
            }
            else
            {
                testHostProcessName = "dotnet";
            }

            return testHostProcessName;
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
