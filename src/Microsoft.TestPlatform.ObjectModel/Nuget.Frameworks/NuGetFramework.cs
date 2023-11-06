// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NuGetClone.Frameworks
{
    /// <summary>
    /// A portable implementation of the .NET FrameworkName type with added support for NuGet folder names.
    /// </summary>
    internal partial class NuGetFramework : IEquatable<NuGetFramework>
    {
        private readonly string _frameworkIdentifier;
        private readonly Version _frameworkVersion;
        private readonly string _frameworkProfile;
        private string? _targetFrameworkMoniker;
        private string? _targetPlatformMoniker;
        private int? _hashCode;

        public NuGetFramework(NuGetFramework framework)
            : this(framework.Framework, framework.Version, framework.Profile, framework.Platform, framework.PlatformVersion)
        {
        }

        public NuGetFramework(string framework)
            : this(framework, FrameworkConstants.EmptyVersion)
        {
        }

        public NuGetFramework(string framework, Version version)
            : this(framework, version, string.Empty, FrameworkConstants.EmptyVersion)
        {
        }

        private const int Version5 = 5;

        /// <summary>
        /// Creates a new NuGetFramework instance, with an optional profile (only available for netframework)
        /// </summary>
        public NuGetFramework(string frameworkIdentifier, Version frameworkVersion, string? frameworkProfile)
            : this(frameworkIdentifier, frameworkVersion, profile: frameworkProfile ?? string.Empty, platform: string.Empty, platformVersion: FrameworkConstants.EmptyVersion)
        {
        }

        /// <summary>
        /// Creates a new NuGetFramework instance, with an optional platform and platformVersion (only available for net5.0+)
        /// </summary>
        public NuGetFramework(string frameworkIdentifier, Version frameworkVersion, string platform, Version platformVersion)
            : this(frameworkIdentifier, frameworkVersion, profile: string.Empty, platform: platform, platformVersion: platformVersion)
        {
        }

        internal NuGetFramework(string frameworkIdentifier, Version frameworkVersion, string profile, string platform, Version platformVersion)
        {
            if (frameworkIdentifier == null) throw new ArgumentNullException(nameof(frameworkIdentifier));
            if (frameworkVersion == null) throw new ArgumentNullException(nameof(frameworkVersion));
            if (platform == null) throw new ArgumentNullException(nameof(platform));
            if (platformVersion == null) throw new ArgumentNullException(nameof(platformVersion));

            _frameworkIdentifier = frameworkIdentifier;
            _frameworkVersion = NormalizeVersion(frameworkVersion);
            _frameworkProfile = profile;

            IsNet5Era = (_frameworkVersion.Major >= Version5 && StringComparer.OrdinalIgnoreCase.Equals(FrameworkConstants.FrameworkIdentifiers.NetCoreApp, _frameworkIdentifier));
            Platform = IsNet5Era ? platform : string.Empty;
            PlatformVersion = IsNet5Era ? NormalizeVersion(platformVersion) : FrameworkConstants.EmptyVersion;
        }

        /// <summary>
        /// Target framework
        /// </summary>
        public string Framework => _frameworkIdentifier;

        /// <summary>
        /// Target framework version
        /// </summary>
        public Version Version => _frameworkVersion;

        /// <summary>
        /// Framework Platform (net5.0+)
        /// </summary>
        public string Platform { get; }

        /// <summary>
        /// Framework Platform Version (net5.0+)
        /// </summary>
        public Version PlatformVersion { get; }

        /// <summary>
        /// True if the platform is non-empty
        /// </summary>
        public bool HasPlatform
        {
            get { return !string.IsNullOrEmpty(Platform); }
        }

        /// <summary>
        /// True if the profile is non-empty
        /// </summary>
        public bool HasProfile
        {
            get { return !string.IsNullOrEmpty(Profile); }
        }

        /// <summary>
        /// Target framework profile
        /// </summary>
        public string Profile => _frameworkProfile;

        /// <summary>The TargetFrameworkMoniker identifier of the current NuGetFramework.</summary>
        /// <remarks>Formatted to a System.Versioning.FrameworkName</remarks>
        public string DotNetFrameworkName
        {
            get
            {
                if (_targetFrameworkMoniker == null)
                {
                    _targetFrameworkMoniker = GetDotNetFrameworkName(DefaultFrameworkNameProvider.Instance);
                }
                return _targetFrameworkMoniker;
            }
        }

        /// <summary>The TargetFrameworkMoniker identifier of the current NuGetFramework.</summary>
        /// <remarks>Formatted to a System.Versioning.FrameworkName</remarks>
        public string GetDotNetFrameworkName(IFrameworkNameProvider mappings)
        {
            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }

            // Check for rewrites
            var framework = mappings.GetFullNameReplacement(this);

            if (framework.IsSpecificFramework)
            {
                var parts = new List<string>(3) { Framework };

                parts.Add(string.Format(CultureInfo.InvariantCulture, "Version=v{0}", GetDisplayVersion(framework.Version)));

                if (!string.IsNullOrEmpty(framework.Profile))
                {
                    parts.Add(string.Format(CultureInfo.InvariantCulture, "Profile={0}", framework.Profile));
                }

                return string.Join(",", parts);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "{0},Version=v0.0", framework.Framework);
            }
        }

        /// <summary>The TargetPlatformMoniker identifier of the current NuGetFramework.</summary>
        /// <remarks>Similar to a System.Versioning.FrameworkName, but missing the v at the beginning of the version.</remarks>
        public string DotNetPlatformName
        {
            get
            {
                if (_targetPlatformMoniker == null)
                {
                    _targetPlatformMoniker = string.IsNullOrEmpty(Platform)
                        ? string.Empty
                        : Platform + ",Version=" + GetDisplayVersion(PlatformVersion);
                }

                return _targetPlatformMoniker;
            }
        }

        /// <summary>
        /// Creates the shortened version of the framework using the default mappings.
        /// Ex: net45
        /// </summary>
        public string GetShortFolderName()
        {
            return GetShortFolderName(DefaultFrameworkNameProvider.Instance);
        }

        /// <summary>
        /// Helper that is .NET 5 Era aware to replace identifier when appropriate
        /// </summary>
        private string GetFrameworkIdentifier()
        {
            return IsNet5Era ? FrameworkConstants.FrameworkIdentifiers.Net : Framework;
        }

        /// <summary>
        /// Creates the shortened version of the framework using the given mappings.
        /// </summary>
        public virtual string GetShortFolderName(IFrameworkNameProvider mappings)
        {
            // Check for rewrites
            var framework = mappings.GetShortNameReplacement(this);

            var sb = new StringBuilder();

            if (IsSpecificFramework)
            {
                var shortFramework = string.Empty;

                // get the framework
                if (!mappings.TryGetShortIdentifier(
                    GetFrameworkIdentifier(),
                    out shortFramework))
                {
                    shortFramework = GetLettersAndDigitsOnly(framework.Framework);
                }

                if (string.IsNullOrEmpty(shortFramework))
                {
                    throw new FrameworkException(string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.InvalidFrameworkIdentifier,
                        shortFramework));
                }

                // add framework
                sb.Append(shortFramework);

                // add the version if it is non-empty
                if (!AllFrameworkVersions)
                {
                    sb.Append(mappings.GetVersionString(framework.Framework, framework.Version));
                }

                if (IsPCL)
                {
                    sb.Append("-");

                    if (framework.HasProfile
                        && mappings.TryGetPortableFrameworks(framework.Profile, includeOptional: false, out IEnumerable<NuGetFramework>? frameworks)
                        && frameworks.Any())
                    {
                        var required = new HashSet<NuGetFramework>(frameworks, Comparer);

                        // Normalize by removing all optional frameworks
                        mappings.TryGetPortableFrameworks(framework.Profile, includeOptional: false, out frameworks);

                        // sort the PCL frameworks by alphabetical order
                        var sortedFrameworks = required.Select(e => e.GetShortFolderName(mappings)).OrderBy(e => e, StringComparer.OrdinalIgnoreCase);

                        sb.Append(string.Join("+", sortedFrameworks));
                    }
                    else
                    {
                        throw new FrameworkException(string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.MissingPortableFrameworks,
                            framework.DotNetFrameworkName));
                    }
                }
                else if (IsNet5Era)
                {
                    if (!string.IsNullOrEmpty(framework.Platform))
                    {
                        sb.Append("-");
                        sb.Append(framework.Platform.ToLowerInvariant());

                        if (framework.PlatformVersion != FrameworkConstants.EmptyVersion)
                        {
                            sb.Append(mappings.GetVersionString(framework.Framework, framework.PlatformVersion));
                        }
                    }
                }
                else
                {
                    // add the profile
                    var shortProfile = string.Empty;

                    if (framework.HasProfile && !mappings.TryGetShortProfile(framework.Framework, framework.Profile, out shortProfile))
                    {
                        // if we have a profile, but can't get a mapping, just use the profile as is
                        shortProfile = framework.Profile;
                    }

                    if (!string.IsNullOrEmpty(shortProfile))
                    {
                        sb.Append("-");
                        sb.Append(shortProfile);
                    }
                }
            }
            else
            {
                // unsupported, any, agnostic
                sb.Append(Framework);
            }

            return sb.ToString().ToLowerInvariant();
        }

        private static string GetDisplayVersion(Version version)
        {
            var sb = new StringBuilder(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor));

            if (version.Build > 0
                || version.Revision > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ".{0}", version.Build);

                if (version.Revision > 0)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ".{0}", version.Revision);
                }
            }

            return sb.ToString();
        }

        private static string GetLettersAndDigitsOnly(string s)
        {
            var sb = new StringBuilder();

            foreach (var c in s.ToCharArray())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Portable class library check
        /// </summary>
        public bool IsPCL
        {
            get { return StringComparer.OrdinalIgnoreCase.Equals(Framework, FrameworkConstants.FrameworkIdentifiers.Portable) && Version.Major < 5; }
        }

        /// <summary>
        /// True if the framework is packages based.
        /// Ex: dotnet, dnxcore, netcoreapp, netstandard, uap, netcore50
        /// </summary>
        public bool IsPackageBased
        {
            get
            {
                // For these frameworks all versions are packages based.
                if (PackagesBased.Contains(Framework))
                {
                    return true;
                }

                // NetCore 5.0 and up are packages based.
                // Everything else is not packages based.
                return NuGetFrameworkUtility.IsNetCore50AndUp(this);
            }
        }

        /// <summary>
        /// True if this framework matches for all versions.
        /// Ex: net
        /// </summary>
        public bool AllFrameworkVersions
        {
            get { return Version.Major == 0 && Version.Minor == 0 && Version.Build == 0 && Version.Revision == 0; }
        }

        /// <summary>
        /// True if this framework was invalid or unknown. This framework is only compatible with Any and Agnostic.
        /// </summary>
        public bool IsUnsupported
        {
            get { return UnsupportedFramework.Equals(this); }
        }

        /// <summary>
        /// True if this framework is non-specific. Always compatible.
        /// </summary>
        public bool IsAgnostic
        {
            get { return AgnosticFramework.Equals(this); }
        }

        /// <summary>
        /// True if this is the any framework. Always compatible.
        /// </summary>
        public bool IsAny
        {
            get { return AnyFramework.Equals(this); }
        }

        /// <summary>
        /// True if this framework is real and not one of the special identifiers.
        /// </summary>
        public bool IsSpecificFramework
        {
            get { return !IsAgnostic && !IsAny && !IsUnsupported; }
        }

        /// <summary>
        /// True if this framework is Net5 or later, until we invent something new.
        /// </summary>
        internal bool IsNet5Era { get; private set; }

        /// <summary>
        /// Full framework comparison of the identifier, version, profile, platform, and platform version
        /// </summary>
        public static readonly IEqualityComparer<NuGetFramework> Comparer = NuGetFrameworkFullComparer.Instance;

        /// <summary>
        /// Framework name only comparison.
        /// </summary>
        public static readonly IEqualityComparer<NuGetFramework> FrameworkNameComparer = NuGetFrameworkNameComparer.Instance;

        public override string ToString()
        {
            return IsNet5Era
                ? GetShortFolderName()
                : DotNetFrameworkName;
        }

        public bool Equals(NuGetFramework? other)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            // Nullable annotations were added to the BCL for IEqualityComparer in .NET 5
            return Comparer.Equals(this, other);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        public static bool operator ==(NuGetFramework? left, NuGetFramework? right)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            // Nullable annotations were added to the BCL for IEqualityComparer in .NET 5
            return Comparer.Equals(left, right);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        public static bool operator !=(NuGetFramework? left, NuGetFramework? right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            if (_hashCode == null)
            {
                _hashCode = Comparer.GetHashCode(this);
            }

            return _hashCode.Value;
        }

        public override bool Equals(object? obj)
        {
            var other = obj as NuGetFramework;

            if (other != null)
            {
                return Equals(other);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        private static Version NormalizeVersion(Version version)
        {
            var normalized = version;

            if (version.Build < 0
                || version.Revision < 0)
            {
                normalized = new Version(
                    version.Major,
                    version.Minor,
                    Math.Max(version.Build, 0),
                    Math.Max(version.Revision, 0));
            }

            return normalized;
        }

        /// <summary>
        /// Frameworks that are packages based across all versions.
        /// </summary>
        private static readonly SortedSet<string> PackagesBased = new(
            new[]
            {
                        FrameworkConstants.FrameworkIdentifiers.DnxCore,
                        FrameworkConstants.FrameworkIdentifiers.NetPlatform,
                        FrameworkConstants.FrameworkIdentifiers.NetStandard,
                        FrameworkConstants.FrameworkIdentifiers.NetStandardApp,
                        FrameworkConstants.FrameworkIdentifiers.NetCoreApp,
                        FrameworkConstants.FrameworkIdentifiers.UAP,
                        FrameworkConstants.FrameworkIdentifiers.Tizen,
            },
            StringComparer.OrdinalIgnoreCase);
    }
}
