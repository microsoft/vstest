// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// Tests that the DTA (Distributed Test Agent) scenario works: a .NET Framework host
/// loads Microsoft.TestPlatform.Common.dll without binding redirects. The shipped
/// System.Collections.Immutable DLL must match the assembly version referenced by
/// the compiled product assemblies.
/// </summary>
[TestClass]
public class DistributedTestAgentScenarioTests : AcceptanceTestBase
{
    /// <summary>
    /// Validates that the Microsoft.TestPlatform nupkg layout (used by DTA/VSTest task)
    /// contains a System.Collections.Immutable whose assembly version matches what
    /// Common.dll was compiled against.
    /// </summary>
    [TestMethod]
    public void NupkgLayout_CommonDll_CanLoadSciWithoutBindingRedirects()
    {
        var testPlatformDir = Path.Combine(
            IntegrationTestEnvironment.PublishDirectory,
            $"Microsoft.TestPlatform.{IntegrationTestEnvironment.LatestLocallyBuiltNugetVersion}.nupkg",
            "tools", "net462", "Common7", "IDE", "Extensions", "TestPlatform");

        ValidateDtaScenario(testPlatformDir, "nupkg");
    }

    /// <summary>
    /// Validates that the V2.CLI VSIX layout also has matching SCI assembly versions.
    /// </summary>
    [TestMethod]
    public void VsixLayout_CommonDll_CanLoadSciWithoutBindingRedirects()
    {
        var vsixDir = Path.GetDirectoryName(IntegrationTestEnvironment.LocalVsixInsertion);
        if (vsixDir is null || !Directory.Exists(vsixDir))
        {
            Assert.Inconclusive("VSIX directory not found. Build with -pack to produce it.");
        }

        var vsixExtractDir = Path.Combine(IntegrationTestEnvironment.ArtifactsTempDirectory, "vsix-extracted");
        if (!Directory.Exists(vsixExtractDir))
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(IntegrationTestEnvironment.LocalVsixInsertion, vsixExtractDir);
        }

        ValidateDtaScenario(vsixExtractDir, "VSIX");
    }

    private static void ValidateDtaScenario(string testPlatformDir, string layoutName)
    {
        Assert.IsTrue(Directory.Exists(testPlatformDir), $"{layoutName} directory not found: {testPlatformDir}");

        var commonDll = Path.Combine(testPlatformDir, "Microsoft.TestPlatform.Common.dll");
        var sciDll = Path.Combine(testPlatformDir, "System.Collections.Immutable.dll");

        Assert.IsTrue(File.Exists(commonDll), $"Common.dll not found in {layoutName} layout: {commonDll}");
        Assert.IsTrue(File.Exists(sciDll), $"System.Collections.Immutable.dll not found in {layoutName} layout: {sciDll}");

        var sciVersion = System.Reflection.AssemblyName.GetAssemblyName(sciDll).Version;
        var sciRefVersion = GetReferencedAssemblyVersion(commonDll, "System.Collections.Immutable");

        Assert.IsNotNull(sciRefVersion, $"Common.dll in {layoutName} layout does not reference System.Collections.Immutable");

        // For DTA scenario (no binding redirects), these MUST match exactly.
        Assert.AreEqual(
            sciRefVersion,
            sciVersion,
            $"{layoutName} layout: Common.dll references SCI {sciRefVersion} but shipped DLL is {sciVersion}. " +
            $"DTA hosts without binding redirects will fail with FileLoadException.");
    }

    private static Version? GetReferencedAssemblyVersion(string assemblyPath, string referenceName)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var mdReader = peReader.GetMetadataReader();
        foreach (var handle in mdReader.AssemblyReferences)
        {
            var asmRef = mdReader.GetAssemblyReference(handle);
            if (mdReader.GetString(asmRef.Name) == referenceName)
            {
                return asmRef.Version;
            }
        }

        return null;
    }
}
