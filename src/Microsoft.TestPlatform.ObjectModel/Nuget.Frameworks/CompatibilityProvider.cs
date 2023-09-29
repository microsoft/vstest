// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NuGetClone.Frameworks
{
    internal class CompatibilityProvider : IFrameworkCompatibilityProvider
    {
        private readonly IFrameworkNameProvider _mappings;
        private readonly FrameworkExpander _expander;
        private static readonly NuGetFrameworkFullComparer FullComparer = NuGetFrameworkFullComparer.Instance;
        private readonly ConcurrentDictionary<CompatibilityCacheKey, bool> _cache;

        public CompatibilityProvider(IFrameworkNameProvider mappings)
        {
            _mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
            _expander = new FrameworkExpander(mappings);
            _cache = new ConcurrentDictionary<CompatibilityCacheKey, bool>();
        }

        /// <summary>
        /// Check if the frameworks are compatible.
        /// </summary>
        /// <param name="target">Project framework</param>
        /// <param name="candidate">Other framework to check against the project framework</param>
        /// <returns>True if framework supports other</returns>
        public bool IsCompatible(NuGetFramework target, NuGetFramework candidate)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));

            // check the cache for a solution
            var cacheKey = new CompatibilityCacheKey(target, candidate);

            if (!_cache.TryGetValue(cacheKey, out bool result))
            {
                result = IsCompatibleCore(target, candidate) == true;
                _cache.TryAdd(cacheKey, result);
            }

            return result;
        }

        /// <summary>
        /// Actual compatibility check without caching
        /// </summary>
        private bool? IsCompatibleCore(NuGetFramework target, NuGetFramework candidate)
        {
            bool? result = null;

            // check if they are the exact same
            if (FullComparer.Equals(target, candidate))
            {
                return true;
            }

            // special cased frameworks
            if (!target.IsSpecificFramework
                || !candidate.IsSpecificFramework)
            {
                result = IsSpecialFrameworkCompatible(target, candidate);
            }

            if (result == null)
            {
                if (target.IsPCL || candidate.IsPCL)
                {
                    // PCL compat logic
                    result = IsPCLCompatible(target, candidate);
                }
                else
                {
                    // regular framework compat check
                    result = IsCompatibleWithTarget(target, candidate);
                }
            }

            return result;
        }

        private bool? IsSpecialFrameworkCompatible(NuGetFramework target, NuGetFramework candidate)
        {
            // TODO: Revist these
            if (target.IsAny
                || candidate.IsAny)
            {
                return true;
            }

            if (target.IsUnsupported)
            {
                return false;
            }

            if (candidate.IsAgnostic)
            {
                return true;
            }

            if (candidate.IsUnsupported)
            {
                return false;
            }

            return null;
        }

        private bool IsPCLCompatible(NuGetFramework target, NuGetFramework candidate)
        {
            if (target.IsPCL && !candidate.IsPCL)
            {
                return IsCompatibleWithTarget(target, candidate);
            }

            IEnumerable<NuGetFramework>? targetFrameworks;
            IEnumerable<NuGetFramework>? candidateFrameworks;

            if (target.IsPCL)
            {
                // do not include optional frameworks here since we might be unable to tell what is optional on the other framework
                if (!_mappings.TryGetPortableFrameworks(target.Profile, includeOptional: false, out targetFrameworks))
                {
                    targetFrameworks = Array.Empty<NuGetFramework>();
                }
            }
            else
            {
                targetFrameworks = new NuGetFramework[] { target };
            }

            if (candidate.IsPCL)
            {
                // include optional frameworks here, the larger the list the more compatible it is
                if (!_mappings.TryGetPortableFrameworks(candidate.Profile, includeOptional: true, out candidateFrameworks))
                {
                    candidateFrameworks = Array.Empty<NuGetFramework>();
                }
            }
            else
            {
                candidateFrameworks = new NuGetFramework[] { candidate };
            }

            // check if we this is a compatible superset
            return PCLInnerCompare(targetFrameworks, candidateFrameworks);
        }

        private bool PCLInnerCompare(IEnumerable<NuGetFramework> targetFrameworks, IEnumerable<NuGetFramework> candidateFrameworks)
        {
            // TODO: Does this check need to make sure multiple frameworks aren't matched against a single framework from the other list?
            return targetFrameworks.Count() <= candidateFrameworks.Count() && targetFrameworks.All(f => candidateFrameworks.Any(ff => IsCompatible(f, ff)));
        }

        private bool IsCompatibleWithTarget(NuGetFramework target, NuGetFramework candidate)
        {
            // find all possible substitutions
            var targetSet = new List<NuGetFramework>() { target };
            targetSet.AddRange(_expander.Expand(target));

            var candidateSet = new List<NuGetFramework>() { candidate };
            candidateSet.AddRange(GetEquivalentFrameworksClosure(candidate));

            // check for compat
            foreach (var currentCandidate in candidateSet)
            {
                if (targetSet.Any(framework => IsCompatibleWithTargetCore(framework, currentCandidate)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCompatibleWithTargetCore(NuGetFramework target, NuGetFramework candidate)
        {
            bool result = true;
            bool isNet6Era = target.IsNet5Era && target.Version.Major >= 6;
            if (isNet6Era && target.HasPlatform && !NuGetFramework.FrameworkNameComparer.Equals(target, candidate))
            {
                if (candidate.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.MonoAndroid, StringComparison.OrdinalIgnoreCase))
                {
                    result = result && StringComparer.OrdinalIgnoreCase.Equals(target.Platform, "android");
                }
                else if (candidate.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.Tizen, StringComparison.OrdinalIgnoreCase))
                {
                    result = result && StringComparer.OrdinalIgnoreCase.Equals(target.Platform, "tizen");
                }
                else
                {
                    result = false;
                }
            }
            else
            {
                result = NuGetFramework.FrameworkNameComparer.Equals(target, candidate)
                            && IsVersionCompatible(target.Version, candidate.Version)
                            && StringComparer.OrdinalIgnoreCase.Equals(target.Profile, candidate.Profile);

                if (target.IsNet5Era && candidate.HasPlatform)
                {
                    result = result
                        && StringComparer.OrdinalIgnoreCase.Equals(target.Platform, candidate.Platform)
                        && IsVersionCompatible(target.PlatformVersion, candidate.PlatformVersion);
                }
            }


            return result;
        }

        private static bool IsVersionCompatible(Version target, Version candidate)
        {
            return candidate == FrameworkConstants.EmptyVersion || candidate <= target;
        }

        /// <summary>
        /// Find all equivalent frameworks, and their equivalent frameworks.
        /// Example:
        /// Mappings:
        /// A &lt;&#8210;&gt; B
        /// B &lt;&#8210;&gt; C
        /// C &lt;&#8210;&gt; D
        /// For A we need to find B, C, and D so we must retrieve equivalent frameworks for A, B, and C
        /// also as we discover them.
        /// </summary>
        private IEnumerable<NuGetFramework> GetEquivalentFrameworksClosure(NuGetFramework framework)
        {
            // add the current framework to the seen list to avoid returning it later
            var seen = new HashSet<NuGetFramework>() { framework };

            var toExpand = new Stack<NuGetFramework>();
            toExpand.Push(framework);

            while (toExpand.Count > 0)
            {
                var frameworkToExpand = toExpand.Pop();

                if (_mappings.TryGetEquivalentFrameworks(frameworkToExpand, out IEnumerable<NuGetFramework>? compatibleFrameworks))
                {
                    foreach (var curFramework in compatibleFrameworks)
                    {
                        if (seen.Add(curFramework))
                        {
                            yield return curFramework;

                            toExpand.Push(curFramework);
                        }
                    }
                }
            }

            yield break;
        }
    }
}
