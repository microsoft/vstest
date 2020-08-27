// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class for target Framework for the test container
    /// </summary>
    public class Framework
    {
        #region Generated code
        // Generated from Nuget.Frameworks 5.7.0.7 nuget package, 
        // you can update it by scripts/generate/update-supported-nuget.frameworks-versions.ps1
        internal static Dictionary<string, Framework> mapping = new Dictionary<string, Framework>
        {
            [".NETFramework,Version=v1.1"] = new Framework
            {
                Name = ".NETFramework,Version=v1.1",
                FrameworkName = ".NETFramework",
                Version = "1.1.0.0",
                ShortName = "net11",
            },
            [".NETFramework,Version=v2.0"] = new Framework
            {
                Name = ".NETFramework,Version=v2.0",
                FrameworkName = ".NETFramework",
                Version = "2.0.0.0",
                ShortName = "net20",
            },
            [".NETFramework,Version=v3.5"] = new Framework
            {
                Name = ".NETFramework,Version=v3.5",
                FrameworkName = ".NETFramework",
                Version = "3.5.0.0",
                ShortName = "net35",
            },
            [".NETFramework,Version=v4.0"] = new Framework
            {
                Name = ".NETFramework,Version=v4.0",
                FrameworkName = ".NETFramework",
                Version = "4.0.0.0",
                ShortName = "net40",
            },
            [".NETFramework,Version=v4.0.3"] = new Framework
            {
                Name = ".NETFramework,Version=v4.0.3",
                FrameworkName = ".NETFramework",
                Version = "4.0.3.0",
                ShortName = "net403",
            },
            [".NETFramework,Version=v4.5"] = new Framework
            {
                Name = ".NETFramework,Version=v4.5",
                FrameworkName = ".NETFramework",
                Version = "4.5.0.0",
                ShortName = "net45",
            },
            [".NETFramework,Version=v4.5.1"] = new Framework
            {
                Name = ".NETFramework,Version=v4.5.1",
                FrameworkName = ".NETFramework",
                Version = "4.5.1.0",
                ShortName = "net451",
            },
            [".NETFramework,Version=v4.5.2"] = new Framework
            {
                Name = ".NETFramework,Version=v4.5.2",
                FrameworkName = ".NETFramework",
                Version = "4.5.2.0",
                ShortName = "net452",
            },
            [".NETFramework,Version=v4.6"] = new Framework
            {
                Name = ".NETFramework,Version=v4.6",
                FrameworkName = ".NETFramework",
                Version = "4.6.0.0",
                ShortName = "net46",
            },
            [".NETFramework,Version=v4.6.1"] = new Framework
            {
                Name = ".NETFramework,Version=v4.6.1",
                FrameworkName = ".NETFramework",
                Version = "4.6.1.0",
                ShortName = "net461",
            },
            [".NETFramework,Version=v4.6.2"] = new Framework
            {
                Name = ".NETFramework,Version=v4.6.2",
                FrameworkName = ".NETFramework",
                Version = "4.6.2.0",
                ShortName = "net462",
            },
            [".NETFramework,Version=v4.6.3"] = new Framework
            {
                Name = ".NETFramework,Version=v4.6.3",
                FrameworkName = ".NETFramework",
                Version = "4.6.3.0",
                ShortName = "net463",
            },
            [".NETCore,Version=v4.5"] = new Framework
            {
                Name = ".NETCore,Version=v4.5",
                FrameworkName = ".NETCore",
                Version = "4.5.0.0",
                ShortName = "netcore45",
            },
            [".NETCore,Version=v4.5.1"] = new Framework
            {
                Name = ".NETCore,Version=v4.5.1",
                FrameworkName = ".NETCore",
                Version = "4.5.1.0",
                ShortName = "netcore451",
            },
            [".NETCore,Version=v5.0"] = new Framework
            {
                Name = ".NETCore,Version=v5.0",
                FrameworkName = ".NETCore",
                Version = "5.0.0.0",
                ShortName = "netcore50",
            },
            ["Windows,Version=v8.0"] = new Framework
            {
                Name = "Windows,Version=v8.0",
                FrameworkName = "Windows",
                Version = "8.0.0.0",
                ShortName = "win8",
            },
            ["Windows,Version=v8.1"] = new Framework
            {
                Name = "Windows,Version=v8.1",
                FrameworkName = "Windows",
                Version = "8.1.0.0",
                ShortName = "win81",
            },
            ["Windows,Version=v10.0"] = new Framework
            {
                Name = "Windows,Version=v10.0",
                FrameworkName = "Windows",
                Version = "10.0.0.0",
                ShortName = "win10.0",
            },
            ["Silverlight,Version=v4.0"] = new Framework
            {
                Name = "Silverlight,Version=v4.0",
                FrameworkName = "Silverlight",
                Version = "4.0.0.0",
                ShortName = "sl4",
            },
            ["Silverlight,Version=v5.0"] = new Framework
            {
                Name = "Silverlight,Version=v5.0",
                FrameworkName = "Silverlight",
                Version = "5.0.0.0",
                ShortName = "sl5",
            },
            ["WindowsPhone,Version=v7.0"] = new Framework
            {
                Name = "WindowsPhone,Version=v7.0",
                FrameworkName = "WindowsPhone",
                Version = "7.0.0.0",
                ShortName = "wp7",
            },
            ["WindowsPhone,Version=v7.5"] = new Framework
            {
                Name = "WindowsPhone,Version=v7.5",
                FrameworkName = "WindowsPhone",
                Version = "7.5.0.0",
                ShortName = "wp75",
            },
            ["WindowsPhone,Version=v8.0"] = new Framework
            {
                Name = "WindowsPhone,Version=v8.0",
                FrameworkName = "WindowsPhone",
                Version = "8.0.0.0",
                ShortName = "wp8",
            },
            ["WindowsPhone,Version=v8.1"] = new Framework
            {
                Name = "WindowsPhone,Version=v8.1",
                FrameworkName = "WindowsPhone",
                Version = "8.1.0.0",
                ShortName = "wp81",
            },
            ["WindowsPhoneApp,Version=v8.1"] = new Framework
            {
                Name = "WindowsPhoneApp,Version=v8.1",
                FrameworkName = "WindowsPhoneApp",
                Version = "8.1.0.0",
                ShortName = "wpa81",
            },
            ["Tizen,Version=v3.0"] = new Framework
            {
                Name = "Tizen,Version=v3.0",
                FrameworkName = "Tizen",
                Version = "3.0.0.0",
                ShortName = "tizen30",
            },
            ["Tizen,Version=v4.0"] = new Framework
            {
                Name = "Tizen,Version=v4.0",
                FrameworkName = "Tizen",
                Version = "4.0.0.0",
                ShortName = "tizen40",
            },
            ["Tizen,Version=v6.0"] = new Framework
            {
                Name = "Tizen,Version=v6.0",
                FrameworkName = "Tizen",
                Version = "6.0.0.0",
                ShortName = "tizen60",
            },
            ["ASP.NET,Version=v0.0"] = new Framework
            {
                Name = "ASP.NET,Version=v0.0",
                FrameworkName = "ASP.NET",
                Version = "0.0.0.0",
                ShortName = "aspnet",
            },
            ["ASP.NETCore,Version=v0.0"] = new Framework
            {
                Name = "ASP.NETCore,Version=v0.0",
                FrameworkName = "ASP.NETCore",
                Version = "0.0.0.0",
                ShortName = "aspnetcore",
            },
            ["ASP.NET,Version=v5.0"] = new Framework
            {
                Name = "ASP.NET,Version=v5.0",
                FrameworkName = "ASP.NET",
                Version = "5.0.0.0",
                ShortName = "aspnet50",
            },
            ["ASP.NETCore,Version=v5.0"] = new Framework
            {
                Name = "ASP.NETCore,Version=v5.0",
                FrameworkName = "ASP.NETCore",
                Version = "5.0.0.0",
                ShortName = "aspnetcore50",
            },
            ["DNX,Version=v0.0"] = new Framework
            {
                Name = "DNX,Version=v0.0",
                FrameworkName = "DNX",
                Version = "0.0.0.0",
                ShortName = "dnx",
            },
            ["DNX,Version=v4.5"] = new Framework
            {
                Name = "DNX,Version=v4.5",
                FrameworkName = "DNX",
                Version = "4.5.0.0",
                ShortName = "dnx45",
            },
            ["DNX,Version=v4.5.1"] = new Framework
            {
                Name = "DNX,Version=v4.5.1",
                FrameworkName = "DNX",
                Version = "4.5.1.0",
                ShortName = "dnx451",
            },
            ["DNX,Version=v4.5.2"] = new Framework
            {
                Name = "DNX,Version=v4.5.2",
                FrameworkName = "DNX",
                Version = "4.5.2.0",
                ShortName = "dnx452",
            },
            ["DNXCore,Version=v0.0"] = new Framework
            {
                Name = "DNXCore,Version=v0.0",
                FrameworkName = "DNXCore",
                Version = "0.0.0.0",
                ShortName = "dnxcore",
            },
            ["DNXCore,Version=v5.0"] = new Framework
            {
                Name = "DNXCore,Version=v5.0",
                FrameworkName = "DNXCore",
                Version = "5.0.0.0",
                ShortName = "dnxcore50",
            },
            [".NETPlatform,Version=v5.0"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.0",
                FrameworkName = ".NETPlatform",
                Version = "0.0.0.0",
                ShortName = "dotnet",
            },
            [".NETPlatform,Version=v5.0"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.0",
                FrameworkName = ".NETPlatform",
                Version = "5.0.0.0",
                ShortName = "dotnet",
            },
            [".NETPlatform,Version=v5.1"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.1",
                FrameworkName = ".NETPlatform",
                Version = "5.1.0.0",
                ShortName = "dotnet51",
            },
            [".NETPlatform,Version=v5.2"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.2",
                FrameworkName = ".NETPlatform",
                Version = "5.2.0.0",
                ShortName = "dotnet52",
            },
            [".NETPlatform,Version=v5.3"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.3",
                FrameworkName = ".NETPlatform",
                Version = "5.3.0.0",
                ShortName = "dotnet53",
            },
            [".NETPlatform,Version=v5.4"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.4",
                FrameworkName = ".NETPlatform",
                Version = "5.4.0.0",
                ShortName = "dotnet54",
            },
            [".NETPlatform,Version=v5.5"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.5",
                FrameworkName = ".NETPlatform",
                Version = "5.5.0.0",
                ShortName = "dotnet55",
            },
            [".NETPlatform,Version=v5.6"] = new Framework
            {
                Name = ".NETPlatform,Version=v5.6",
                FrameworkName = ".NETPlatform",
                Version = "5.6.0.0",
                ShortName = "dotnet56",
            },
            [".NETStandard,Version=v0.0"] = new Framework
            {
                Name = ".NETStandard,Version=v0.0",
                FrameworkName = ".NETStandard",
                Version = "0.0.0.0",
                ShortName = "netstandard",
            },
            [".NETStandard,Version=v1.0"] = new Framework
            {
                Name = ".NETStandard,Version=v1.0",
                FrameworkName = ".NETStandard",
                Version = "1.0.0.0",
                ShortName = "netstandard1.0",
            },
            [".NETStandard,Version=v1.1"] = new Framework
            {
                Name = ".NETStandard,Version=v1.1",
                FrameworkName = ".NETStandard",
                Version = "1.1.0.0",
                ShortName = "netstandard1.1",
            },
            [".NETStandard,Version=v1.2"] = new Framework
            {
                Name = ".NETStandard,Version=v1.2",
                FrameworkName = ".NETStandard",
                Version = "1.2.0.0",
                ShortName = "netstandard1.2",
            },
            [".NETStandard,Version=v1.3"] = new Framework
            {
                Name = ".NETStandard,Version=v1.3",
                FrameworkName = ".NETStandard",
                Version = "1.3.0.0",
                ShortName = "netstandard1.3",
            },
            [".NETStandard,Version=v1.4"] = new Framework
            {
                Name = ".NETStandard,Version=v1.4",
                FrameworkName = ".NETStandard",
                Version = "1.4.0.0",
                ShortName = "netstandard1.4",
            },
            [".NETStandard,Version=v1.5"] = new Framework
            {
                Name = ".NETStandard,Version=v1.5",
                FrameworkName = ".NETStandard",
                Version = "1.5.0.0",
                ShortName = "netstandard1.5",
            },
            [".NETStandard,Version=v1.6"] = new Framework
            {
                Name = ".NETStandard,Version=v1.6",
                FrameworkName = ".NETStandard",
                Version = "1.6.0.0",
                ShortName = "netstandard1.6",
            },
            [".NETStandard,Version=v1.7"] = new Framework
            {
                Name = ".NETStandard,Version=v1.7",
                FrameworkName = ".NETStandard",
                Version = "1.7.0.0",
                ShortName = "netstandard1.7",
            },
            [".NETStandard,Version=v2.0"] = new Framework
            {
                Name = ".NETStandard,Version=v2.0",
                FrameworkName = ".NETStandard",
                Version = "2.0.0.0",
                ShortName = "netstandard2.0",
            },
            [".NETStandard,Version=v2.1"] = new Framework
            {
                Name = ".NETStandard,Version=v2.1",
                FrameworkName = ".NETStandard",
                Version = "2.1.0.0",
                ShortName = "netstandard2.1",
            },
            [".NETStandardApp,Version=v1.5"] = new Framework
            {
                Name = ".NETStandardApp,Version=v1.5",
                FrameworkName = ".NETStandardApp",
                Version = "1.5.0.0",
                ShortName = "netstandardapp15",
            },
            ["UAP,Version=v10.0"] = new Framework
            {
                Name = "UAP,Version=v10.0",
                FrameworkName = "UAP",
                Version = "10.0.0.0",
                ShortName = "uap10.0",
            },
            [".NETCoreApp,Version=v1.0"] = new Framework
            {
                Name = ".NETCoreApp,Version=v1.0",
                FrameworkName = ".NETCoreApp",
                Version = "1.0.0.0",
                ShortName = "netcoreapp1.0",
            },
            [".NETCoreApp,Version=v1.1"] = new Framework
            {
                Name = ".NETCoreApp,Version=v1.1",
                FrameworkName = ".NETCoreApp",
                Version = "1.1.0.0",
                ShortName = "netcoreapp1.1",
            },
            [".NETCoreApp,Version=v2.0"] = new Framework
            {
                Name = ".NETCoreApp,Version=v2.0",
                FrameworkName = ".NETCoreApp",
                Version = "2.0.0.0",
                ShortName = "netcoreapp2.0",
            },
            [".NETCoreApp,Version=v2.1"] = new Framework
            {
                Name = ".NETCoreApp,Version=v2.1",
                FrameworkName = ".NETCoreApp",
                Version = "2.1.0.0",
                ShortName = "netcoreapp2.1",
            },
            [".NETCoreApp,Version=v2.2"] = new Framework
            {
                Name = ".NETCoreApp,Version=v2.2",
                FrameworkName = ".NETCoreApp",
                Version = "2.2.0.0",
                ShortName = "netcoreapp2.2",
            },
            [".NETCoreApp,Version=v3.0"] = new Framework
            {
                Name = ".NETCoreApp,Version=v3.0",
                FrameworkName = ".NETCoreApp",
                Version = "3.0.0.0",
                ShortName = "netcoreapp3.0",
            },
            [".NETCoreApp,Version=v3.1"] = new Framework
            {
                Name = ".NETCoreApp,Version=v3.1",
                FrameworkName = ".NETCoreApp",
                Version = "3.1.0.0",
                ShortName = "netcoreapp3.1",
            },
            [".NETCoreApp,Version=v5.0"] = new Framework
            {
                Name = ".NETCoreApp,Version=v5.0",
                FrameworkName = ".NETCoreApp",
                Version = "5.0.0.0",
                ShortName = "net5.0",
            },

        };
        private static bool TryParseCommonFramework(string frameworkString, out string framework)
        {
            framework = null;

            frameworkString = frameworkString.ToLowerInvariant();

            switch (frameworkString)
            {
                case "dotnet":
                case "dotnet50":
                case "dotnet5.0":
                    framework = ".NETPlatform,Version=v5.0";
                    break;
                case "net40":
                case "net4":
                    framework = ".NETFramework,Version=v4.0";
                    break;
                case "net45":
                    framework = ".NETFramework,Version=v4.5";
                    break;
                case "net451":
                    framework = ".NETFramework,Version=v4.5.1";
                    break;
                case "net46":
                    framework = ".NETFramework,Version=v4.6";
                    break;
                case "net461":
                    framework = ".NETFramework,Version=v4.6.1";
                    break;
                case "net462":
                    framework = ".NETFramework,Version=v4.6.2";
                    break;
                case "win8":
                    framework = "Windows,Version=v8.0";
                    break;
                case "win81":
                    framework = "Windows,Version=v8.1";
                    break;
                case "netstandard":
                    framework = ".NETStandard,Version=v0.0";
                    break;
                case "netstandard1.0":
                case "netstandard10":
                    framework = ".NETStandard,Version=v1.0";
                    break;
                case "netstandard1.1":
                case "netstandard11":
                    framework = ".NETStandard,Version=v1.1";
                    break;
                case "netstandard1.2":
                case "netstandard12":
                    framework = ".NETStandard,Version=v1.2";
                    break;
                case "netstandard1.3":
                case "netstandard13":
                    framework = ".NETStandard,Version=v1.3";
                    break;
                case "netstandard1.4":
                case "netstandard14":
                    framework = ".NETStandard,Version=v1.4";
                    break;
                case "netstandard1.5":
                case "netstandard15":
                    framework = ".NETStandard,Version=v1.5";
                    break;
                case "netstandard1.6":
                case "netstandard16":
                    framework = ".NETStandard,Version=v1.6";
                    break;
                case "netstandard1.7":
                case "netstandard17":
                    framework = ".NETStandard,Version=v1.7";
                    break;
                case "netstandard2.0":
                case "netstandard20":
                    framework = ".NETStandard,Version=v2.0";
                    break;
                case "netstandard2.1":
                case "netstandard21":
                    framework = ".NETStandard,Version=v2.1";
                    break;
                case "netcoreapp2.1":
                case "netcoreapp21":
                    framework = ".NETCoreApp,Version=v2.1";
                    break;
                case "netcoreapp3.1":
                case "netcoreapp31":
                    framework = ".NETCoreApp,Version=v3.1";
                    break;
                case "netcoreapp5.0":
                case "netcoreapp50":
                case "net5.0":
                case "net50":
                    framework = ".NETCoreApp,Version=v5.0";
                    break;
            }

            return framework != null;
        }

        #endregion

#if NETFRAMEWORK
        private static readonly Framework Default = Framework.FromString(".NETFramework,Version=v4.0");
#else
        private static readonly Framework Default = Framework.FromString(".NETCoreApp,Version=v1.0");
#endif

        private Framework()
        {
        }

        /// <summary>
        /// Default .Net target framework.
        /// </summary>
        public static Framework DefaultFramework => Framework.Default;

        /// <summary>
        /// Gets the FullName of framework.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the framework name such as .NETCoreApp.
        /// </summary>
        public string FrameworkName { get; private set; }

        /// <summary>
        /// Gets the framework version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Common short name, as well as directory name, such as net5.0.
        /// </summary>
        public string ShortName { get; private set; }

        /// <summary>
        /// Returns a valid framework else returns null
        /// </summary>
        /// <param name="frameworkString">Framework name</param>
        /// <returns>A framework object</returns>
        public static Framework FromString(string frameworkString)
        {
            if (string.IsNullOrWhiteSpace(frameworkString))
            {
                return null;
            }

            try
            {
                // IDE always sends framework in form of ENUM, which always throws exception
                // This throws up in first chance exception, refer Bug https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/591142
                switch (frameworkString.Trim().ToLower())
                {
                    // Maps common names to newer version of a common name,
                    // not adding it to TryParseCommonFramework below, because that is 
                    // copied from Nuget.Frameworks, and we might need to update that in the future
                    // which would likely cause these additional names to get lost.
                    case "framework35":
                        frameworkString = "net35";
                        break;
                    case "framework40":
                        frameworkString = "net40";
                        break;
                    case "framework45":
                        frameworkString = "net45";
                        break;
                    case "frameworkcore10":
                        frameworkString = "netcoreapp1.0";
                        break;
                    case "frameworkuap10":
                    case "uap10":
                        frameworkString = "uap10.0";
                        break;
                }

                return TryParse(frameworkString, out var framework) ? framework : null;
            }
            catch
            {
                return null;
            }
        }

        public static string GetShortFolderName(string frameworkName)
        {
            return FromString(frameworkName)?.ShortName ?? frameworkName;
        }

        /// <summary>
        /// Returns full name of the framework.
        /// </summary>
        /// <returns>String presentation of the object.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        internal static bool TryParse(string frameworkString, out Framework framework)
        {
            if (string.IsNullOrWhiteSpace(frameworkString))
            {
                framework = null;
                return false;
            }

            if (mapping.TryGetValue(frameworkString, out framework))
            {
                // we found it by long name
                return true;
            }

            var byShortName = mapping.Values.SingleOrDefault(t => t.ShortName == frameworkString);
            if (byShortName != null)
            {
                // we found it by a common short name, e.g net5.0
                framework = byShortName;
                return true;
            }

            if (TryParseCommonFramework(frameworkString, out var fullName))
            {
                // we found it by uncommon short name e.g. net50 
                // we get long name, try find it in the mapping
                if (mapping.TryGetValue(frameworkString, out framework))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
