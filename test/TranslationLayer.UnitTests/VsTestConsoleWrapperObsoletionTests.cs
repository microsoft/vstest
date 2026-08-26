// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// This file deliberately references the deprecated interface it guards.
#pragma warning disable CS0618, TPVS001

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests;

/// <summary>
/// Guards the deprecation shape of <see cref="IVsTestConsoleWrapperAsync"/>.
/// </summary>
[TestClass]
public class VsTestConsoleWrapperObsoletionTests
{
    /// <summary>
    /// The only members of <see cref="IVsTestConsoleWrapperAsync"/> that are not individually deprecated.
    /// </summary>
    ///
    /// <remarks>
    /// Deprecating them would emit a brand new <c>CS0618</c> at every call site that goes through
    /// <see cref="IVsTestConsoleWrapper"/>, which builds with warnings as errors would fail on. That belongs
    /// with the breaking changes of the deprecation clean up, so the gap is pinned here instead of closed.
    /// </remarks>
    private const string NotIndividuallyObsolete = "ProcessTestRunAttachmentsAsync";

#if NET
    /// <summary>
    /// The diagnostic id shared by <see cref="IVsTestConsoleWrapperAsync"/> and every one of its members.
    /// </summary>
    private const string Tpvs001 = "TPVS001";

    /// <summary>
    /// The document every TPVS deprecation links to, see <c>docs/diagnostics.md</c>.
    /// </summary>
    private const string DocumentationUrl = "https://github.com/microsoft/vstest/blob/main/docs/diagnostics.md";

    /// <summary>
    /// The anchor has to be the lowercased diagnostic id. GitHub lowercases the <c>id</c> of an anchor while
    /// sanitizing rendered markdown but resolves the fragment case sensitively, so an uppercase fragment never
    /// matches. That is also why <see cref="ObsoleteAttribute.UrlFormat"/> cannot use the <c>{0}</c> placeholder
    /// here: it expands to the diagnostic id verbatim, which is uppercase.
    /// </summary>
    private static string ExpectedUrl(string diagnosticId)
        => $"{DocumentationUrl}#{diagnosticId.ToLowerInvariant()}";
#endif

    /// <summary>
    /// <see cref="IVsTestConsoleWrapper"/> keeps deriving from the deprecated async interface so that already
    /// compiled callers keep resolving the inherited members. Dropping the base interface would be a binary
    /// breaking change, so it is asserted here rather than left to review.
    /// </summary>
    [TestMethod]
    public void IVsTestConsoleWrapper_StillInheritsTheObsoleteAsyncInterface()
        => Assert.IsTrue(typeof(IVsTestConsoleWrapperAsync).IsAssignableFrom(typeof(IVsTestConsoleWrapper)));

    [TestMethod]
    public void IVsTestConsoleWrapperAsync_IsObsolete()
        => Assert.IsNotNull(GetObsoleteAttribute(typeof(IVsTestConsoleWrapperAsync)));

    /// <summary>
    /// The interface level attribute alone is not enough: the compiler does not report it when an inherited
    /// member is reached through <see cref="IVsTestConsoleWrapper"/>, which is how virtually every consumer
    /// uses these APIs. Every member that carries the attribute today therefore has to keep it.
    /// </summary>
    [TestMethod]
    public void IVsTestConsoleWrapperAsync_KeepsEveryMemberIndividuallyObsolete()
    {
        foreach (var member in GetIndividuallyObsoleteMembers())
        {
            Assert.IsNotNull(
                GetObsoleteAttribute(member),
                $"'{member.Name}' must stay marked with [Obsolete], otherwise callers going through "
                + $"{nameof(IVsTestConsoleWrapper)} silently lose the deprecation warning.");
        }
    }

