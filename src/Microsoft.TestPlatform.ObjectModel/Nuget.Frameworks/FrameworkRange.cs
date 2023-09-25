// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace NuGetClone.Frameworks
{
    /// <summary>
    /// An inclusive range of frameworks
    /// </summary>
    internal class FrameworkRange : IEquatable<FrameworkRange>
    {
        private readonly bool _includeMin;
        private readonly bool _includeMax;

        public FrameworkRange(NuGetFramework min, NuGetFramework max)
            : this(min, max, true, true)
        {

        }

        public FrameworkRange(NuGetFramework min, NuGetFramework max, bool includeMin, bool includeMax)
        {
            if (min == null) throw new ArgumentNullException(nameof(min));
            if (max == null) throw new ArgumentNullException(nameof(max));

            if (!SameExceptForVersion(min, max))
            {
                throw new FrameworkException(Strings.FrameworkMismatch);
            }

            Min = min;
            Max = max;
            _includeMin = includeMin;
            _includeMax = includeMax;
        }

        /// <summary>
        /// Minimum Framework
        /// </summary>
        public NuGetFramework Min { get; }

        /// <summary>
        /// Maximum Framework
        /// </summary>
        public NuGetFramework Max { get; }

        /// <summary>
        /// Minimum version inclusiveness.
        /// </summary>
        public bool IncludeMin
        {
            get
            {
                return _includeMin;
            }
        }

        /// <summary>
        /// Maximum version inclusiveness.
        /// </summary>
        public bool IncludeMax
        {
            get
            {
                return _includeMax;
            }
        }

        /// <summary>
        /// Framework Identifier of both the Min and Max
        /// </summary>
        public string FrameworkIdentifier => Min.Framework;

        /// <summary>
        /// True if the framework version falls between the min and max
        /// </summary>
        public bool Satisfies(NuGetFramework framework)
        {
            return SameExceptForVersion(Min, framework)
                && (_includeMin ? Min.Version <= framework.Version : Min.Version < framework.Version)
                && (_includeMax ? Max.Version >= framework.Version : Max.Version > framework.Version);
        }

        private static bool SameExceptForVersion(NuGetFramework x, NuGetFramework y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x.Framework, y.Framework)
                   && (StringComparer.OrdinalIgnoreCase.Equals(x.Profile, y.Profile));
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "[{0}, {1}]", Min.ToString(), Max.ToString());
        }

        public bool Equals(FrameworkRange? other)
        {
            return FrameworkRangeComparer.Instance.Equals(this, other);
        }

        public override bool Equals(object? obj)
        {
            var other = obj as FrameworkRange;

            if (other != null)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return FrameworkRangeComparer.Instance.GetHashCode(this);
        }
    }
}
