// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using NuGetClone.Shared;

namespace NuGetClone.Frameworks
{
    /// <summary>
    /// Internal cache key used to store framework compatibility.
    /// </summary>
    internal readonly struct CompatibilityCacheKey : IEquatable<CompatibilityCacheKey>
    {
        public NuGetFramework Target { get; }

        public NuGetFramework Candidate { get; }

        private readonly int _hashCode;

        public CompatibilityCacheKey(NuGetFramework target, NuGetFramework candidate)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            Target = target;
            Candidate = candidate;

            // This is designed to be cached, just get the hash up front
            var combiner = new HashCodeCombiner();
            combiner.AddObject(target);
            combiner.AddObject(candidate);
            _hashCode = combiner.CombinedHash;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public bool Equals(CompatibilityCacheKey other)
        {
            return Target.Equals(other.Target)
                && Candidate.Equals(other.Candidate);
        }

        public override bool Equals(object? obj)
        {
            return obj is CompatibilityCacheKey other && Equals(other);
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0} -> {1}",
                Target.DotNetFrameworkName,
                Candidate.DotNetFrameworkName);
        }
    }
}
