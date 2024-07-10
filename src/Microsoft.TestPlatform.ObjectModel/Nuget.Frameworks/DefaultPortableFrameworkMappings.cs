// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace NuGetClone.Frameworks
{
    /// <summary>
    /// Contains the standard portable framework mappings
    /// </summary>
    internal class DefaultPortableFrameworkMappings : IPortableFrameworkMappings
    {

        private static readonly Lazy<KeyValuePair<int, NuGetFramework[]>[]> ProfileFrameworksLazy = new(() =>
        {
            var net4 = FrameworkConstants.CommonFrameworks.Net4;
            var net403 = FrameworkConstants.CommonFrameworks.Net403;
            var net45 = FrameworkConstants.CommonFrameworks.Net45;
            var net451 = FrameworkConstants.CommonFrameworks.Net451;

            var win8 = FrameworkConstants.CommonFrameworks.Win8;
            var win81 = FrameworkConstants.CommonFrameworks.Win81;

            var sl4 = FrameworkConstants.CommonFrameworks.SL4;
            var sl5 = FrameworkConstants.CommonFrameworks.SL5;

            var wp7 = FrameworkConstants.CommonFrameworks.WP7;
            var wp75 = FrameworkConstants.CommonFrameworks.WP75;
            var wp8 = FrameworkConstants.CommonFrameworks.WP8;
            var wp81 = FrameworkConstants.CommonFrameworks.WP81;

            var wpa81 = FrameworkConstants.CommonFrameworks.WPA81;

            return
            [
                // v4.6
                CreateProfileFrameworks(31, win81, wp81),
                CreateProfileFrameworks(32, win81, wpa81),
                CreateProfileFrameworks(44, net451, win81),
                CreateProfileFrameworks(84, wp81, wpa81),
                CreateProfileFrameworks(151, net451, win81, wpa81),
                CreateProfileFrameworks(157, win81, wp81, wpa81),

                // v4.5
                CreateProfileFrameworks(7, net45, win8),
                CreateProfileFrameworks(49, net45, wp8),
                CreateProfileFrameworks(78, net45, win8, wp8),
                CreateProfileFrameworks(111, net45, win8, wpa81),
                CreateProfileFrameworks(259, net45, win8, wpa81, wp8),

                // v4.0
                CreateProfileFrameworks(2, net4, win8, sl4, wp7),
                CreateProfileFrameworks(3, net4, sl4),
                CreateProfileFrameworks(4, net45, sl4, win8, wp7),
                CreateProfileFrameworks(5, net4, win8),
                CreateProfileFrameworks(6, net403, win8),
                CreateProfileFrameworks(14, net4, sl5),
                CreateProfileFrameworks(18, net403, sl4),
                CreateProfileFrameworks(19, net403, sl5),
                CreateProfileFrameworks(23, net45, sl4),
                CreateProfileFrameworks(24, net45, sl5),
                CreateProfileFrameworks(36, net4, sl4, win8, wp8),
                CreateProfileFrameworks(37, net4, sl5, win8),
                CreateProfileFrameworks(41, net403, sl4, win8),
                CreateProfileFrameworks(42, net403, sl5, win8),
                CreateProfileFrameworks(46, net45, sl4, win8),
                CreateProfileFrameworks(47, net45, sl5, win8),
                CreateProfileFrameworks(88, net4, sl4, win8, wp75),
                CreateProfileFrameworks(92, net4, win8, wpa81),
                CreateProfileFrameworks(95, net403, sl4, win8, wp7),
                CreateProfileFrameworks(96, net403, sl4, win8, wp75),
                CreateProfileFrameworks(102, net403, win8, wpa81),
                CreateProfileFrameworks(104, net45, sl4, win8, wp75),
                CreateProfileFrameworks(136, net4, sl5, win8, wp8),
                CreateProfileFrameworks(143, net403, sl4, win8, wp8),
                CreateProfileFrameworks(147, net403, sl5, win8, wp8),
                CreateProfileFrameworks(154, net45, sl4, win8, wp8),
                CreateProfileFrameworks(158, net45, sl5, win8, wp8),
                CreateProfileFrameworks(225, net4, sl5, win8, wpa81),
                CreateProfileFrameworks(240, net403, sl5, win8, wpa81),
                CreateProfileFrameworks(255, net45, sl5, win8, wpa81),
                CreateProfileFrameworks(328, net4, sl5, win8, wpa81, wp8),
                CreateProfileFrameworks(336, net403, sl5, win8, wpa81, wp8),
                CreateProfileFrameworks(344, net45, sl5, win8, wpa81, wp8)
            ];
        });

        public IEnumerable<KeyValuePair<int, NuGetFramework[]>> ProfileFrameworks
        {
            get
            {
                return ProfileFrameworksLazy.Value;
            }
        }

        private static KeyValuePair<int, NuGetFramework[]> CreateProfileFrameworks(int profile, params NuGetFramework[] frameworks)
        {
            return new KeyValuePair<int, NuGetFramework[]>(profile, frameworks);
        }

        // profiles that also support monotouch1+monoandroid1
        private static readonly int[] ProfilesWithOptionalFrameworks =
        [
            5, 6, 7, 14, 19, 24, 37, 42, 44, 47, 49, 78, 92, 102, 111, 136, 147, 151, 158, 225, 255, 259, 328, 336, 344
        ];

        private static readonly Lazy<List<KeyValuePair<int, NuGetFramework[]>>> ProfileOptionalFrameworksLazy = new(() =>
        {
            var monoandroid = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.MonoAndroid, new Version(0, 0));
            var monotouch = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.MonoTouch, new Version(0, 0));
            var xamarinIOs = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.XamarinIOs, new Version(0, 0));
            var xamarinMac = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.XamarinMac, new Version(0, 0));
            var xamarinTVOS = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.XamarinTVOS, new Version(0, 0));
            var xamarinWatchOS = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.XamarinWatchOS, new Version(0, 0));
            var monoFrameworks = new NuGetFramework[] { monoandroid, monotouch, xamarinIOs, xamarinMac, xamarinWatchOS, xamarinTVOS };

            var profileOptionalFrameworks = new List<KeyValuePair<int, NuGetFramework[]>>(ProfilesWithOptionalFrameworks.Length);

            foreach (var profile in ProfilesWithOptionalFrameworks)
            {
                profileOptionalFrameworks.Add(new KeyValuePair<int, NuGetFramework[]>(profile, monoFrameworks));
            }

            return profileOptionalFrameworks;
        });

        public IEnumerable<KeyValuePair<int, NuGetFramework[]>> ProfileOptionalFrameworks
        {
            get
            {
                return ProfileOptionalFrameworksLazy.Value;
            }
        }

        private static readonly Lazy<KeyValuePair<int, FrameworkRange>[]> CompatibilityMappingsLazy = new(() =>
        {
            return
            [
                CreateStandardMapping(7, FrameworkConstants.CommonFrameworks.NetStandard11),
                CreateStandardMapping(31, FrameworkConstants.CommonFrameworks.NetStandard10),
                CreateStandardMapping(32, FrameworkConstants.CommonFrameworks.NetStandard12),
                CreateStandardMapping(44, FrameworkConstants.CommonFrameworks.NetStandard12),
                CreateStandardMapping(49, FrameworkConstants.CommonFrameworks.NetStandard10),
                CreateStandardMapping(78, FrameworkConstants.CommonFrameworks.NetStandard10),
                CreateStandardMapping(84, FrameworkConstants.CommonFrameworks.NetStandard10),
                CreateStandardMapping(111, FrameworkConstants.CommonFrameworks.NetStandard11),
                CreateStandardMapping(151, FrameworkConstants.CommonFrameworks.NetStandard12),
                CreateStandardMapping(157, FrameworkConstants.CommonFrameworks.NetStandard10),
                CreateStandardMapping(259, FrameworkConstants.CommonFrameworks.NetStandard10)
            ];
        });

        public IEnumerable<KeyValuePair<int, FrameworkRange>> CompatibilityMappings
        {
            get
            {
                return CompatibilityMappingsLazy.Value;
            }
        }

        private static KeyValuePair<int, FrameworkRange> CreateStandardMapping(
            int profileNumber,
            NuGetFramework netStandard)
        {
            var range = new FrameworkRange(
                    FrameworkConstants.CommonFrameworks.NetStandard10,
                    netStandard);

            return new KeyValuePair<int, FrameworkRange>(profileNumber, range);
        }

        private static readonly Lazy<IPortableFrameworkMappings> InstanceLazy = new(() => new DefaultPortableFrameworkMappings());

        /// <summary>
        /// Static instance of the portable framework mappings
        /// </summary>
        public static IPortableFrameworkMappings Instance
        {
            get
            {
                return InstanceLazy.Value;
            }
        }
    }
}
