// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// This file deliberately references the deprecated members it guards.
#pragma warning disable CS0618, TPVS002, TPVS003, TPVS004, TPVS005, TPVS006

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

/// <summary>
/// Pins the project-owned TPVS deprecation diagnostic ids so consumers can keep suppressing a single
/// deprecation (for example <c>#pragma warning disable TPVS003</c>) instead of silencing every obsolete
/// diagnostic in scope with <c>CS0618</c>. Renaming or dropping an id is a source breaking change for those
/// suppressions, so the ids are asserted rather than left implicit.
/// </summary>
[TestClass]
public class ObsoleteDiagnosticIdTests
{
#if NET
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

    private static MemberInfo?[] TpvsMembers =>
    [
        typeof(ITestRunEventsHandler2),
        typeof(ITestRunEventsHandler2).GetMethod(nameof(ITestRunEventsHandler2.AttachDebuggerToProcess)),
        typeof(IDataCollectorAttachments),
        typeof(RunConfiguration).GetProperty(nameof(RunConfiguration.TargetFrameworkVersion)),
        typeof(TestPropertyAttributes).GetField(nameof(TestPropertyAttributes.Trait)),
        typeof(IFrameworkHandle).GetProperty(nameof(IFrameworkHandle.EnableShutdownAfterTestRun)),
    ];

    // This group of deprecations is advisory. Escalating any of them to error: true would break consumers
    // that still compile against them, so that change is deliberately kept out of this set.
    [TestMethod]
    public void TpvsDeprecations_AreWarningsAndNotErrors()
    {
        foreach (var member in TpvsMembers)
        {
            Assert.IsFalse(GetObsoleteAttribute(member).IsError, $"'{member?.Name}' must stay a warning.");
        }
    }

#if NET
    // ObsoleteAttribute.DiagnosticId only exists on .NET 5 and newer, so the ids are applied under #if NET in
    // the product code and can only be asserted here on the same target frameworks. The .NET Framework and
    // netstandard2.0 assemblies keep emitting plain CS0618.
    [TestMethod]
    public void ITestRunEventsHandler2_HasTpvs002DiagnosticId()
        => AssertDiagnosticId(typeof(ITestRunEventsHandler2), "TPVS002");

    [TestMethod]
    public void ITestRunEventsHandler2AttachDebuggerToProcess_HasTpvs002DiagnosticId()
        => AssertDiagnosticId(
            typeof(ITestRunEventsHandler2).GetMethod(nameof(ITestRunEventsHandler2.AttachDebuggerToProcess)),
            "TPVS002");

    [TestMethod]
    public void IDataCollectorAttachments_HasTpvs003DiagnosticId()
        => AssertDiagnosticId(typeof(IDataCollectorAttachments), "TPVS003");

    [TestMethod]
    public void RunConfigurationTargetFrameworkVersion_HasTpvs004DiagnosticId()
        => AssertDiagnosticId(
            typeof(RunConfiguration).GetProperty(nameof(RunConfiguration.TargetFrameworkVersion)),
            "TPVS004");

    [TestMethod]
    public void TestPropertyAttributesTrait_HasTpvs005DiagnosticId()
        => AssertDiagnosticId(
            typeof(TestPropertyAttributes).GetField(nameof(TestPropertyAttributes.Trait)),
            "TPVS005");

    [TestMethod]
    public void IFrameworkHandleEnableShutdownAfterTestRun_HasTpvs006DiagnosticId()
        => AssertDiagnosticId(
            typeof(IFrameworkHandle).GetProperty(nameof(IFrameworkHandle.EnableShutdownAfterTestRun)),
            "TPVS006");

    [TestMethod]
    public void TpvsDiagnosticIds_AreDistinctPerDeprecation()
    {
        var ids = TpvsMembers.Select(member => GetObsoleteAttribute(member).DiagnosticId).Distinct().ToList();

        // ITestRunEventsHandler2 and its AttachDebuggerToProcess member share TPVS002 on purpose: they are a
        // single deprecation and a consumer should be able to silence both with one suppression.
        Assert.HasCount(5, ids);
    }

    // Setting DiagnosticId replaces CS0618, which silently invalidates any existing consumer suppression of
    // CS0618. UrlFormat makes the replacement self-documenting by pointing the diagnostic at docs/diagnostics.md,
    // where the combined "#pragma warning disable CS0618, TPVS0nn" form is spelled out.
    [TestMethod]
    public void TpvsDeprecations_PointAtTheDiagnosticDocumentation()
    {
        foreach (var member in TpvsMembers)
        {
            var attribute = GetObsoleteAttribute(member);
            Assert.AreEqual(
                ExpectedUrl(attribute.DiagnosticId!),
                attribute.UrlFormat,
                $"'{member?.Name}' must point at its section in the diagnostic documentation.");
        }
    }

    private static void AssertDiagnosticId(MemberInfo? member, string expectedDiagnosticId)
        => Assert.AreEqual(expectedDiagnosticId, GetObsoleteAttribute(member).DiagnosticId);
#endif

    private static ObsoleteAttribute GetObsoleteAttribute(MemberInfo? member)
    {
        Assert.IsNotNull(member, "The obsolete member was not found, it was probably renamed or removed.");

        var attribute = member.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
            .Cast<ObsoleteAttribute>()
            .SingleOrDefault();

        Assert.IsNotNull(attribute, $"'{member.Name}' is expected to be marked with [Obsolete].");

        return attribute;
    }
}
