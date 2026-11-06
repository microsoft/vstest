// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.Common.UnitTests.Utilities;

[TestClass]
public class IntegrationTestAssetSetupTests : IntegrationTestBase
{
    [TestMethod]
    [DataRow("MSTest")]
    [DataRow("Microsoft.NET.Test.Sdk")]
    public void GeneratedProjectShouldUseTheBuiltTestSdk(string originalPackage)
    {
        var projectPath = Path.Combine(TempDirectory.Path, "Generated.csproj");
        File.WriteAllText(projectPath,
            $@"<?xml version=""1.0"" encoding=""utf-16""?><Project Sdk=""Microsoft.NET.Sdk""><ItemGroup><PackageReference Include=""{originalPackage}"" Version=""99.0.0"" /></ItemGroup></Project>");

        UseLocallyBuiltTestSdk(TempDirectory.Path);

        var references = XDocument.Load(projectPath).Descendants("PackageReference").ToList();
        Assert.IsFalse(references.Any(reference => (string?)reference.Attribute("Include") == "MSTest"));
        var sdk = references.Single(reference => (string?)reference.Attribute("Include") == "Microsoft.NET.Test.Sdk");
        Assert.AreEqual(_testEnvironment.DependencyVersions["NETTestSdkVersion"], (string?)sdk.Attribute("Version"));
        if (originalPackage == "MSTest")
        {
            Assert.AreEqual(_testEnvironment.DependencyVersions["MSTestAdapterVersion"],
                (string?)references.Single(reference => (string?)reference.Attribute("Include") == "MSTest.TestAdapter").Attribute("Version"));
            Assert.AreEqual(_testEnvironment.DependencyVersions["MSTestFrameworkVersion"],
                (string?)references.Single(reference => (string?)reference.Attribute("Include") == "MSTest.TestFramework").Attribute("Version"));
        }

        var sources = XDocument.Load(Path.Combine(TempDirectory.Path, "NuGet.config")).Descendants("add");
        Assert.IsTrue(sources.Any(source => (string?)source.Attribute("value") == IntegrationTestEnvironment.LocalPackageSource));
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void ChildProcessShouldNotReceiveExplicitlyRemovedEnvironmentVariable()
    {
        const string variable = "VSTEST_TEST_REMOVED_ENVIRONMENT_VARIABLE";
        var previous = Environment.GetEnvironmentVariable(variable);
        try
        {
            Environment.SetEnvironmentVariable(variable, "parent-value");
            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe");
            ExecuteApplication(powershell,
                $"-NoProfile -NonInteractive -Command \"[Environment]::GetEnvironmentVariables().Contains('{variable}')\"",
                out var output, out var error, out var exitCode,
                new Dictionary<string, string?> { [variable] = null });

            Assert.AreEqual(0, exitCode, error);
            Assert.AreEqual("False", output.Trim());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    [TestMethod]
    public void TemplateEngineHomesShouldBeIsolatedPerProject()
    {
        var first = CreateIsolatedTemplateEngineHome(Path.Combine(TempDirectory.Path, "first"));
        var second = CreateIsolatedTemplateEngineHome(Path.Combine(TempDirectory.Path, "second"));

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(Directory.Exists(first));
        Assert.IsTrue(Directory.Exists(second));
    }
}
