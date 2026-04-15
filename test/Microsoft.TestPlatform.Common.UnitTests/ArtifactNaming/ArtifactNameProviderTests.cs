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
public sealed class ArtifactNameProviderTests
{
    private static readonly DateTime FixedTime = new(2026, 4, 15, 10, 51, 0, 123, DateTimeKind.Utc);

    private static Dictionary<string, string> CreateContext(
        string? assemblyName = "MyTests",
        string? tfm = "net8.0",
        string? architecture = "x64",
        string? testResultsDir = null)
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (assemblyName is not null)
        {
            ctx[ArtifactNameTokens.AssemblyName] = assemblyName;
        }

        if (tfm is not null)
        {
            ctx[ArtifactNameTokens.Tfm] = tfm;
        }

        if (architecture is not null)
        {
            ctx[ArtifactNameTokens.Architecture] = architecture;
        }

        if (testResultsDir is not null)
        {
            ctx[ArtifactNameTokens.TestResultsDirectory] = testResultsDir;
        }

        return ctx;
    }

    private static ArtifactNameProvider CreateProvider(HashSet<string>? existingFiles = null)
    {
        existingFiles ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return new ArtifactNameProvider(
            () => FixedTime,
            path => existingFiles.Contains(Path.GetFullPath(path)));
    }

    [TestMethod]
    public void Resolve_BasicTemplate_ProducesExpectedPath()
    {
        var provider = CreateProvider();
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
        });

        Assert.IsTrue(result.EndsWith("MyTests_net8.0_x64.trx", StringComparison.Ordinal), $"Actual: {result}");
    }

    [TestMethod]
    public void Resolve_WithDirectoryTemplate_CreatesSubdirectory()
    {
        var provider = CreateProvider();
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}",
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = "{TestResultsDirectory}/{Tfm}",
        });

        // Should contain the TFM as a directory segment.
        Assert.Contains(Path.Combine("TestResults", "net8.0"), result);
        Assert.IsTrue(result.EndsWith("MyTests_net8.0.trx", StringComparison.Ordinal), $"Actual: {result}");
    }

    [TestMethod]
    public void Resolve_Overwrite_ReturnsSamePathEvenIfExists()
    {
        string expectedPath = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64.trx"));
        var provider = CreateProvider(new HashSet<string> { expectedPath });
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.Overwrite,
        });

        Assert.AreEqual(expectedPath, Path.GetFullPath(result));
    }

    [TestMethod]
    public void Resolve_AppendCounter_AppendsSuffixWhenFileExists()
    {
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64.trx"));
        var provider = CreateProvider(new HashSet<string> { basePath });
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.AppendCounter,
        });

        string expected = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_2.trx"));
        Assert.AreEqual(expected, Path.GetFullPath(result));
    }

    [TestMethod]
    public void Resolve_AppendCounter_SkipsExistingCounters()
    {
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64.trx"));
        string counter2 = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_2.trx"));
        string counter3 = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_3.trx"));
        var provider = CreateProvider(new HashSet<string> { basePath, counter2, counter3 });
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.AppendCounter,
        });

        string expected = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_4.trx"));
        Assert.AreEqual(expected, Path.GetFullPath(result));
    }

    [TestMethod]
    public void Resolve_Fail_ThrowsWhenFileExists()
    {
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests.trx"));
        var provider = CreateProvider(new HashSet<string> { basePath });
        var context = CreateContext(testResultsDir: "TestResults");

        Assert.ThrowsExactly<InvalidOperationException>(() => provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.Fail,
        }));
    }

    [TestMethod]
    public void Resolve_AppendTimestamp_AddsTimestampSuffix()
    {
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64.trx"));
        var provider = CreateProvider(new HashSet<string> { basePath });
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.AppendTimestamp,
        });

        Assert.Contains("20260415T105100.123", result);
    }

    [TestMethod]
    public void Resolve_AutoTokens_TimestampAndMachineNamePopulated()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>
        {
            [ArtifactNameTokens.TestResultsDirectory] = "TestResults",
        };

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{Timestamp}_{MachineName}",
            Extension = ".trx",
            Context = context,
        });

        Assert.Contains("20260415T105100.123", result);
        Assert.Contains(Environment.MachineName, result);
    }

    [TestMethod]
    public void Resolve_MissingToken_KeptLiterallyInOutput()
    {
        var provider = CreateProvider();
        var context = new Dictionary<string, string>
        {
            [ArtifactNameTokens.TestResultsDirectory] = "TestResults",
            [ArtifactNameTokens.Tfm] = "net8.0",
        };

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}",
            Extension = ".trx",
            Context = context,
        });

        // AssemblyName not in context → kept as {AssemblyName}
        // {} are NOT in our invalid chars list (we discussed this!)
        Assert.Contains("{AssemblyName}", result);
        Assert.Contains("net8.0", result);
    }

    [TestMethod]
    public void Resolve_ExtensionWithoutDot_DotIsAdded()
    {
        var provider = CreateProvider();
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = "trx",
            Context = context,
        });

        Assert.IsTrue(result.EndsWith(".trx", StringComparison.Ordinal), $"Actual: {result}");
    }

    [TestMethod]
    public void Resolve_CIPreset_ProducesFlatDeterministicPath()
    {
        Assert.IsTrue(ArtifactNamingPresets.TryGetPreset("ci", out string dirTemplate, out string fileTemplate, out CollisionBehavior collision));

        var provider = CreateProvider();
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = fileTemplate,
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = dirTemplate,
            Collision = collision,
        });

        Assert.IsTrue(result.EndsWith("MyTests_net8.0_x64.trx", StringComparison.Ordinal), $"Actual: {result}");
        Assert.AreEqual(CollisionBehavior.Overwrite, collision);
    }

    [TestMethod]
    public void Resolve_LocalPreset_CreatesTimestampedSubdirectory()
    {
        Assert.IsTrue(ArtifactNamingPresets.TryGetPreset("local", out string dirTemplate, out string fileTemplate, out CollisionBehavior collision));

        var provider = CreateProvider();
        var context = CreateContext(testResultsDir: "TestResults");

        string result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = fileTemplate,
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = dirTemplate,
            Collision = collision,
        });

        Assert.Contains("20260415T105100.123", result);
    }

    [TestMethod]
    public void SanitizeFileName_InvalidChars_ReplacedWithUnderscore()
    {
        string result = ArtifactNameProvider.SanitizeFileName("test<file>name");

        Assert.AreEqual("test_file_name", result);
    }

    [TestMethod]
    public void SanitizeFileName_ReservedName_PrefixedWithUnderscore()
    {
        string result = ArtifactNameProvider.SanitizeFileName("CON");

        Assert.AreEqual("_CON", result);
    }

    [TestMethod]
    public void SanitizeFileName_ReservedNameWithExtension_PrefixedWithUnderscore()
    {
        string result = ArtifactNameProvider.SanitizeFileName("NUL.txt");

        Assert.AreEqual("_NUL.txt", result);
    }

    [TestMethod]
    public void SanitizePathSegments_DotDotTraversal_IsStripped()
    {
        string result = ArtifactNameProvider.SanitizePathSegments("TestResults/../etc/passwd");

        Assert.DoesNotContain("..", result);
        Assert.Contains("etc", result);
    }

    [TestMethod]
    public void SanitizePathSegments_DriveLetterPreserved()
    {
        string result = ArtifactNameProvider.SanitizePathSegments("C:/TestResults/output");

        Assert.IsTrue(result.StartsWith("C:", StringComparison.Ordinal), $"Actual: {result}");
    }

    [TestMethod]
    public void Resolve_NullRequest_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();
        Assert.ThrowsExactly<ArgumentNullException>(() => provider.Resolve(null!));
    }

    [TestMethod]
    public void Resolve_MultipleFilesPerProducer_BlameScenario()
    {
        var provider = CreateProvider();
        var context = CreateContext(testResultsDir: "TestResults");
        context[ArtifactNameTokens.Pid] = "12345";

        string seqPath = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}_blame-sequence",
            Extension = ".xml",
            Context = context,
            ArtifactKind = "blame-sequence",
        });

        string dumpPath = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}_crashdump_{Pid}",
            Extension = ".dmp",
            Context = context,
            ArtifactKind = "blame-crashdump",
        });

        Assert.IsTrue(seqPath.EndsWith("blame-sequence.xml", StringComparison.Ordinal), $"Actual: {seqPath}");
        Assert.IsTrue(dumpPath.EndsWith("crashdump_12345.dmp", StringComparison.Ordinal), $"Actual: {dumpPath}");
        Assert.AreNotEqual(seqPath, dumpPath);
    }
}