    // This deprecation is advisory. Escalating it to error: true would break consumers that still compile
    // against the async API, so that change is deliberately kept out of this set.
    [TestMethod]
    public void IVsTestConsoleWrapperAsync_DeprecationIsAWarningAndNotAnError()
    {
        Assert.IsFalse(GetObsoleteAttribute(typeof(IVsTestConsoleWrapperAsync))!.IsError);

        foreach (var member in GetIndividuallyObsoleteMembers())
        {
            Assert.IsFalse(GetObsoleteAttribute(member)!.IsError, $"'{member.Name}' must stay a warning.");
        }
    }

#if NET
    // ObsoleteAttribute.DiagnosticId only exists on .NET 5 and newer, so the id is applied under #if NET in the
    // product code. The .NET Framework and netstandard2.0 assemblies keep emitting plain CS0618.
    [TestMethod]
    public void IVsTestConsoleWrapperAsync_UsesTpvs001OnTheInterfaceAndEveryObsoleteMember()
    {
        Assert.AreEqual(Tpvs001, GetObsoleteAttribute(typeof(IVsTestConsoleWrapperAsync))!.DiagnosticId);

        foreach (var member in GetIndividuallyObsoleteMembers())
        {
            Assert.AreEqual(Tpvs001, GetObsoleteAttribute(member)!.DiagnosticId, $"'{member.Name}' id mismatch.");
        }
    }

    // Setting DiagnosticId replaces CS0618, which silently invalidates any existing consumer suppression of
    // CS0618. UrlFormat makes the replacement self-documenting by pointing the diagnostic at docs/diagnostics.md.
    [TestMethod]
    public void IVsTestConsoleWrapperAsync_PointsAtTheDiagnosticDocumentation()
    {
        Assert.AreEqual(
            ExpectedUrl(Tpvs001),
            GetObsoleteAttribute(typeof(IVsTestConsoleWrapperAsync))!.UrlFormat);

        foreach (var member in GetIndividuallyObsoleteMembers())
        {
            Assert.AreEqual(
                ExpectedUrl(Tpvs001),
                GetObsoleteAttribute(member)!.UrlFormat,
                $"'{member.Name}' must point at its section in the diagnostic documentation.");
        }
    }
#endif

    /// <summary>
    /// Fails if <see cref="NotIndividuallyObsolete"/> goes stale, either because the overloads were finally
    /// deprecated or because they were renamed or removed.
    /// </summary>
    [TestMethod]
    public void IVsTestConsoleWrapperAsync_OnlyProcessTestRunAttachmentsAsyncIsNotIndividuallyObsolete()
    {
        var notObsolete = typeof(IVsTestConsoleWrapperAsync).GetMembers()
            .Where(member => GetObsoleteAttribute(member) is null)
            .Select(member => member.Name)
            .Distinct()
            .ToList();

        Assert.AreEqual(NotIndividuallyObsolete, string.Join(", ", notObsolete));
    }

    /// <summary>
    /// The overloads are excluded by name everywhere else, so deprecating only one of them would slip through
    /// every other assertion here. Each one is therefore checked individually.
    /// </summary>
    [TestMethod]
    public void IVsTestConsoleWrapperAsync_BothProcessTestRunAttachmentsAsyncOverloadsStayUndeprecated()
    {
        var overloads = typeof(IVsTestConsoleWrapperAsync).GetMembers()
            .Where(member => member.Name == NotIndividuallyObsolete)
            .ToList();

        Assert.HasCount(2, overloads);

        foreach (var overload in overloads)
        {
            Assert.IsNull(
                GetObsoleteAttribute(overload),
                $"'{overload}' must stay undeprecated until the breaking half of the clean up.");
        }
    }

    private static IEnumerable<MemberInfo> GetIndividuallyObsoleteMembers()
        => typeof(IVsTestConsoleWrapperAsync).GetMembers()
            .Where(member => member.Name != NotIndividuallyObsolete);

    private static ObsoleteAttribute? GetObsoleteAttribute(MemberInfo member)
        => member.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
            .Cast<ObsoleteAttribute>()
            .SingleOrDefault();
}
