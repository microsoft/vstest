// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace NuGetClone.Frameworks
{
    internal partial class NuGetFramework
    {
        /// <summary>
        /// An unknown or invalid framework
        /// </summary>
        public static readonly NuGetFramework UnsupportedFramework = new(FrameworkConstants.SpecialIdentifiers.Unsupported);

        /// <summary>
        /// A framework with no specific target framework. This can be used for content only packages.
        /// </summary>
        public static readonly NuGetFramework AgnosticFramework = new(FrameworkConstants.SpecialIdentifiers.Agnostic);

        /// <summary>
        /// A wildcard matching all frameworks
        /// </summary>
        public static readonly NuGetFramework AnyFramework = new(FrameworkConstants.SpecialIdentifiers.Any);

        /// <summary>
        /// Creates a NuGetFramework from a folder name using the default mappings.
        /// </summary>
        public static NuGetFramework Parse(string folderName)
        {
            return Parse(folderName, DefaultFrameworkNameProvider.Instance);
        }

        /// <summary>
        /// Creates a NuGetFramework from a folder name using the given mappings.
        /// </summary>
        public static NuGetFramework Parse(string folderName, IFrameworkNameProvider mappings)
        {
            if (folderName == null) throw new ArgumentNullException(nameof(folderName));
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));

            Debug.Assert(folderName.IndexOf(";", StringComparison.Ordinal) < 0, "invalid folder name, this appears to contain multiple frameworks");

            NuGetFramework framework = folderName.IndexOf(',') > -1
                ? ParseFrameworkName(folderName, mappings)
                : ParseFolder(folderName, mappings);

            return framework;
        }

        /// <summary>
        /// Creates a NuGetFramework from individual components
        /// </summary>
        public static NuGetFramework ParseComponents(string targetFrameworkMoniker, string? targetPlatformMoniker)
        {
            return ParseComponents(targetFrameworkMoniker, targetPlatformMoniker, DefaultFrameworkNameProvider.Instance);
        }

        /// <summary>
        /// Creates a NuGetFramework from individual components, using the given mappings.
        /// This method may have individual component preference, as described in the remarks.
        /// </summary>
        /// <remarks>
        /// Profiles and TargetPlatforms can't mix. As such the precedence order is profile over target platforms (TPI, TPV).
        /// .NETCoreApp,Version=v5.0 and later do not support profiles.
        /// Target Platforms are ignored for any frameworks not supporting them.
        /// This allows to handle the old project scenarios where the TargetPlatformIdentifier and TargetPlatformVersion may be set to Windows and v7.0 respectively.
        /// </remarks>
        internal static NuGetFramework ParseComponents(string targetFrameworkMoniker, string? targetPlatformMoniker, IFrameworkNameProvider mappings)
        {
            if (string.IsNullOrEmpty(targetFrameworkMoniker)) throw new ArgumentException(Strings.ArgumentCannotBeNullOrEmpty, nameof(targetFrameworkMoniker));
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));

            NuGetFramework? result;
            string targetFrameworkIdentifier;
            Version targetFrameworkVersion;
            var parts = GetParts(targetFrameworkMoniker);

            // if the first part is a special framework, ignore the rest
            if (TryParseSpecialFramework(parts[0], out result))
            {
                return result;
            }

            string? profile;
            string? targetFrameworkProfile;
            ParseFrameworkNameParts(mappings, parts, out targetFrameworkIdentifier, out targetFrameworkVersion, out targetFrameworkProfile);
            if (!mappings.TryGetProfile(targetFrameworkIdentifier, targetFrameworkProfile ?? string.Empty, out profile))
            {
                profile = targetFrameworkProfile;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.Portable, targetFrameworkIdentifier))
            {
                if (profile != null && mappings.TryGetPortableFrameworks(profile, out IEnumerable<NuGetFramework>? clientFrameworks))
                {
                    if (mappings.TryGetPortableProfile(clientFrameworks, out int profileNumber))
                    {
                        profile = FrameworkNameHelpers.GetPortableProfileNumberString(profileNumber);
                    }
                }
                else
                {
                    return UnsupportedFramework;
                }
            }

            var isNet5EraTfm = targetFrameworkVersion.Major >= 5 &&
                StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.NetCoreApp, targetFrameworkIdentifier);

            if (!string.IsNullOrEmpty(profile) && isNet5EraTfm)
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.FrameworkDoesNotSupportProfiles,
                    profile
                ));
            }

            if (!string.IsNullOrEmpty(targetPlatformMoniker) && isNet5EraTfm)
            {
                string targetPlatformIdentifier;
                Version platformVersion;
                ParsePlatformParts(GetParts(targetPlatformMoniker!), out targetPlatformIdentifier, out platformVersion);
                result = new NuGetFramework(targetFrameworkIdentifier, targetFrameworkVersion, targetPlatformIdentifier ?? string.Empty, platformVersion);
            }
            else
            {
                result = new NuGetFramework(targetFrameworkIdentifier, targetFrameworkVersion, profile);
            }

            return result;
        }

        private static readonly char[] CommaSeparator = [','];

        private static string[] GetParts(string targetPlatformMoniker)
        {
            return targetPlatformMoniker.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        }

        /// <summary>
        /// Creates a NuGetFramework from a .NET FrameworkName
        /// </summary>
        public static NuGetFramework ParseFrameworkName(string frameworkName, IFrameworkNameProvider mappings)
        {
            if (frameworkName == null) throw new ArgumentNullException(nameof(frameworkName));
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));

            string[] parts = frameworkName.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

            // if the first part is a special framework, ignore the rest
            if (!TryParseSpecialFramework(parts[0], out NuGetFramework? result))
            {
                ParseFrameworkNameParts(mappings, parts, out string? framework, out Version version, out string? profile);

                if (version.Major >= 5
                    && StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.NetCoreApp, framework))
                {
                    result = new NuGetFramework(framework, version, string.Empty, FrameworkConstants.EmptyVersion);
                }
                else
                {
                    result = new NuGetFramework(framework, version, profile);
                }
            }

            return result;
        }

        private static void ParseFrameworkNameParts(IFrameworkNameProvider mappings, string[] parts, out string framework, out Version version, out string? profile)
        {
            framework = mappings.TryGetIdentifier(parts[0], out string? mappedFramework)
                ? mappedFramework
                : parts[0];

            version = new Version(0, 0);
            profile = null;
            var versionPart = SingleOrDefaultSafe(parts.Where(s => s.IndexOf("Version=", StringComparison.OrdinalIgnoreCase) == 0));
            var profilePart = SingleOrDefaultSafe(parts.Where(s => s.IndexOf("Profile=", StringComparison.OrdinalIgnoreCase) == 0));
            if (!string.IsNullOrEmpty(versionPart))
            {
                var versionString = versionPart!.Split('=')[1].TrimStart('v');

                if (versionString.IndexOf('.') < 0)
                {
                    versionString += ".0";
                }

                version = Version.TryParse(versionString, out Version? parsedVersion)
                    ? parsedVersion
                    : throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.InvalidFrameworkVersion,
                        versionString));
            }

            if (!string.IsNullOrEmpty(profilePart))
            {
                profile = profilePart!.Split('=')[1];
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.Portable, framework)
                && !string.IsNullOrEmpty(profile)
                && profile!.Contains("-"))
            {
                // Frameworks within the portable profile are not allowed
                // to have profiles themselves #1869
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.InvalidPortableFrameworksDueToHyphen,
                    profile));
            }
        }

        private static void ParsePlatformParts(string[] parts, out string targetPlatformIdentifier, out Version platformVersion)
        {
            targetPlatformIdentifier = parts[0];
            platformVersion = new Version(0, 0);
            var versionPart = SingleOrDefaultSafe(parts.Where(s => s.IndexOf("Version=", StringComparison.OrdinalIgnoreCase) == 0));
            if (!string.IsNullOrEmpty(versionPart))
            {
                var versionString = versionPart!.Split('=')[1].TrimStart('v');

                if (versionString.IndexOf('.') < 0)
                {
                    versionString += ".0";
                }

                platformVersion = Version.TryParse(versionString, out Version? parsedVersion)
                    ? parsedVersion
                    : throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.InvalidPlatformVersion,
                        versionString));
            }
        }

        /// <summary>
        /// Creates a NuGetFramework from a folder name using the default mappings.
        /// </summary>
        public static NuGetFramework ParseFolder(string folderName)
        {
            return ParseFolder(folderName, DefaultFrameworkNameProvider.Instance);
        }

        /// <summary>
        /// Creates a NuGetFramework from a folder name using the given mappings.
        /// </summary>
        public static NuGetFramework ParseFolder(string folderName, IFrameworkNameProvider mappings)
        {
            if (folderName == null)
            {
                throw new ArgumentNullException(nameof(folderName));
            }

            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }

            if (folderName.IndexOf('%') > -1)
            {
                folderName = Uri.UnescapeDataString(folderName);
            }

            NuGetFramework? result;
            // first check if we have a special or common framework
            if (!TryParseSpecialFramework(folderName, out result)
                && !TryParseCommonFramework(folderName, out result))
            {
                // assume this is unsupported unless we find a match
                result = UnsupportedFramework;

                var parts = RawParse(folderName);

                if (parts != null)
                {
                    if (mappings.TryGetIdentifier(parts.Item1, out string? framework))
                    {
                        var version = FrameworkConstants.EmptyVersion;

                        if (parts.Item2 == null
                            || mappings.TryGetVersion(parts.Item2, out version))
                        {
                            string profileShort = parts.Item3;

                            if (version.Major >= 5
                                && (StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.Net, framework)
                                    || StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.NetCoreApp, framework)
                                   )
                                )
                            {
                                // net should be treated as netcoreapp in 5.0 and later
                                framework = FrameworkConstants.FrameworkIdentifiers.NetCoreApp;
                                if (!string.IsNullOrEmpty(profileShort))
                                {
                                    // Find a platform version if it exists and yank it out
                                    var platformChars = profileShort;
                                    var versionStart = 0;
                                    while (versionStart < platformChars.Length
                                           && IsLetterOrDot(platformChars[versionStart]))
                                    {
                                        versionStart++;
                                    }
                                    string platform = versionStart > 0 ? profileShort.Substring(0, versionStart) : profileShort;
                                    string? platformVersionString = versionStart > 0 ? profileShort.Substring(versionStart, profileShort.Length - versionStart) : null;

                                    // Parse the version if it's there.
                                    Version? platformVersion = FrameworkConstants.EmptyVersion;
                                    if ((string.IsNullOrEmpty(platformVersionString) || mappings.TryGetPlatformVersion(platformVersionString!, out platformVersion)))
                                    {
                                        result = new NuGetFramework(framework, version, platform ?? string.Empty, platformVersion ?? FrameworkConstants.EmptyVersion);
                                    }
                                    else
                                    {
                                        return result; // with result == UnsupportedFramework
                                    }
                                }
                                else
                                {
                                    result = new NuGetFramework(framework, version, string.Empty, FrameworkConstants.EmptyVersion);
                                }
                            }
                            else
                            {
                                if (!mappings.TryGetProfile(framework, profileShort, out string? profile))
                                {
                                    profile = profileShort ?? string.Empty;
                                }

                                if (StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.Portable, framework))
                                {
                                    if (!mappings.TryGetPortableFrameworks(profileShort!, out IEnumerable<NuGetFramework>? clientFrameworks))
                                    {
                                        result = UnsupportedFramework;
                                    }
                                    else
                                    {
                                        var profileNumber = -1;
                                        if (mappings.TryGetPortableProfile(clientFrameworks, out profileNumber))
                                        {
                                            var portableProfileNumber = FrameworkNameHelpers.GetPortableProfileNumberString(profileNumber);
                                            result = new NuGetFramework(framework, version, portableProfileNumber);
                                        }
                                        else
                                        {
                                            result = new NuGetFramework(framework, version, profileShort);
                                        }
                                    }
                                }
                                else
                                {
                                    result = new NuGetFramework(framework, version, profile);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If the framework was not recognized check if it is a deprecated framework
                    if (TryParseDeprecatedFramework(folderName, out NuGetFramework? deprecated))
                    {
                        result = deprecated;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Attempt to parse a common but deprecated framework using an exact string match
        /// Support for these should be dropped as soon as possible.
        /// </summary>
        private static bool TryParseDeprecatedFramework(string s, [NotNullWhen(true)] out NuGetFramework? framework)
        {
            framework = null;

            switch (s)
            {
                case "45":
                case "4.5":
                    framework = FrameworkConstants.CommonFrameworks.Net45;
                    break;
                case "40":
                case "4.0":
                case "4":
                    framework = FrameworkConstants.CommonFrameworks.Net4;
                    break;
                case "35":
                case "3.5":
                    framework = FrameworkConstants.CommonFrameworks.Net35;
                    break;
                case "20":
                case "2":
                case "2.0":
                    framework = FrameworkConstants.CommonFrameworks.Net2;
                    break;
            }

            return framework != null;
        }

        private static Tuple<string, string?, string>? RawParse(string s)
        {
            string identifier;
            var profile = string.Empty;
            string? version = null;

            var chars = s.ToCharArray();

            var versionStart = 0;

            while (versionStart < chars.Length
                   && IsLetterOrDot(chars[versionStart]))
            {
                versionStart++;
            }

            if (versionStart > 0)
            {
                identifier = s.Substring(0, versionStart);
            }
            else
            {
                // invalid, we no longer support names like: 40
                return null;
            }

            var profileStart = versionStart;

            while (profileStart < chars.Length
                   && IsDigitOrDot(chars[profileStart]))
            {
                profileStart++;
            }

            var versionLength = profileStart - versionStart;

            if (versionLength > 0)
            {
                version = s.Substring(versionStart, versionLength);
            }

            if (profileStart < chars.Length)
            {
                if (chars[profileStart] == '-')
                {
                    var actualProfileStart = profileStart + 1;

                    if (actualProfileStart == chars.Length)
                    {
                        // empty profiles are not allowed
                        return null;
                    }

                    profile = s.Substring(actualProfileStart, s.Length - actualProfileStart);

                    foreach (var c in profile.ToArray())
                    {
                        // validate the profile string to AZaz09-+.
                        if (!IsValidProfileChar(c))
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    // invalid profile
                    return null;
                }
            }

            return new Tuple<string, string?, string>(identifier, version, profile);
        }

        private static bool IsLetterOrDot(char c)
        {
            var x = (int)c;

            // "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
            return (x >= 65 && x <= 90) || (x >= 97 && x <= 122) || x == 46;
        }

        private static bool IsDigitOrDot(char c)
        {
            var x = (int)c;

            // "0123456789"
            return (x >= 48 && x <= 57) || x == 46;
        }

        private static bool IsValidProfileChar(char c)
        {
            var x = (int)c;

            // letter, digit, dot, dash, or plus
            return (x >= 48 && x <= 57) || (x >= 65 && x <= 90) || (x >= 97 && x <= 122) || x == 46 || x == 43 || x == 45;
        }

        private static bool TryParseSpecialFramework(string frameworkString, [NotNullWhen(true)] out NuGetFramework? framework)
        {
            framework = null;

            if (StringComparer.OrdinalIgnoreCase.Equals(frameworkString, FrameworkConstants.SpecialIdentifiers.Any))
            {
                framework = AnyFramework;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(frameworkString, FrameworkConstants.SpecialIdentifiers.Agnostic))
            {
                framework = AgnosticFramework;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(frameworkString, FrameworkConstants.SpecialIdentifiers.Unsupported))
            {
                framework = UnsupportedFramework;
            }

            return framework != null;
        }

        /// <summary>
        /// A set of special and common frameworks that can be returned from the list of constants without parsing
        /// Using the interned frameworks here optimizes comparisons since they can be checked by reference.
        /// This is designed to optimize
        /// </summary>
        private static bool TryParseCommonFramework(string frameworkString, [NotNullWhen(true)] out NuGetFramework? framework)
        {
            framework = null;

            frameworkString = frameworkString.ToLowerInvariant();

            switch (frameworkString)
            {
                case "dotnet":
                case "dotnet50":
                case "dotnet5.0":
                    framework = FrameworkConstants.CommonFrameworks.DotNet50;
                    break;
                case "net40":
                case "net4":
                    framework = FrameworkConstants.CommonFrameworks.Net4;
                    break;
                case "net45":
                    framework = FrameworkConstants.CommonFrameworks.Net45;
                    break;
                case "net451":
                    framework = FrameworkConstants.CommonFrameworks.Net451;
                    break;
                case "net46":
                    framework = FrameworkConstants.CommonFrameworks.Net46;
                    break;
                case "net461":
                    framework = FrameworkConstants.CommonFrameworks.Net461;
                    break;
                case "net462":
                    framework = FrameworkConstants.CommonFrameworks.Net462;
                    break;
                case "net47":
                    framework = FrameworkConstants.CommonFrameworks.Net47;
                    break;
                case "net471":
                    framework = FrameworkConstants.CommonFrameworks.Net471;
                    break;
                case "net472":
                    framework = FrameworkConstants.CommonFrameworks.Net472;
                    break;
                case "win8":
                    framework = FrameworkConstants.CommonFrameworks.Win8;
                    break;
                case "win81":
                    framework = FrameworkConstants.CommonFrameworks.Win81;
                    break;
                case "netstandard":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard;
                    break;
                case "netstandard1.0":
                case "netstandard10":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard10;
                    break;
                case "netstandard1.1":
                case "netstandard11":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard11;
                    break;
                case "netstandard1.2":
                case "netstandard12":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard12;
                    break;
                case "netstandard1.3":
                case "netstandard13":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard13;
                    break;
                case "netstandard1.4":
                case "netstandard14":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard14;
                    break;
                case "netstandard1.5":
                case "netstandard15":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard15;
                    break;
                case "netstandard1.6":
                case "netstandard16":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard16;
                    break;
                case "netstandard1.7":
                case "netstandard17":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard17;
                    break;
                case "netstandard2.0":
                case "netstandard20":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard20;
                    break;
                case "netstandard2.1":
                case "netstandard21":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard21;
                    break;
                case "netcoreapp2.1":
                case "netcoreapp21":
                    framework = FrameworkConstants.CommonFrameworks.NetCoreApp21;
                    break;
                case "netcoreapp3.0":
                case "netcoreapp30":
                    framework = FrameworkConstants.CommonFrameworks.NetCoreApp30;
                    break;
                case "netcoreapp3.1":
                case "netcoreapp31":
                    framework = FrameworkConstants.CommonFrameworks.NetCoreApp31;
                    break;
                case "netcoreapp5.0":
                case "netcoreapp50":
                case "net5.0":
                case "net50":
                    framework = FrameworkConstants.CommonFrameworks.Net50;
                    break;
                case "netcoreapp6.0":
                case "netcoreapp60":
                case "net6.0":
                case "net60":
                    framework = FrameworkConstants.CommonFrameworks.Net60;
                    break;
                case "netcoreapp7.0":
                case "netcoreapp70":
                case "net7.0":
                case "net70":
                    framework = FrameworkConstants.CommonFrameworks.Net70;
                    break;
            }

            return framework != null;
        }

        private static string? SingleOrDefaultSafe(IEnumerable<string> items)
        {
            if (items.Count() == 1)
            {
                return items.Single();
            }

            return null;
        }
    }
}
