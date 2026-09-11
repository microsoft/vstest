// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public sealed class CodeCoveragePublishTests : AcceptanceTestBase
{
    [TestMethod]
    [DataRow("Dependency.dll")]
    [DataRow("./Dependency.dll")]
    [DataRow("nested/../Dependency.dll")]
    [DataRow(@"nested\..\Dependency.dll")]
    public void PublishPreservesApplicationDependencyRegardlessOfTimestamp(string relativePath)
    {
        CreateProject(relativePath);
        WriteFile("coverage", "Dependency.dll", "collector dependency", 2030);
        WriteFile("coverage", Path.Combine("nested", "Dependency.dll"), "nested collector dependency", 2030);
        WriteFile("application", "selected.txt", "application dependency", 2000);
        WriteFile("publish", "Dependency.dll", "polluted dependency", 2040);

        Publish();
        AssertPublishedFile("Dependency.dll", "application dependency");
        AssertPublishedFile(Path.Combine("nested", "Dependency.dll"), "nested collector dependency");

        WriteFile("application", "selected.txt", "updated application dependency", 2040);
        WriteFile("coverage", "Dependency.dll", "older collector dependency", 2000);
        Publish();
        Publish();
        AssertPublishedFile("Dependency.dll", "updated application dependency");

        Directory.Delete(Path.Combine(TempDirectory.Path, "publish"), recursive: true);
        WriteFile("coverage", "Dependency.dll", "newer collector dependency", 2050);
        Publish();
        AssertPublishedFile("Dependency.dll", "updated application dependency");
    }

    [TestMethod]
    public void PublishOmitsFrameworkDependenciesAndRemovesPollutedOutput()
    {
        CreateProject("application.txt");
        WriteFile("application", "selected.txt", "application content", 2000);
        WriteFile("coverage", "System.Memory.dll", "private framework dependency", 2030);
        WriteFile("coverage", Path.Combine("nested", "System.Memory.dll"), "nested dependency", 2030);
        WriteFile("publish", "System.Memory.dll", "polluted framework dependency", 2040);
        WriteFile("publish", "unrelated.txt", "unrelated output", 2000);

        Publish();
        Publish();

        Assert.IsFalse(File.Exists(Path.Combine(TempDirectory.Path, "publish", "System.Memory.dll")));
        AssertPublishedFile(Path.Combine("nested", "System.Memory.dll"), "nested dependency");
        AssertPublishedFile("application.txt", "application content");
        AssertPublishedFile("unrelated.txt", "unrelated output");
    }

    [TestMethod]
    public void PublishPreservesExplicitApplicationFileEvenWhenFrameworkProvidesAssembly()
    {
        CreateProject("./System.Memory.dll");
        WriteFile("application", "selected.txt", "explicit application file", 2000);
        WriteFile("coverage", "System.Memory.dll", "collector dependency", 2030);
        WriteFile("publish", "System.Memory.dll", "polluted dependency", 2040);

        Publish();
        Publish();

        AssertPublishedFile("System.Memory.dll", "explicit application file");
    }

    [TestMethod]
    public void PublishHandlesOneApplicationSourceWithMultipleDestinations()
    {
        CreateProject("Dependency.dll", includeSecondDestination: true);
        WriteFile("application", "selected.txt", "application dependency", 2000);
        WriteFile("coverage", "Dependency.dll", "collector dependency", 2030);
        WriteFile("publish", "Dependency.dll", "polluted dependency", 2040);

        Publish();
        Publish();

        AssertPublishedFile("Dependency.dll", "application dependency");
        AssertPublishedFile(Path.Combine("other", "Dependency.dll"), "application dependency");
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void PublishMatchesDestinationCasingOnWindows()
    {
        CreateProject("DEPENDENCY.dll");
        WriteFile("application", "selected.txt", "application dependency", 2000);
        WriteFile("coverage", "Dependency.dll", "collector dependency", 2030);
        WriteFile("publish", "Dependency.dll", "polluted dependency", 2040);

        Publish();

        AssertPublishedFile("Dependency.dll", "application dependency");
    }

    private void CreateProject(string relativePath, bool includeSecondDestination = false)
    {
        var coverageDirectory = TempDirectory.CreateDirectory("coverage").FullName;
        File.Copy(
            Path.Combine(IntegrationTestEnvironment.RepoRootDirectory, "src", "package", "Microsoft.CodeCoverage", "Microsoft.CodeCoverage.targets"),
            Path.Combine(coverageDirectory, "Microsoft.CodeCoverage.targets"));

        // A private collector payload lets these tests control timestamps without touching the NuGet cache.
        File.WriteAllText(Path.Combine(TempDirectory.Path, "Publish.csproj"), $"""
            <Project>
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
              <PropertyGroup>
                <TargetFramework>{Core11TargetFramework}</TargetFramework>
                <EnableDefaultItems>false</EnableDefaultItems>
                <NuGetAudit>false</NuGetAudit>
              </PropertyGroup>
              <ItemGroup>
                <Content Include="application/selected.txt" TargetPath="{SecurityElement.Escape(relativePath)}" CopyToPublishDirectory="PreserveNewest" />
                <Content Include="application/selected.txt" TargetPath="other/Dependency.dll" CopyToPublishDirectory="PreserveNewest" Condition="{includeSecondDestination}" />
              </ItemGroup>
              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
              <Import Project="coverage/Microsoft.CodeCoverage.targets" />
            </Project>
            """);
    }

    private void Publish()
    {
        var dotnetPath = Path.Combine(IntegrationTestEnvironment.RepoRootDirectory, ".dotnet", OSUtils.IsWindows ? "dotnet.exe" : "dotnet");
        ExecuteApplication(
            dotnetPath,
            "publish Publish.csproj --nologo -v:quiet -o publish",
            out var output,
            out var error,
            out var exitCode,
            workingDirectory: TempDirectory.Path);

        Assert.AreEqual(0, exitCode, $"Publish failed.\n{output}\n{error}");
    }

    private void WriteFile(string directory, string relativePath, string contents, int year)
    {
        var path = Path.Combine(TempDirectory.Path, directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        File.SetLastWriteTimeUtc(path, new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private void AssertPublishedFile(string relativePath, string expected)
    {
        var path = Path.Combine(TempDirectory.Path, "publish", relativePath);
        Assert.IsTrue(File.Exists(path), $"Missing published file: {path}");
        Assert.AreEqual(expected, File.ReadAllText(path), $"Unexpected published file: {path}");
    }
}
