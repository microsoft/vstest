// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGetClone.Frameworks
{
    internal sealed class DefaultFrameworkMappings : IFrameworkMappings
    {
        private static Lazy<KeyValuePair<string, string>[]> IdentifierSynonymsLazy = new(() =>
        {
            return
            [
                // .NET
                new KeyValuePair<string, string>("NETFramework", FrameworkConstants.FrameworkIdentifiers.Net),
                new KeyValuePair<string, string>(".NET", FrameworkConstants.FrameworkIdentifiers.Net),

                // .NET Core
                new KeyValuePair<string, string>("NETCore", FrameworkConstants.FrameworkIdentifiers.NetCore),

                // Portable
                new KeyValuePair<string, string>("NETPortable", FrameworkConstants.FrameworkIdentifiers.Portable),

                // ASP
                new KeyValuePair<string, string>("asp.net", FrameworkConstants.FrameworkIdentifiers.AspNet),
                new KeyValuePair<string, string>("asp.netcore", FrameworkConstants.FrameworkIdentifiers.AspNetCore),

                // Mono/Xamarin
                new KeyValuePair<string, string>("Xamarin.PlayStationThree", FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation3),
                new KeyValuePair<string, string>("XamarinPlayStationThree", FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation3),
                new KeyValuePair<string, string>("Xamarin.PlayStationFour", FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation4),
                new KeyValuePair<string, string>("XamarinPlayStationFour", FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation4),
                new KeyValuePair<string, string>("XamarinPlayStationVita", FrameworkConstants.FrameworkIdentifiers.XamarinPlayStationVita)
            ];
        });

        public IEnumerable<KeyValuePair<string, string>> IdentifierSynonyms
        {
            get
            {
                return IdentifierSynonymsLazy.Value;
            }
        }

        private static readonly Lazy<KeyValuePair<string, string>[]> IdentifierShortNamesLazy = new(() =>
        {
            return
            [
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.NetCoreApp, "netcoreapp"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.NetStandardApp, "netstandardapp"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.NetStandard, "netstandard"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.NetPlatform, "dotnet"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.Net, "net"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.NetMicro, "netmf"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.Silverlight, "sl"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.Portable, "portable"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.WindowsPhone, "wp"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.WindowsPhoneApp, "wpa"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.Windows, "win"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.AspNet, "aspnet"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.AspNetCore, "aspnetcore"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.Native, "native"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.MonoAndroid, "monoandroid"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.MonoTouch, "monotouch"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.MonoMac, "monomac"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinIOs, "xamarinios"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinMac, "xamarinmac"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation3, "xamarinpsthree"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation4, "xamarinpsfour"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinPlayStationVita, "xamarinpsvita"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinWatchOS, "xamarinwatchos"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinTVOS, "xamarintvos"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinXbox360, "xamarinxboxthreesixty"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.XamarinXboxOne, "xamarinxboxone"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.Dnx, "dnx"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.DnxCore, "dnxcore"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.NetCore, "netcore"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.WinRT, "winrt"), // legacy
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.UAP, "uap"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.Tizen, "tizen"),
                new KeyValuePair<string, string>(FrameworkConstants.FrameworkIdentifiers.NanoFramework, "netnano")
            ];
        });

        public IEnumerable<KeyValuePair<string, string>> IdentifierShortNames
        {
            get
            {
                return IdentifierShortNamesLazy.Value;
            }
        }

        private static readonly Lazy<FrameworkSpecificMapping[]> ProfileShortNamesLazy = new(() =>
        {
            return
            [
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Net, "Client", "Client"),
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Net, "CF", "CompactFramework"),
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Net, "Full", string.Empty),
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Silverlight, "WP", "WindowsPhone"),
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Silverlight, "WP71", "WindowsPhone71")
            ];
        });

        public IEnumerable<FrameworkSpecificMapping> ProfileShortNames
        {
            get
            {
                return ProfileShortNamesLazy.Value;
            }
        }

        private static readonly Lazy<KeyValuePair<NuGetFramework, NuGetFramework>[]> EquivalentFrameworksLazy = new(() =>
        {
            return
            [
                // UAP 0.0 <-> UAP 10.0
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, FrameworkConstants.EmptyVersion),
                    FrameworkConstants.CommonFrameworks.UAP10),

                // win <-> win8
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Windows, FrameworkConstants.EmptyVersion),
                    FrameworkConstants.CommonFrameworks.Win8),

                // win8 <-> netcore45
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.Win8,
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.NetCore, new Version(4, 5, 0, 0))),

                // netcore45 <-> winrt45
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.NetCore, new Version(4, 5, 0, 0)),
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WinRT, new Version(4, 5, 0, 0))),

                // netcore <-> netcore45
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.NetCore, FrameworkConstants.EmptyVersion),
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.NetCore, new Version(4, 5, 0, 0))),

                // winrt <-> winrt45
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WinRT, FrameworkConstants.EmptyVersion),
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WinRT, new Version(4, 5, 0, 0))),

                // win81 <-> netcore451
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.Win81,
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.NetCore, new Version(4, 5, 1, 0))),

                // wp <-> wp7
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WindowsPhone, FrameworkConstants.EmptyVersion),
                    FrameworkConstants.CommonFrameworks.WP7),

                // wp7 <-> f:sl3-wp
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.WP7,
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Silverlight, new Version(3, 0, 0, 0), "WindowsPhone")),

                // wp71 <-> f:sl4-wp71
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WindowsPhone, new Version(7, 1, 0, 0)),
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Silverlight, new Version(4, 0, 0, 0), "WindowsPhone71")),

                // wp8 <-> f:sl8-wp
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.WP8,
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Silverlight, new Version(8, 0, 0, 0), "WindowsPhone")),

                // wp81 <-> f:sl81-wp
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.WP81,
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Silverlight, new Version(8, 1, 0, 0), "WindowsPhone")),

                // wpa <-> wpa81
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WindowsPhoneApp, FrameworkConstants.EmptyVersion),
                    FrameworkConstants.CommonFrameworks.WPA81),

                // tizen <-> tizen3
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Tizen, FrameworkConstants.EmptyVersion),
                    FrameworkConstants.CommonFrameworks.Tizen3),

                // dnx <-> dnx45
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.Dnx,
                    FrameworkConstants.CommonFrameworks.Dnx45),

                // dnxcore <-> dnxcore50
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.DnxCore,
                    FrameworkConstants.CommonFrameworks.DnxCore50),

                // dotnet <-> dotnet50
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.DotNet,
                    FrameworkConstants.CommonFrameworks.DotNet50),

                // TODO: remove these rules post-RC
                // aspnet <-> aspnet50
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.AspNet,
                    FrameworkConstants.CommonFrameworks.AspNet50),

                // aspnetcore <-> aspnetcore50
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.AspNetCore,
                    FrameworkConstants.CommonFrameworks.AspNetCore50),

                // dnx451 <-> aspnet50
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.Dnx45,
                    FrameworkConstants.CommonFrameworks.AspNet50),

                // dnxcore50 <-> aspnetcore50
                new KeyValuePair<NuGetFramework, NuGetFramework>(
                    FrameworkConstants.CommonFrameworks.DnxCore50,
                    FrameworkConstants.CommonFrameworks.AspNetCore50)
            ];
        });

        public IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> EquivalentFrameworks
        {
            get
            {
                return EquivalentFrameworksLazy.Value;
            }
        }

        private static readonly Lazy<FrameworkSpecificMapping[]> EquivalentProfilesLazy = new(() =>
        {
            return
            [
                // The client profile, for the purposes of NuGet, is the same as the full framework
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Net, "Client", string.Empty),
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Net, "Full", string.Empty),
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.Silverlight, "WindowsPhone71", "WindowsPhone"),
                new FrameworkSpecificMapping(FrameworkConstants.FrameworkIdentifiers.WindowsPhone, "WindowsPhone71", "WindowsPhone")
            ];
        });

        public IEnumerable<FrameworkSpecificMapping> EquivalentProfiles
        {
            get
            {
                return EquivalentProfilesLazy.Value;
            }
        }

        private static readonly Lazy<KeyValuePair<string, string>[]> SubSetFrameworksLazy = new(() =>
        {
            return
            [
                // .NET is a subset of DNX
                new KeyValuePair<string, string>(
                    FrameworkConstants.FrameworkIdentifiers.Net,
                    FrameworkConstants.FrameworkIdentifiers.Dnx),

                // NetPlatform is a subset of DNXCore
                new KeyValuePair<string, string>(
                    FrameworkConstants.FrameworkIdentifiers.NetPlatform,
                    FrameworkConstants.FrameworkIdentifiers.DnxCore),

                // NetStandard is a subset of NetStandardApp
                new KeyValuePair<string, string>(
                    FrameworkConstants.FrameworkIdentifiers.NetStandard,
                    FrameworkConstants.FrameworkIdentifiers.NetStandardApp)
            ];
        });

        public IEnumerable<KeyValuePair<string, string>> SubSetFrameworks
        {
            get
            {
                return SubSetFrameworksLazy.Value;
            }
        }

        private static readonly Lazy<OneWayCompatibilityMappingEntry[]> CompatibilityMappingsLazy = new(() =>
        {
            return new[]
                {
                    // UAP supports Win81
                    new OneWayCompatibilityMappingEntry(new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, FrameworkConstants.EmptyVersion),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, FrameworkConstants.MaxVersion)),
                        new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Windows, FrameworkConstants.EmptyVersion),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Windows, new Version(8, 1, 0, 0)))),

                    // UAP supports WPA81
                    new OneWayCompatibilityMappingEntry(new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, FrameworkConstants.EmptyVersion),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, FrameworkConstants.MaxVersion)),
                        new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WindowsPhoneApp, FrameworkConstants.EmptyVersion),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WindowsPhoneApp, new Version(8, 1, 0, 0)))),

                    // UAP supports NetCore50
                    new OneWayCompatibilityMappingEntry(new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, FrameworkConstants.EmptyVersion),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, FrameworkConstants.MaxVersion)),
                        new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.NetCore, FrameworkConstants.Version5),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.NetCore, FrameworkConstants.Version5))),

                    // Win projects support WinRT
                    new OneWayCompatibilityMappingEntry(new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Windows, FrameworkConstants.EmptyVersion),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Windows, FrameworkConstants.MaxVersion)),
                        new FrameworkRange(
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WinRT, FrameworkConstants.EmptyVersion),
                            new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.WinRT, new Version(4, 5, 0, 0)))),

                    // Tizen3 projects support NETStandard1.6
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.Tizen3,
                        FrameworkConstants.CommonFrameworks.NetStandard16),

                    // Tizen4 projects support NETStandard2.0
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.Tizen4,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    // Tizen6 projects support NETStandard2.1
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.Tizen6,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    // UAP 10.0.15064.0 projects support NETStandard2.0
                    CreateStandardMapping(
                        new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.UAP, new Version(10, 0, 15064, 0)),
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    // NetCoreApp1.0 projects support NetStandard1.6
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.NetCoreApp10,
                        FrameworkConstants.CommonFrameworks.NetStandard16),

                    // NetCoreApp1.1 projects support NetStandard1.7
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.NetCoreApp11,
                        FrameworkConstants.CommonFrameworks.NetStandard17),

                    // NetCoreApp2.0 projects support NetStandard2.0
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.NetCoreApp20,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    // NetCoreApp3.0 projects support NetStandard2.1
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.NetCoreApp30,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    // net463 projects support NetStandard2.0
                    CreateStandardMapping(
                        FrameworkConstants.CommonFrameworks.Net463,
                        FrameworkConstants.CommonFrameworks.NetStandard20)
                }
                .Concat(new[]
                {
                    // dnxcore50 -> dotnet5.6, netstandard1.5
                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.DnxCore,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard15),

                    // uap -> dotnet5.5, netstandard1.4
                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.UAP,
                        FrameworkConstants.CommonFrameworks.DotNet55,
                        FrameworkConstants.CommonFrameworks.NetStandard14),

                    // netcore50 -> dotnet5.5, netstandard1.4
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.NetCore50,
                        FrameworkConstants.CommonFrameworks.DotNet55,
                        FrameworkConstants.CommonFrameworks.NetStandard14),

                    // wpa81 -> dotnet5.3, netstandard1.2
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.WPA81,
                        FrameworkConstants.CommonFrameworks.DotNet53,
                        FrameworkConstants.CommonFrameworks.NetStandard12),

                    // wp8, wp81 -> dotnet5.1, netstandard1.0
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.WP8,
                        FrameworkConstants.CommonFrameworks.DotNet51,
                        FrameworkConstants.CommonFrameworks.NetStandard10),

                    // net45 -> dotnet5.2, netstandard1.1
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.Net45,
                        FrameworkConstants.CommonFrameworks.DotNet52,
                        FrameworkConstants.CommonFrameworks.NetStandard11),

                    // net451 -> dotnet5.3, netstandard1.2
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.Net451,
                        FrameworkConstants.CommonFrameworks.DotNet53,
                        FrameworkConstants.CommonFrameworks.NetStandard12),

                    // net46 -> dotnet5.4, netstandard1.3
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.Net46,
                        FrameworkConstants.CommonFrameworks.DotNet54,
                        FrameworkConstants.CommonFrameworks.NetStandard13),

                    // net461 -> dotnet5.5, netstandard2.0
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.Net461,
                        FrameworkConstants.CommonFrameworks.DotNet55,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    // net462 -> dotnet5.6, netstandard2.0
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.Net462,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    // netcore45 -> dotnet5.2, netstandard1.1
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.NetCore45,
                        FrameworkConstants.CommonFrameworks.DotNet52,
                        FrameworkConstants.CommonFrameworks.NetStandard11),

                    // netcore451 -> dotnet5.3, netstandard1.2
                    CreateGenerationAndStandardMapping(
                        FrameworkConstants.CommonFrameworks.NetCore451,
                        FrameworkConstants.CommonFrameworks.DotNet53,
                        FrameworkConstants.CommonFrameworks.NetStandard12),

                    // xamarin frameworks
                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.MonoAndroid,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.MonoMac,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.MonoTouch,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinIOs,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinMac,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation3,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinPlayStation4,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinPlayStationVita,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinXbox360,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinXboxOne,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard20),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinTVOS,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard21),

                    CreateGenerationAndStandardMappingForAllVersions(
                        FrameworkConstants.FrameworkIdentifiers.XamarinWatchOS,
                        FrameworkConstants.CommonFrameworks.DotNet56,
                        FrameworkConstants.CommonFrameworks.NetStandard21)
                }.SelectMany(mappings => mappings))
                .ToArray();
        });

        public IEnumerable<OneWayCompatibilityMappingEntry> CompatibilityMappings
        {
            get
            {
                return CompatibilityMappingsLazy.Value;
            }
        }

        private static OneWayCompatibilityMappingEntry CreateGenerationMapping(
            NuGetFramework framework,
            NuGetFramework netPlatform)
        {
            return new OneWayCompatibilityMappingEntry(
                new FrameworkRange(
                    framework,
                    new NuGetFramework(framework.Framework, FrameworkConstants.MaxVersion)),
                new FrameworkRange(
                    FrameworkConstants.CommonFrameworks.DotNet,
                    netPlatform));
        }

        private static OneWayCompatibilityMappingEntry CreateStandardMapping(
            NuGetFramework framework,
            NuGetFramework netPlatform)
        {
            return new OneWayCompatibilityMappingEntry(
                new FrameworkRange(
                    framework,
                    new NuGetFramework(framework.Framework, FrameworkConstants.MaxVersion)),
                new FrameworkRange(
                    FrameworkConstants.CommonFrameworks.NetStandard10,
                    netPlatform));
        }

        private static IEnumerable<OneWayCompatibilityMappingEntry> CreateGenerationAndStandardMapping(
            NuGetFramework framework,
            NuGetFramework netPlatform,
            NuGetFramework netStandard)
        {
            yield return CreateGenerationMapping(framework, netPlatform);
            yield return CreateStandardMapping(framework, netStandard);
        }

        private static IEnumerable<OneWayCompatibilityMappingEntry> CreateGenerationAndStandardMappingForAllVersions(
            string framework,
            NuGetFramework netPlatform,
            NuGetFramework netStandard)
        {
            var lowestFramework = new NuGetFramework(framework, FrameworkConstants.EmptyVersion);
            return CreateGenerationAndStandardMapping(lowestFramework, netPlatform, netStandard);
        }

        private static readonly Lazy<string[]> NonPackageBasedFrameworkPrecedenceLazy = new(() =>
        {
            return
            [
                FrameworkConstants.FrameworkIdentifiers.Net,
                FrameworkConstants.FrameworkIdentifiers.NetCore,
                FrameworkConstants.FrameworkIdentifiers.Windows,
                FrameworkConstants.FrameworkIdentifiers.WindowsPhoneApp
            ];
        });

        public IEnumerable<string> NonPackageBasedFrameworkPrecedence
        {
            get
            {
                return NonPackageBasedFrameworkPrecedenceLazy.Value;
            }
        }

        private static readonly Lazy<string[]> PackageBasedFrameworkPrecedenceLazy = new(() =>
        {
            return
            [
                FrameworkConstants.FrameworkIdentifiers.NetCoreApp,
                FrameworkConstants.FrameworkIdentifiers.NetStandardApp,
                FrameworkConstants.FrameworkIdentifiers.NetStandard,
                FrameworkConstants.FrameworkIdentifiers.NetPlatform
            ];
        });

        public IEnumerable<string> PackageBasedFrameworkPrecedence
        {
            get
            {
                return PackageBasedFrameworkPrecedenceLazy.Value;
            }
        }

        private static readonly Lazy<string[]> EquivalentFrameworkPrecedenceLazy = new(() =>
        {
            return
            [
                FrameworkConstants.FrameworkIdentifiers.Windows,
                FrameworkConstants.FrameworkIdentifiers.NetCore,
                FrameworkConstants.FrameworkIdentifiers.WinRT,

                FrameworkConstants.FrameworkIdentifiers.WindowsPhone,
                FrameworkConstants.FrameworkIdentifiers.Silverlight,

                FrameworkConstants.FrameworkIdentifiers.DnxCore,
                FrameworkConstants.FrameworkIdentifiers.AspNetCore,

                FrameworkConstants.FrameworkIdentifiers.Dnx,
                FrameworkConstants.FrameworkIdentifiers.AspNet
            ];
        });

        public IEnumerable<string> EquivalentFrameworkPrecedence
        {
            get
            {
                return EquivalentFrameworkPrecedenceLazy.Value;
            }
        }

        private static readonly Lazy<KeyValuePair<NuGetFramework, NuGetFramework>[]> ShortNameReplacementsLazy = new(() =>
        {
            return
            [
                new KeyValuePair<NuGetFramework, NuGetFramework>(FrameworkConstants.CommonFrameworks.DotNet50, FrameworkConstants.CommonFrameworks.DotNet)
            ];
        });

        public IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> ShortNameReplacements
        {
            get
            {
                return ShortNameReplacementsLazy.Value;
            }
        }

        private static readonly Lazy<KeyValuePair<NuGetFramework, NuGetFramework>[]> FullNameReplacementsLazy = new(() =>
        {
            return
            [
                new KeyValuePair<NuGetFramework, NuGetFramework>(FrameworkConstants.CommonFrameworks.DotNet, FrameworkConstants.CommonFrameworks.DotNet50)
            ];
        });

        public IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> FullNameReplacements
        {
            get
            {
                return FullNameReplacementsLazy.Value;
            }
        }

        private static readonly Lazy<IFrameworkMappings> InstanceLazy = new(() => new DefaultFrameworkMappings());

        /// <summary>
        /// Singleton instance of the default framework mappings.
        /// </summary>
        public static IFrameworkMappings Instance
        {
            get
            {
                return InstanceLazy.Value;
            }
        }
    }
}
