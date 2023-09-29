// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGetClone.Frameworks
{
    /// <summary>
    /// A raw list of framework mappings. These are indexed by the framework name provider and in most cases all
    /// mappings are
    /// mirrored so that the IFrameworkMappings implementation only needs to provide the minimum amount of
    /// mappings.
    /// </summary>
    internal interface IFrameworkMappings
    {
        /// <summary>
        /// Synonym &#8210;&gt; Identifier
        /// Ex: NET Framework &#8210;&gt; .NET Framework
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> IdentifierSynonyms { get; }

        /// <summary>
        /// Ex: .NET Framework &#8210;&gt; net
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> IdentifierShortNames { get; }

        /// <summary>
        /// Ex: WindowsPhone &#8210;&gt; wp
        /// </summary>
        IEnumerable<FrameworkSpecificMapping> ProfileShortNames { get; }

        /// <summary>
        /// Equal frameworks. Used for legacy conversions.
        /// ex: Framework: Win8 &lt;&#8210;&gt; Framework: NetCore45 Platform: Win8
        /// </summary>
        IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> EquivalentFrameworks { get; }

        /// <summary>
        /// Framework, EquivalentProfile1, EquivalentProfile2
        /// Ex: Silverlight, WindowsPhone71, WindowsPhone
        /// </summary>
        IEnumerable<FrameworkSpecificMapping> EquivalentProfiles { get; }

        /// <summary>
        /// Frameworks which are subsets of others.
        /// Ex: .NETCore &#8210;&gt; .NET
        /// Everything in .NETCore maps to .NET and is one way compatible. Version numbers follow the same format.
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> SubSetFrameworks { get; }

        /// <summary>
        /// Additional framework compatibility rules beyond name and version matching.
        /// Ex: .NETFramework supports &#8210;&gt; Native
        /// </summary>
        IEnumerable<OneWayCompatibilityMappingEntry> CompatibilityMappings { get; }

        /// <summary>
        /// Ordered list of framework identifiers. The first framework in the list will be preferred over other 
        /// framework identifiers. This is enable better tie breaking in scenarios where legacy frameworks are 
        /// equivalently compatible to a new framework.
        /// Example: UAP10.0 &#8210;&gt; win81, wpa81
        /// </summary>
        IEnumerable<string> NonPackageBasedFrameworkPrecedence { get; }

        /// <summary>
        /// Same as <see cref="NonPackageBasedFrameworkPrecedence"/> but is only referred to if all of the packages
        /// in consideration are package based (determined by <see cref="NuGetFramework.IsPackageBased"/>).
        /// </summary>
        IEnumerable<string> PackageBasedFrameworkPrecedence { get; }

        /// <summary>
        /// Only used to choose between frameworks that are equivalent. This favors more human-readable target
        /// frameworks identifiers.
        /// </summary>
        IEnumerable<string> EquivalentFrameworkPrecedence { get; }

        /// <summary>
        /// Rewrite folder short names to the given value.
        /// Ex: dotnet50 &#8210;&gt; dotnet
        /// </summary>
        IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> ShortNameReplacements { get; }

        /// <summary>
        /// Rewrite full framework names to the given value.
        /// Ex: .NETPlatform,Version=v0.0 &#8210;&gt; .NETPlatform,Version=v5.0
        /// </summary>
        IEnumerable<KeyValuePair<NuGetFramework, NuGetFramework>> FullNameReplacements { get; }
    }
}
