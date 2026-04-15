// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.Common.ArtifactNaming;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.Common.UnitTests.ArtifactNaming;

[TestClass]
public sealed class ArtifactNameTemplateTests
{
    private static readonly DateTime FixedTime = new(2026, 4, 15, 10, 51, 0, 123, DateTimeKind.Utc);

    private static ArtifactNameProvider CreateProvider(Func<string, bool>? fileExists = null)
        => new(() => FixedTime, fileExists ?? (_ => false));

    [TestMethod]
    public void ExpandTemplate_SimpleToken_IsReplaced()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string> { ["Name"] = "MyTests" };

        string result = provider.ExpandTemplate("{Name}", context);

        Assert.AreEqual("MyTests", result);
    }

    [TestMethod]
    public void ExpandTemplate_MultipleTokens_AreReplaced()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>
        {
            ["AssemblyName"] = "MyTests",
            ["Tfm"] = "net8.0",
            ["Architecture"] = "x64",
        };

        string result = provider.ExpandTemplate("{AssemblyName}_{Tfm}_{Architecture}", context);

        Assert.AreEqual("MyTests_net8.0_x64", result);
    }

    [TestMethod]
    public void ExpandTemplate_UnknownToken_IsKeptLiterally()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>();

        string result = provider.ExpandTemplate("{Unknown}", context);

        Assert.AreEqual("{Unknown}", result);
    }

    [TestMethod]
    public void ExpandTemplate_MixOfKnownAndUnknown_ExpandsCorrectly()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string> { ["Tfm"] = "net8.0" };

        string result = provider.ExpandTemplate("{AssemblyName}_{Tfm}", context);

        Assert.AreEqual("{AssemblyName}_net8.0", result);
    }

    [TestMethod]
    public void ExpandTemplate_EscapedBraces_ProduceLiteralBraces()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>();

        string result = provider.ExpandTemplate("{{literal}}", context);

        Assert.AreEqual("{literal}", result);
    }

    [TestMethod]
    public void ExpandTemplate_QuestionMarkInToken_KeptLiterally()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>();

        // ? is not a valid path char — kept literally to show misconfiguration.
        string result = provider.ExpandTemplate("{AssemblyName?default}", context);

        Assert.AreEqual("{AssemblyName?default}", result);
    }

    [TestMethod]
    public void ExpandTemplate_FormatSyntax_FormatsTimestamp()
    {
        var provider = CreateProvider();
        string isoTimestamp = FixedTime.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        var context = new Dictionary<string, string> { ["Timestamp"] = isoTimestamp };

        string result = provider.ExpandTemplate("{Timestamp:yyyyMMdd}", context);

        Assert.AreEqual("20260415", result);
    }

    [TestMethod]
    public void ExpandTemplate_EmptyTemplate_ReturnsEmpty()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>();

        string result = provider.ExpandTemplate("", context);

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ExpandTemplate_NoTokens_ReturnsLiteral()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>();

        string result = provider.ExpandTemplate("plain_text", context);

        Assert.AreEqual("plain_text", result);
    }

    [TestMethod]
    public void ExpandTemplate_UnclosedBrace_KeptAsLiteral()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>();

        string result = provider.ExpandTemplate("{unclosed", context);

        Assert.AreEqual("{unclosed", result);
    }

    [TestMethod]
    public void ExpandTemplate_AdjacentTokens_ExpandCorrectly()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>
        {
            ["A"] = "hello",
            ["B"] = "world",
        };

        string result = provider.ExpandTemplate("{A}{B}", context);

        Assert.AreEqual("helloworld", result);
    }

    [TestMethod]
    public void ExpandTemplate_TokenInDirectoryPath_ExpandsCorrectly()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>
        {
            ["TestResultsDirectory"] = "TestResults",
            ["Timestamp"] = "20260415T105100.123",
        };

        string result = provider.ExpandTemplate("{TestResultsDirectory}/{Timestamp}", context);

        Assert.AreEqual("TestResults/20260415T105100.123", result);
    }
}
