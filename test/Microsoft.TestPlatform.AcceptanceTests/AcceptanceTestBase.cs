// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.TestPlatform.TestUtilities;

    public class AcceptanceTestBase : IntegrationTestBase
    {
        public const string Net451TargetFramework = "net451";
        public const string Net452TargetFramework = "net452";
        public const string Net46TargetFramework = "net46";
        public const string Net461TargetFramework = "net461";
        public const string Net462TargetFramework = "net462";
        public const string Net47TargetFramework = "net47";
        public const string Net471TargetFramework = "net471";
        public const string Net472TargetFramework = "net472";
        public const string Net48TargetFramework = "net48";
        public const string DesktopTargetFramework = "net451";
        public const string Core21TargetFramework = "netcoreapp2.1";
        public const string Core31TargetFramework = "netcoreapp3.1";
        public const string Core50TargetFramework = "net5.0";

        public const string DesktopFrameworkArgValue = ".NETFramework,Version=v4.5.1";
        public const string Net451FrameworkArgValue = ".NETFramework,Version=v4.5.1";
        public const string Net452FrameworkArgValue = ".NETFramework,Version=v4.5.2";
        public const string Net46FrameworkArgValue = ".NETFramework,Version=v4.6";
        public const string Net461FrameworkArgValue = ".NETFramework,Version=v4.6.1";
        public const string Net462FrameworkArgValue = ".NETFramework,Version=v4.6.2";
        public const string Net47FrameworkArgValue = ".NETFramework,Version=v4.7";
        public const string Net471FrameworkArgValue = ".NETFramework,Version=v4.7.1";
        public const string Net472FrameworkArgValue = ".NETFramework,Version=v4.7.2";
        public const string Net48FrameworkArgValue = ".NETFramework,Version=v4.8";

        public const string Core21FrameworkArgValue = ".NETCoreApp,Version=v2.1";
        public const string Core31FrameworkArgValue = ".NETCoreApp,Version=v3.1";
        public const string Core50FrameworkArgValue = ".NETCoreApp,Version=v5.0";

        public const string DesktopRunnerTargetRuntime = "win7-x64";
        public const string CoreRunnerTargetRuntime = "";
        public const string InIsolation = "/InIsolation";

        public const string NETFX452_48 = "net452;net461;net472;net48";
        public const string NETFX451_48 = "net452;net461;net472;net48";
        public const string NETCORE21_50 = "netcoreapp2.1;netcoreapp3.1;net5.0";
        public const string NETFX452_NET50 = "net452;net461;net472;net48;netcoreapp2.1;netcoreapp3.1;net5.0";
        public const string NETFX452_NET31 = "net452;net461;net472;net48;netcoreapp2.1;netcoreapp3.1";

        public static string And(string left, string right)
        {
            return string.Join(";", left, right);
        }

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
                case Core50TargetFramework:
                    return Core50FrameworkArgValue;
                case Net451TargetFramework:
                    return Net451FrameworkArgValue;
                case Net452TargetFramework:
                    return Net452FrameworkArgValue;
                case Net46TargetFramework:
                    return Net46FrameworkArgValue;
                case Net461TargetFramework:
                    return Net461FrameworkArgValue;
                case Net462TargetFramework:
                    return Net462FrameworkArgValue;
                case Net47TargetFramework:
                    return Net47FrameworkArgValue;
                case Net471TargetFramework:
                    return Net471FrameworkArgValue;
                case Net472TargetFramework:
                    return Net472FrameworkArgValue;
                case Net48TargetFramework:
                    return Net48FrameworkArgValue;
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
