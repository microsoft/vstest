// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace NuGetClone.Frameworks
{
    /// <summary>
    /// Sorts NuGet Frameworks in a consistent way for package readers.
    /// The order is not particularly useful here beyond making things deterministic
    /// since it compares completely different frameworks.
    /// </summary>
    internal class NuGetFrameworkSorter : IComparer<NuGetFramework>
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public static NuGetFrameworkSorter Instance { get; } = new();
#pragma warning restore CS0618 // Type or member is obsolete

        [Obsolete("Use Instance singleton instead")]
        public NuGetFrameworkSorter()
        {
        }

        public int Compare(NuGetFramework? x, NuGetFramework? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(x, null))
            {
                return -1;
            }

            if (ReferenceEquals(y, null))
            {
                return 1;
            }

            // Any goes first
            if (x.IsAny
                && !y.IsAny)
            {
                return -1;
            }

            if (!x.IsAny
                && y.IsAny)
            {
                return 1;
            }

            // Unsupported goes last
            if (x.IsUnsupported
                && !y.IsUnsupported)
            {
                return 1;
            }

            if (!x.IsUnsupported
                && y.IsUnsupported)
            {
                return -1;
            }

            // Compare on Framework, Version, Profile, Platform, and PlatformVersion
            var result = StringComparer.OrdinalIgnoreCase.Compare(x.Framework, y.Framework);

            if (result != 0)
            {
                return result;
            }

            result = x.Version.CompareTo(y.Version);

            if (result != 0)
            {
                return result;
            }

            result = StringComparer.OrdinalIgnoreCase.Compare(x.Profile, y.Profile);

            if (result != 0)
            {
                return result;
            }

            result = StringComparer.OrdinalIgnoreCase.Compare(x.Platform, y.Platform);

            if (result != 0)
            {
                return result;
            }

            result = x.PlatformVersion.CompareTo(y.PlatformVersion);

            if (result != 0)
            {
                return result;
            }

            return 0;
        }
    }
}
