// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.Common.UnitTests.Utilities;

/// <summary>
/// Validates that the hand-written binding redirects in the three net462/net472 app.config files
/// (vstest.console, testhost.x86, datacollector) stay in sync for the dependencies that all three hosts
/// share. See AGENTS.md: bumping a netstandard2.0 package cascades into binding redirects that must be
/// added to all three app.configs; a missed/mismatched redirect causes a silent FileLoadException in
/// net462 hosts (e.g. the DTA scenario covered by the NoBindingRedirectHostTestBase acceptance test).
/// </summary>
[TestClass]
public class AppConfigBindingRedirectConsistencyTests
{
    // Dependencies that vstest.console, testhost.x86, and datacollector all redirect and that must agree
    // across the three files. Entries like Microsoft.VisualStudio.TestWindow.Interfaces or
    // Microsoft.VisualStudio.QualityTools.UnitTestFramework are intentionally testhost-only and excluded.
    private static readonly string[] SharedAssemblyNames =
    {
        "Microsoft.VisualStudio.TestPlatform.ObjectModel",
        "System.Runtime.CompilerServices.Unsafe",
        "System.Collections.Immutable",
        "System.Reflection.Metadata",
        "System.Memory",
        "System.Buffers",
    };

    private static string RepoRootDirectory { get; } = FindRepoRootDirectory();

    private static string FindRepoRootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TestPlatform.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException($"Could not locate the repository root (containing TestPlatform.slnx) starting from '{AppContext.BaseDirectory}'.");
        }

        return directory.FullName;
    }

    [TestMethod]
    public void SharedBindingRedirectsAreIdenticalAcrossAllThreeAppConfigFiles()
    {
        var appConfigPaths = new Dictionary<string, string>
        {
            ["vstest.console"] = Path.Combine(RepoRootDirectory, "src", "vstest.console", "app.config"),
            ["testhost.x86"] = Path.Combine(RepoRootDirectory, "src", "testhost.x86", "app.config"),
            ["datacollector"] = Path.Combine(RepoRootDirectory, "src", "datacollector", "app.config"),
        };

        foreach (var kvp in appConfigPaths)
        {
            Assert.IsTrue(File.Exists(kvp.Value), $"Expected app.config for '{kvp.Key}' at '{kvp.Value}'.");
        }

        var redirectsByHost = appConfigPaths.ToDictionary(
            kvp => kvp.Key,
            kvp => ParseBindingRedirects(kvp.Value));

        foreach (var assemblyName in SharedAssemblyNames)
        {
            string? expected = null;
            string? expectedHost = null;

            foreach (var pair in redirectsByHost)
            {
                var host = pair.Key;
                var redirects = pair.Value;
                Assert.IsTrue(
                    redirects.TryGetValue(assemblyName, out var redirect),
                    $"'{host}' app.config is missing a binding redirect for '{assemblyName}', but it is expected to be shared across vstest.console, testhost.x86, and datacollector.");

                if (expected is null)
                {
                    expected = redirect;
                    expectedHost = host;
                }
                else
                {
                    Assert.AreEqual(
                        expected,
                        redirect,
                        $"Binding redirect for '{assemblyName}' in '{host}' app.config ('{redirect}') does not match '{expectedHost}' app.config ('{expected}'). All three app.configs must agree for shared dependencies.");
                }
            }
        }
    }

    private static Dictionary<string, string> ParseBindingRedirects(string appConfigPath)
    {
        var document = XDocument.Load(appConfigPath);
        XNamespace ns = "urn:schemas-microsoft-com:asm.v1";

        var redirects = new Dictionary<string, string>();
        foreach (var dependentAssembly in document.Descendants(ns + "dependentAssembly"))
        {
            var identity = dependentAssembly.Element(ns + "assemblyIdentity");
            var bindingRedirect = dependentAssembly.Element(ns + "bindingRedirect");
            if (identity is null || bindingRedirect is null)
            {
                continue;
            }

            var name = identity.Attribute("name")?.Value;
            var oldVersion = bindingRedirect.Attribute("oldVersion")?.Value;
            var newVersion = bindingRedirect.Attribute("newVersion")?.Value;
            if (name is null)
            {
                continue;
            }

            redirects[name] = $"{oldVersion}->{newVersion}";
        }

        return redirects;
    }
}
