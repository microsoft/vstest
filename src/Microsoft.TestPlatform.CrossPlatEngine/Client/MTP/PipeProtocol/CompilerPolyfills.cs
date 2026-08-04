// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Compiler feature polyfills required by the vendored YTest.MTP.PipeProtocol sources so that records,
// init-only setters and required members compile on the .NET Framework and netstandard2.0 target
// frameworks (these attributes ship in the framework starting with .NET 5 / .NET 7). The upstream
// project obtains them through PolySharp; vstest has no such reference, so they are declared here.

#if NETFRAMEWORK || NETSTANDARD

#nullable enable

namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved to be used by the compiler for tracking metadata. This class should not be used by
/// developers in source code.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}

/// <summary>Specifies that a type has required members or that a member is required.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class RequiredMemberAttribute : Attribute
{
}

/// <summary>
/// Indicates that compiler support for a particular feature is required for the location where this
/// attribute is applied.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;

    public string FeatureName { get; }

    public bool IsOptional { get; init; }

    public const string RefStructs = nameof(RefStructs);

    public const string RequiredMembers = nameof(RequiredMembers);
}

#endif
