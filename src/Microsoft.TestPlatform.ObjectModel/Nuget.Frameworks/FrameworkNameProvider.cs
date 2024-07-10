// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NuGetClone.Frameworks
{
    internal class FrameworkNameProvider : IFrameworkNameProvider
    {
        private static readonly HashSet<NuGetFramework> EmptyFrameworkSet = new();

        /// <summary>
        /// Legacy frameworks that are allowed to have a single digit for the version number.
        /// </summary>
        private static readonly HashSet<string> SingleDigitVersionFrameworks = new(StringComparer.OrdinalIgnoreCase)
        {
            FrameworkConstants.FrameworkIdentifiers.Windows,
            FrameworkConstants.FrameworkIdentifiers.WindowsPhone,
            FrameworkConstants.FrameworkIdentifiers.Silverlight
        };

        /// <summary>
        /// Frameworks that must always include a decimal point (period) between numerical parts.
        /// </summary>
        private static readonly HashSet<string> DecimalPointFrameworks = new(StringComparer.OrdinalIgnoreCase)
        {
            FrameworkConstants.FrameworkIdentifiers.NetCoreApp,
            FrameworkConstants.FrameworkIdentifiers.NetStandard,
            FrameworkConstants.FrameworkIdentifiers.NanoFramework
        };

        /// <summary>
        /// Contains identifier -> identifier
        /// Ex: .NET Framework -> .NET Framework
        /// Ex: NET Framework -> .NET Framework
        /// This includes self mappings.
        /// </summary>
        private readonly Dictionary<string, string> _identifierSynonyms;

        private readonly Dictionary<string, string> _identifierToShortName;
        private readonly Dictionary<string, string> _profilesToShortName;
        private readonly Dictionary<string, string> _identifierShortToLong;
        private readonly Dictionary<string, string> _profileShortToLong;

        // profile -> supported frameworks, optional frameworks
        private readonly Dictionary<int, HashSet<NuGetFramework>> _portableFrameworks;
        private readonly Dictionary<int, HashSet<NuGetFramework>> _portableOptionalFrameworks;

        // PCL compatibility mappings
        private readonly Dictionary<int, HashSet<FrameworkRange>> _portableCompatibilityMappings;

        // equivalent frameworks
        private readonly Dictionary<NuGetFramework, HashSet<NuGetFramework>> _equivalentFrameworks;

        // equivalent profiles
        private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _equivalentProfiles;

        // non-PCL compatibility mappings
        private readonly Dictionary<string, HashSet<OneWayCompatibilityMappingEntry>> _compatibilityMappings;

        // subsets, net -> netcore
        private readonly Dictionary<string, HashSet<string>> _subSetFrameworks;

        // framework ordering (for non-package based frameworks)
        private readonly Dictionary<string, int> _nonPackageBasedFrameworkPrecedence;

        // framework ordering (for package based frameworks)
        private readonly Dictionary<string, int> _packageBasedFrameworkPrecedence;

        // framework ordering (when choosing between equivalent frameworks)
        private readonly Dictionary<string, int> _equivalentFrameworkPrecedence;

        // Rewrite mappings
        private readonly Dictionary<NuGetFramework, NuGetFramework> _shortNameRewrites;
        private readonly Dictionary<NuGetFramework, NuGetFramework> _fullNameRewrites;

        // NetStandard information
        private readonly List<NuGetFramework> _netStandardVersions;
        private readonly List<NuGetFramework> _compatibleCandidates;

        public FrameworkNameProvider(IEnumerable<IFrameworkMappings>? mappings, IEnumerable<IPortableFrameworkMappings>? portableMappings)
        {
            _identifierSynonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _identifierToShortName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _profilesToShortName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _identifierShortToLong = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _profileShortToLong = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _portableFrameworks = new Dictionary<int, HashSet<NuGetFramework>>();
            _portableOptionalFrameworks = new Dictionary<int, HashSet<NuGetFramework>>();
            _equivalentFrameworks = new Dictionary<NuGetFramework, HashSet<NuGetFramework>>();
            _equivalentProfiles = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
            _subSetFrameworks = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            _nonPackageBasedFrameworkPrecedence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _packageBasedFrameworkPrecedence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _equivalentFrameworkPrecedence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _compatibilityMappings = new Dictionary<string, HashSet<OneWayCompatibilityMappingEntry>>(StringComparer.OrdinalIgnoreCase);
            _portableCompatibilityMappings = new Dictionary<int, HashSet<FrameworkRange>>();
            _shortNameRewrites = new Dictionary<NuGetFramework, NuGetFramework>();
            _fullNameRewrites = new Dictionary<NuGetFramework, NuGetFramework>();
            _netStandardVersions = new List<NuGetFramework>();
            _compatibleCandidates = new List<NuGetFramework>();

            InitMappings(mappings);

            InitPortableMappings(portableMappings);

            InitNetStandard();
        }

        /// <summary>
        /// Converts a key using the mappings, or if the key is already converted, finds the normalized form.
        /// </summary>
        private static bool TryConvertOrNormalize(string key, IDictionary<string, string> mappings, IDictionary<string, string> reverse, [NotNullWhen(true)] out string? value)
        {
            if (mappings.TryGetValue(key, out value))
            {
                return true;
            }
            else if (reverse.ContainsKey(key))
            {
                value = reverse.Where(p => StringComparer.OrdinalIgnoreCase.Equals(p.Key, key)).Select(s => s.Key).Single();
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetIdentifier(string framework, [NotNullWhen(true)] out string? identifier)
        {
            return TryConvertOrNormalize(framework, _identifierSynonyms, _identifierToShortName, out identifier);
        }

        public bool TryGetProfile(string frameworkIdentifier, string profileShortName, [NotNullWhen(true)] out string? profile)
        {
            return TryConvertOrNormalize(profileShortName, _profileShortToLong, _profilesToShortName, out profile);
        }

        public bool TryGetShortIdentifier(string identifier, [NotNullWhen(true)] out string? identifierShortName)
        {
            return TryConvertOrNormalize(identifier, _identifierToShortName, _identifierShortToLong, out identifierShortName);
        }

        public bool TryGetShortProfile(string frameworkIdentifier, string profile, [NotNullWhen(true)] out string? profileShortName)
        {
            return TryConvertOrNormalize(profile, _profilesToShortName, _profileShortToLong, out profileShortName);
        }

        public bool TryGetVersion(string versionString, [NotNullWhen(true)] out Version? version)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                version = null;
                return false;
            }
            else
            {
                if (versionString.IndexOf('.') > -1)
                {
                    // parse the version as a normal dot delimited version
                    return Version.TryParse(versionString, out version);
                }
                else
                {
                    // make sure we have at least 2 digits
                    if (versionString.Length < 2)
                    {
                        versionString += "0";
                    }

                    // take only the first 4 digits and add dots
                    // 451 -> 4.5.1
                    // 81233 -> 8123
                    return Version.TryParse(string.Join(".", versionString.ToCharArray().Take(4)), out version);
                }
            }
        }

        public bool TryGetPlatformVersion(string versionString, [NotNullWhen(true)] out Version? version)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                version = null;
                return false;
            }
            else
            {
                if (versionString.IndexOf('.') < 0)
                {
                    versionString += ".0";
                }
                return Version.TryParse(versionString, out version);
            }
        }

        public string GetVersionString(string framework, Version version)
        {
            if (version is null || IsZero(version))
            {
                return string.Empty;
            }

            int major = version.Major > 0 ? version.Major : 0;
            int minor = version.Minor > 0 ? version.Minor : 0;
            int build = version.Build > 0 ? version.Build : 0;
            int revision = version.Revision > 0 ? version.Revision : 0;

            // Remove all trailing zeros beyond the minor version.
            int partCount = (minor == 0, build == 0, revision == 0) switch
            {
                (true, true, true) => 1,
                (false, true, true) => 2,
                (_, false, true) => 3,
                (_, _, false) => 4
            };

            // Only some legacy frameworks are allowed to have one part in their version.
            if (partCount == 1 && !SingleDigitVersionFrameworks.Contains(framework))
            {
                partCount = 2;
            }

            StringBuilder sb = StringBuilderPool.Shared.Rent(256);

            // Some frameworks require a decimal point between parts.
            // If any part is greater than 9 (requiring multiple digits), we add decimal points.
            if (DecimalPointFrameworks.Contains(framework) || HasGreaterThanNinePart())
            {
                // An additional zero is needed for decimals.
                if (partCount == 1)
                    partCount = 2;

                sb.Append(major);
                if (partCount > 1)
                    sb.Append('.').Append(minor);
                if (partCount > 2)
                    sb.Append('.').Append(build);
                if (partCount > 3)
                    sb.Append('.').Append(revision);
            }
            else
            {
                sb.Append(major);
                if (partCount > 1)
                    sb.Append(minor);
                if (partCount > 2)
                    sb.Append(build);
                if (partCount > 3)
                    sb.Append(revision);
            }

            return StringBuilderPool.Shared.ToStringAndReturn(sb);

            bool HasGreaterThanNinePart()
            {
                return major > 9 || minor > 9 || build > 9 || revision > 9;
            }

            static bool IsZero(Version version)
            {
                // Build and Revision can be -1 when only major & minor are specified.
                // Out of caution, check all values for zero or less.
                return version.Major <= 0
                    && version.Minor <= 0
                    && version.Build <= 0
                    && version.Revision <= 0;
            }
        }

        public bool TryGetPortableProfile(IEnumerable<NuGetFramework> supportedFrameworks, out int profileNumber)
        {
            if (supportedFrameworks == null)
            {
                throw new ArgumentNullException(nameof(supportedFrameworks));
            }

            profileNumber = -1;

            // Remove duplicate frameworks, ex: win+win8 -> win
            var profileFrameworks = RemoveDuplicateFramework(supportedFrameworks);

            var reduced = new HashSet<NuGetFramework>();
            foreach (var pair in _portableFrameworks)
            {
                // to match the required set must be less than or the same count as the input
                // if we knew which frameworks were optional in the input we could rule out the lesser ones also
                if (pair.Value.Count <= profileFrameworks.Count)
                {
                    foreach (var curFw in profileFrameworks)
                    {
                        var isOptional = false;

                        foreach (var optional in GetOptionalFrameworks(pair.Key))
                        {
                            // TODO: profile check? Is the version check correct here?
                            if (NuGetFramework.FrameworkNameComparer.Equals(optional, curFw)
                                && StringComparer.OrdinalIgnoreCase.Equals(optional.Profile, curFw.Profile)
                                && curFw.Version >= optional.Version)
                            {
                                isOptional = true;
                            }
                        }

                        if (!isOptional)
                        {
                            reduced.Add(curFw);
                        }
                    }

                    // check all frameworks while taking into account equivalent variations
                    foreach (var permutation in GetEquivalentPermutations(pair.Value))
                    {
                        if (SetEquals(reduced, permutation))
                        {
                            // found a match
                            profileNumber = pair.Key;
                            return true;
                        }
                    }
                }

                reduced.Clear();
            }

            return false;
        }

        private HashSet<NuGetFramework> RemoveDuplicateFramework(IEnumerable<NuGetFramework> supportedFrameworks)
        {
            var result = new HashSet<NuGetFramework>();
            var existingFrameworks = new HashSet<NuGetFramework>();

            foreach (var framework in supportedFrameworks)
            {
                if (!existingFrameworks.Contains(framework))
                {
                    result.Add(framework);

                    // Add in the existing framework (included here) and all equivalent frameworks
                    var equivalentFrameworks = GetAllEquivalentFrameworks(framework);

                    UnionWith(existingFrameworks, equivalentFrameworks);
                }
            }

            return result;
        }

        /// <summary>
        /// Get all equivalent frameworks including the given framework
        /// </summary>
        private HashSet<NuGetFramework> GetAllEquivalentFrameworks(NuGetFramework framework)
        {
            // Loop through the frameworks, all frameworks that are not in results yet
            // will be added to toProcess to get the equivalent frameworks
            var toProcess = new Stack<NuGetFramework>();
            var results = new HashSet<NuGetFramework>();

            toProcess.Push(framework);
            results.Add(framework);

            while (toProcess.Count > 0)
            {
                var current = toProcess.Pop();

                if (_equivalentFrameworks.TryGetValue(current, out HashSet<NuGetFramework>? currentEquivalent))
                {
                    foreach (var equalFramework in currentEquivalent)
                    {
                        if (results.Add(equalFramework))
                        {
                            toProcess.Push(equalFramework);
                        }
                    }
                }
            }

            return results;
        }

        // find all combinations that are equivalent
        // ex: net4+win8 <-> net4+netcore45
        private IEnumerable<HashSet<NuGetFramework>> GetEquivalentPermutations(HashSet<NuGetFramework> frameworks)
        {
            if (frameworks.Count > 0)
            {
                NuGetFramework? current = null;
                var remaining = frameworks.Count == 1 ? null : new HashSet<NuGetFramework>();

                var isFirst = true;
                foreach (var fw in frameworks)
                {
                    if (isFirst)
                    {
                        current = fw;
                        isFirst = false;
                        continue;
                    }

                    remaining!.Add(fw);
                }

                var equalFrameworks = new HashSet<NuGetFramework>();
                // include ourselves
                equalFrameworks.Add(current!);

                // find all equivalent frameworks for the current one
                if (_equivalentFrameworks.TryGetValue(current!, out HashSet<NuGetFramework>? curFrameworks))
                {
                    UnionWith(equalFrameworks, curFrameworks);
                }

                foreach (var fw in equalFrameworks)
                {
                    if (remaining != null && remaining.Count > 0)
                    {
                        foreach (var result in GetEquivalentPermutations(remaining))
                        {
                            // work backwards adding the frameworks into the sets
                            result.Add(fw);
                            yield return result;
                        }
                    }
                    else
                    {
                        var singleFramework = new HashSet<NuGetFramework>();
                        singleFramework.Add(fw);
                        yield return singleFramework;
                    }
                }
            }

            yield break;
        }

        private HashSet<NuGetFramework> GetOptionalFrameworks(int profile)
        {
            if (_portableOptionalFrameworks.TryGetValue(profile, out HashSet<NuGetFramework>? frameworks))
            {
                return frameworks;
            }

            return EmptyFrameworkSet;
        }

        public bool TryGetPortableFrameworks(int profile, [NotNullWhen(true)] out IEnumerable<NuGetFramework>? frameworks)
        {
            return TryGetPortableFrameworks(profile, true, out frameworks);
        }

        public bool TryGetPortableFrameworks(int profile, bool includeOptional, [NotNullWhen(true)] out IEnumerable<NuGetFramework>? frameworks)
        {
            var result = new HashSet<NuGetFramework>();
            if (_portableFrameworks.TryGetValue(profile, out HashSet<NuGetFramework>? tmpFrameworks))
            {
                foreach (var fw in tmpFrameworks)
                {
                    result.Add(fw);
                }
            }

            if (includeOptional)
            {
                if (_portableOptionalFrameworks.TryGetValue(profile, out HashSet<NuGetFramework>? optional))
                {
                    foreach (var fw in optional)
                    {
                        result.Add(fw);
                    }
                }
            }

            frameworks = result;
            return result.Count > 0;
        }

        public bool TryGetPortableFrameworks(string shortPortableProfiles, [NotNullWhen(true)] out IEnumerable<NuGetFramework>? frameworks)
        {
            if (shortPortableProfiles == null)
            {
                throw new ArgumentNullException(nameof(shortPortableProfiles));
            }

            var shortNames = shortPortableProfiles.Split(['+'], StringSplitOptions.RemoveEmptyEntries);

            var result = new List<NuGetFramework>();
            foreach (var name in shortNames)
            {
                var framework = NuGetFramework.Parse(name, this);
                if (framework.HasProfile)
                {
                    // Frameworks within the portable profile are not allowed
                    // to have profiles themselves #1869
                    throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.InvalidPortableFrameworksDueToHyphen,
                        shortPortableProfiles));
                }

                result.Add(framework);
            }

            frameworks = result;
            return result.Count > 0;
        }

        public bool TryGetPortableCompatibilityMappings(int profile, [NotNullWhen(true)] out IEnumerable<FrameworkRange>? supportedFrameworkRanges)
        {
            if (_portableCompatibilityMappings.TryGetValue(profile, out HashSet<FrameworkRange>? entries))
            {
                supportedFrameworkRanges = entries;
                return supportedFrameworkRanges.Any();
            }

            supportedFrameworkRanges = null;
            return false;
        }

        public bool TryGetPortableProfileNumber(string profile, out int profileNumber)
        {
            // attempt to parse the profile for a number
            if (profile.StartsWith("Profile", StringComparison.OrdinalIgnoreCase))
            {
                var trimmed = profile.Substring(7, profile.Length - 7);
                return int.TryParse(trimmed, out profileNumber);
            }

            profileNumber = -1;
            return false;
        }

        public bool TryGetPortableFrameworks(string profile, bool includeOptional, [NotNullWhen(true)] out IEnumerable<NuGetFramework>? frameworks)
        {
            // attempt to parse the profile for a number
            int profileNum;
            if (TryGetPortableProfileNumber(profile, out profileNum))
            {
                if (TryGetPortableFrameworks(profileNum, includeOptional, out frameworks))
                {
                    return true;
                }

                frameworks = [];
                return false;
            }

            // treat the profile as a list of frameworks
            return TryGetPortableFrameworks(profile, out frameworks);
        }

        public bool TryGetEquivalentFrameworks(NuGetFramework framework, [NotNullWhen(true)] out IEnumerable<NuGetFramework>? frameworks)
        {
            var result = new HashSet<NuGetFramework>();

            // add in all framework aliases
            if (_equivalentFrameworks.TryGetValue(framework, out HashSet<NuGetFramework>? eqFrameworks))
            {
                foreach (var eqFw in eqFrameworks)
                {
                    result.Add(eqFw);
                }
            }

            var baseFrameworks = new List<NuGetFramework>(result);
            baseFrameworks.Add(framework);

            // add in all profile aliases
            foreach (var fw in baseFrameworks)
            {
                if (_equivalentProfiles.TryGetValue(fw.Framework, out Dictionary<string, HashSet<string>>? eqProfiles))
                {
                    if (eqProfiles.TryGetValue(fw.Profile, out HashSet<string>? matchingProfiles))
                    {
                        foreach (var eqProfile in matchingProfiles)
                        {
                            result.Add(new NuGetFramework(fw.Framework, fw.Version, eqProfile));
                        }
                    }
                }
            }

            // do not include the original framework
            result.Remove(framework);

            frameworks = result;
            return result.Count > 0;
        }

        public bool TryGetEquivalentFrameworks(FrameworkRange range, [NotNullWhen(true)] out IEnumerable<NuGetFramework>? frameworks)
        {
            if (range == null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            var relevant = new HashSet<NuGetFramework>();

            foreach (var framework in _equivalentFrameworks.Keys.Where(f => range.Satisfies(f)))
            {
                relevant.Add(framework);
            }

            var results = new HashSet<NuGetFramework>();

            foreach (var framework in relevant)
            {
                if (TryGetEquivalentFrameworks(framework, out IEnumerable<NuGetFramework>? values))
                {
                    foreach (var val in values)
                    {
                        results.Add(val);
                    }
                }
            }

            frameworks = results;
            return results.Count > 0;
        }

        private void InitMappings(IEnumerable<IFrameworkMappings>? mappings)
        {
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    // eq profiles
                    AddEquivalentProfiles(mapping.EquivalentProfiles);

                    // equivalent frameworks
                    AddEquivalentFrameworks(mapping.EquivalentFrameworks);

                    // add synonyms
                    AddFrameworkSynonyms(mapping.IdentifierSynonyms);

                    // populate short <-> long
                    AddIdentifierShortNames(mapping.IdentifierShortNames);

                    // official profile short names
                    AddProfileShortNames(mapping.ProfileShortNames);

                    // add compatibility mappings
                    AddCompatibilityMappings(mapping.CompatibilityMappings);

                    // add subset frameworks
                    AddSubSetFrameworks(mapping.SubSetFrameworks);

                    // add framework ordering rules
                    AddFrameworkPrecedenceMappings(_nonPackageBasedFrameworkPrecedence, mapping.NonPackageBasedFrameworkPrecedence);
                    AddFrameworkPrecedenceMappings(_packageBasedFrameworkPrecedence, mapping.PackageBasedFrameworkPrecedence);
                    AddFrameworkPrecedenceMappings(_equivalentFrameworkPrecedence, mapping.EquivalentFrameworkPrecedence);

                    // add rewrite rules
                    AddShortNameRewriteMappings(mapping.ShortNameReplacements);
                    AddFullNameRewriteMappings(mapping.FullNameReplacements);
                }
            }
        }

        private void InitPortableMappings(IEnumerable<IPortableFrameworkMappings>? portableMappings)
        {
            if (portableMappings != null)
            {
                foreach (var portableMapping in portableMappings)
                {
                    // populate portable framework names
                    AddPortableProfileMappings(portableMapping.ProfileFrameworks);

                    // populate portable optional frameworks
                    AddPortableOptionalFrameworks(portableMapping.ProfileOptionalFrameworks);

                    // populate portable compatibility mappings
                    AddPortableCompatibilityMappings(portableMapping.CompatibilityMappings);
                }
            }
        }

        private void InitNetStandard()
        {
            // populate the list of frameworks that could be compatible with NetStandard
            AddCompatibleCandidates();

            // populate the list of NetStandard versions
            AddNetStandardVersions();
        }

        private void AddShortNameRewriteMappings(IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> mappings)
        {
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (!_shortNameRewrites.ContainsKey(mapping.Key))
                    {
                        _shortNameRewrites.Add(mapping.Key, mapping.Value);
                    }
                }
            }
        }

        private void AddFullNameRewriteMappings(IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> mappings)
        {
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (!_fullNameRewrites.ContainsKey(mapping.Key))
                    {
                        _fullNameRewrites.Add(mapping.Key, mapping.Value);
                    }
                }
            }
        }

        private void AddCompatibilityMappings(IEnumerable<OneWayCompatibilityMappingEntry> mappings)
        {
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (!_compatibilityMappings.TryGetValue(mapping.TargetFrameworkRange.Min.Framework, out HashSet<OneWayCompatibilityMappingEntry>? entries))
                    {
                        entries = new HashSet<OneWayCompatibilityMappingEntry>(OneWayCompatibilityMappingEntry.Comparer);
                        _compatibilityMappings.Add(mapping.TargetFrameworkRange.Min.Framework, entries);
                    }

                    entries.Add(mapping);
                }
            }
        }

        private void AddSubSetFrameworks(IEnumerable<KeyValuePair<string, string>> mappings)
        {
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (!_subSetFrameworks.TryGetValue(mapping.Value, out HashSet<string>? subSets))
                    {
                        subSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        _subSetFrameworks.Add(mapping.Value, subSets);
                    }

                    subSets.Add(mapping.Key);
                }
            }
        }

        /// <summary>
        /// 2 way per framework profile equivalence
        /// </summary>
        /// <param name="mappings"></param>
        private void AddEquivalentProfiles(IEnumerable<FrameworkSpecificMapping> mappings)
        {
            if (mappings != null)
            {
                foreach (var profileMapping in mappings)
                {
                    var frameworkIdentifier = profileMapping.FrameworkIdentifier;
                    var profile1 = profileMapping.Mapping.Key;
                    var profile2 = profileMapping.Mapping.Value;

                    if (!_equivalentProfiles.TryGetValue(frameworkIdentifier, out Dictionary<string, HashSet<string>>? profileMappings))
                    {
                        profileMappings = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                        _equivalentProfiles.Add(frameworkIdentifier, profileMappings);
                    }

                    if (!profileMappings.TryGetValue(profile1, out HashSet<string>? innerMappings1))
                    {
                        innerMappings1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        profileMappings.Add(profile1, innerMappings1);
                    }

                    if (!profileMappings.TryGetValue(profile2, out HashSet<string>? innerMappings2))
                    {
                        innerMappings2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        profileMappings.Add(profile2, innerMappings2);
                    }

                    innerMappings1.Add(profile2);
                    innerMappings2.Add(profile1);
                }
            }
        }

        /// <summary>
        /// 2 way framework equivalence
        /// </summary>
        /// <param name="mappings"></param>
        private void AddEquivalentFrameworks(IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> mappings)
        {
            if (mappings != null)
            {
                foreach (var pair in mappings)
                {
                    var remaining = new Stack<NuGetFramework>();
                    remaining.Push(pair.Key);
                    remaining.Push(pair.Value);

                    var seen = new HashSet<NuGetFramework>();
                    while (remaining.Any())
                    {
                        var next = remaining.Pop();
                        if (!seen.Add(next))
                        {
                            continue;
                        }

                        if (!_equivalentFrameworks.TryGetValue(next, out HashSet<NuGetFramework>? eqFrameworks))
                        {
                            // initialize set
                            eqFrameworks = new HashSet<NuGetFramework>();
                            _equivalentFrameworks.Add(next, eqFrameworks);
                        }
                        else
                        {
                            // explore all equivalent
                            foreach (var framework in eqFrameworks)
                            {
                                remaining.Push(framework);
                            }
                        }
                    }

                    // add this equivalency rule, enforcing transitivity
                    foreach (var framework in seen)
                    {
                        foreach (var other in seen)
                        {
                            if (!NuGetFramework.Comparer.Equals(framework, other))
                            {
                                _equivalentFrameworks[framework].Add(other);
                            }
                        }
                    }

                }
            }
        }

        private void AddFrameworkSynonyms(IEnumerable<KeyValuePair<string, string>> mappings)
        {
            if (mappings != null)
            {
                foreach (var pair in mappings)
                {
                    if (!_identifierSynonyms.ContainsKey(pair.Key))
                    {
                        _identifierSynonyms.Add(pair.Key, pair.Value);
                    }
                }
            }
        }

        private void AddIdentifierShortNames(IEnumerable<KeyValuePair<string, string>> mappings)
        {
            if (mappings != null)
            {
                foreach (var pair in mappings)
                {
                    var shortName = pair.Value;
                    var longName = pair.Key;

                    if (!_identifierSynonyms.ContainsKey(pair.Value))
                    {
                        _identifierSynonyms.Add(pair.Value, pair.Key);
                    }

                    _identifierShortToLong.Add(shortName, longName);

                    _identifierToShortName.Add(longName, shortName);
                }
            }
        }

        private void AddProfileShortNames(IEnumerable<FrameworkSpecificMapping> mappings)
        {
            if (mappings != null)
            {
                foreach (var profileMapping in mappings)
                {
                    _profilesToShortName.Add(profileMapping.Mapping.Value, profileMapping.Mapping.Key);
                    _profileShortToLong.Add(profileMapping.Mapping.Key, profileMapping.Mapping.Value);
                }
            }
        }

        // Add supported frameworks for each portable profile number
        private void AddPortableProfileMappings(IEnumerable<KeyValuePair<int, NuGetFramework[]>> mappings)
        {
            if (mappings != null)
            {
                foreach (var pair in mappings)
                {
                    if (!_portableFrameworks.TryGetValue(pair.Key, out HashSet<NuGetFramework>? frameworks))
                    {
                        frameworks = new HashSet<NuGetFramework>();
                        _portableFrameworks.Add(pair.Key, frameworks);
                    }

                    foreach (var fw in pair.Value)
                    {
                        frameworks.Add(fw);
                    }
                }
            }
        }

        // Add optional frameworks for each portable profile number
        private void AddPortableOptionalFrameworks(IEnumerable<KeyValuePair<int, NuGetFramework[]>> mappings)
        {
            if (mappings != null)
            {
                foreach (var pair in mappings)
                {
                    if (!_portableOptionalFrameworks.TryGetValue(pair.Key, out HashSet<NuGetFramework>? frameworks))
                    {
                        frameworks = new HashSet<NuGetFramework>();
                        _portableOptionalFrameworks.Add(pair.Key, frameworks);
                    }

                    foreach (var fw in pair.Value)
                    {
                        frameworks.Add(fw);
                    }
                }
            }
        }

        private void AddPortableCompatibilityMappings(IEnumerable<KeyValuePair<int, FrameworkRange>> mappings)
        {
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (!_portableCompatibilityMappings.TryGetValue(mapping.Key, out HashSet<FrameworkRange>? entries))
                    {
                        entries = new HashSet<FrameworkRange>(FrameworkRangeComparer.Instance);
                        _portableCompatibilityMappings.Add(mapping.Key, entries);
                    }

                    entries.Add(mapping.Value);
                }
            }
        }

        // Ordered lists of framework identifiers
        public void AddFrameworkPrecedenceMappings(IDictionary<string, int> destination, IEnumerable<string> mappings)
        {
            if (mappings != null)
            {
                foreach (var framework in mappings)
                {
                    if (!destination.ContainsKey(framework))
                    {
                        destination.Add(framework, destination.Count);
                    }
                }
            }
        }

        public bool TryGetCompatibilityMappings(NuGetFramework framework, [NotNullWhen(true)] out IEnumerable<FrameworkRange>? supportedFrameworkRanges)
        {
            if (_compatibilityMappings.TryGetValue(framework.Framework, out HashSet<OneWayCompatibilityMappingEntry>? entries))
            {
                supportedFrameworkRanges = entries.Where(m => m.TargetFrameworkRange.Satisfies(framework)).Select(m => m.SupportedFrameworkRange);
                return supportedFrameworkRanges.Any();
            }

            supportedFrameworkRanges = null;
            return false;
        }

        public bool TryGetSubSetFrameworks(string frameworkIdentifier, [NotNullWhen(true)] out IEnumerable<string>? subSetFrameworks)
        {
            if (_subSetFrameworks.TryGetValue(frameworkIdentifier, out HashSet<string>? values))
            {
                subSetFrameworks = values;
                return true;
            }

            subSetFrameworks = null;
            return false;
        }

        public int CompareFrameworks(NuGetFramework? x, NuGetFramework? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            // For the purposes of this compare do not treat netcore50 as packages based
            var xPackagesBased = x.IsPackageBased && !NuGetFrameworkUtility.IsNetCore50AndUp(x);
            var yPackagesBased = y.IsPackageBased && !NuGetFrameworkUtility.IsNetCore50AndUp(y);

            if (xPackagesBased != yPackagesBased)
            {
                // non-package based always come before package based
                return xPackagesBased.CompareTo(yPackagesBased);
            }

            var precedence = xPackagesBased ? _packageBasedFrameworkPrecedence : _nonPackageBasedFrameworkPrecedence;

            return CompareUsingPrecedence(x, y, precedence);
        }

        public int CompareEquivalentFrameworks(NuGetFramework? x, NuGetFramework? y)
        {
            return CompareUsingPrecedence(x, y, _equivalentFrameworkPrecedence);
        }

        private static int CompareUsingPrecedence(NuGetFramework? x, NuGetFramework? y, Dictionary<string, int> precedence)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            if (StringComparer.OrdinalIgnoreCase.Equals(x.Framework, y.Framework))
            {
                return 0;
            }

            int xIndex;
            if (!precedence.TryGetValue(x.Framework, out xIndex))
            {
                xIndex = int.MaxValue;
            }

            int yIndex;
            if (!precedence.TryGetValue(y.Framework, out yIndex))
            {
                yIndex = int.MaxValue;
            }

            return xIndex.CompareTo(yIndex);
        }


        public NuGetFramework GetShortNameReplacement(NuGetFramework framework)
        {
            // Replace the framework name if a rewrite exists
            if (!_shortNameRewrites.TryGetValue(framework, out NuGetFramework? result))
            {
                result = framework;
            }

            return result;
        }

        public NuGetFramework GetFullNameReplacement(NuGetFramework framework)
        {
            // Replace the framework name if a rewrite exists
            if (!_fullNameRewrites.TryGetValue(framework, out NuGetFramework? result))
            {
                result = framework;
            }

            return result;
        }

        public IEnumerable<NuGetFramework> GetNetStandardVersions()
        {
            return _netStandardVersions.AsReadOnly();
        }

        public IEnumerable<NuGetFramework> GetCompatibleCandidates()
        {
            return _compatibleCandidates.AsReadOnly();
        }

        private void AddNetStandardVersions()
        {
            foreach (var framework in _compatibleCandidates)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(framework.Framework, FrameworkConstants.FrameworkIdentifiers.NetStandard))
                {
                    _netStandardVersions.Add(framework);
                }
            }

            _netStandardVersions.Sort(NuGetFrameworkSorter.Instance);
        }

        private void AddCompatibleCandidates()
        {
            var set = new HashSet<NuGetFramework>();

            // equivalent
            foreach (var framework in _equivalentFrameworks.Values.SelectMany(x => x))
            {
                set.Add(framework);
            }

            // compatible
            foreach (var mapping in _compatibilityMappings.SelectMany(p => p.Value))
            {
                set.Add(mapping.TargetFrameworkRange.Min);
                set.Add(mapping.TargetFrameworkRange.Max);
                set.Add(mapping.SupportedFrameworkRange.Min);
                set.Add(mapping.SupportedFrameworkRange.Max);
            }

            // portable compatible
            foreach (var pair in _portableCompatibilityMappings)
            {
                var portable = new NuGetFramework(
                    FrameworkConstants.FrameworkIdentifiers.Portable,
                    FrameworkConstants.EmptyVersion,
                    string.Format(NumberFormatInfo.InvariantInfo, "Profile{0}", pair.Key));

                set.Add(portable);
                foreach (var range in pair.Value)
                {
                    set.Add(range.Min);
                    set.Add(range.Max);
                }
            }

            // subset and superset
            var superSetFrameworks = _subSetFrameworks
                .SelectMany(p => p.Value.Select(subset => new { Superset = p.Key, Subset = subset }))
                .GroupBy(p => p.Subset, p => p.Superset, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => new HashSet<string>(g, StringComparer.OrdinalIgnoreCase));

            foreach (var framework in set.ToArray())
            {
                if (framework.HasProfile)
                {
                    continue;
                }

                if (_subSetFrameworks.TryGetValue(framework.Framework, out HashSet<string>? subset))
                {
                    foreach (var subFramework in subset)
                    {
                        set.Add(new NuGetFramework(subFramework, framework.Version, framework.Profile));
                    }
                }

                if (superSetFrameworks.TryGetValue(framework.Framework, out HashSet<string>? superset))
                {
                    foreach (var superFramework in superset)
                    {
                        set.Add(new NuGetFramework(superFramework, framework.Version, framework.Profile));
                    }
                }
            }

            _compatibleCandidates.AddRange(set);
            _compatibleCandidates.Sort(NuGetFrameworkSorter.Instance);
        }

        // Strong typed non-IEnumerator based HashSet functions
        private static bool SetEquals(HashSet<NuGetFramework> left, HashSet<NuGetFramework> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (var fw in left)
            {
                if (!right.Contains(fw))
                {
                    return false;
                }
            }

            return true;
        }

        private static void UnionWith(HashSet<NuGetFramework> toAccumulate, HashSet<NuGetFramework> toAdd)
        {
            foreach (var fw in toAdd)
            {
                toAccumulate.Add(fw);
            }
        }
    }
}
