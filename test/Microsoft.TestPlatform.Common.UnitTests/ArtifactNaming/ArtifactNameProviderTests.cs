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

    private static ArtifactNameProvider CreateProvider(
        HashSet<string>? existingFiles = null,
        HashSet<string>? existingMarkers = null,
        HashSet<string>? existingDirectories = null)
    {
        existingFiles ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        existingMarkers ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        existingDirectories ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new ArtifactNameProvider(
            () => FixedTime,
            path => existingFiles.Contains(Path.GetFullPath(path)),
            tryCreateMarkerFile: (path, _) =>
            {
                if (existingMarkers.Contains(Path.GetFullPath(path)))
                {
                    return false;
                }

                existingMarkers.Add(Path.GetFullPath(path));
                return true;
            },
            directoryExists: path => existingDirectories.Contains(Path.GetFullPath(path)),
            createDirectory: path => existingDirectories.Add(Path.GetFullPath(path)));
    }

    [TestMethod]
    public void Resolve_BasicTemplate_ProducesExpectedPath()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
        });

        Assert.IsTrue(result.FilePath.EndsWith("MyTests_net8.0_x64.trx", StringComparison.Ordinal), $"Actual: {result.FilePath}");
        Assert.IsFalse(result.IsOverwrite);
    }

    [TestMethod]
    public void Resolve_WithDirectoryTemplate_CreatesSubdirectory()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}",
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = "{TestResultsDirectory}/{Tfm}",
        });

        Assert.Contains(Path.Combine("TestResults", "net8.0"), result.FilePath);
        Assert.IsTrue(result.FilePath.EndsWith("MyTests_net8.0.trx", StringComparison.Ordinal), $"Actual: {result.FilePath}");
    }

    [TestMethod]
    public void Resolve_Overwrite_ReturnsSamePathEvenIfExists()
    {
        string expectedPath = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64.trx"));
        ArtifactNameProvider provider = CreateProvider(existingFiles: new HashSet<string> { expectedPath });
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.Overwrite,
        });

        Assert.AreEqual(expectedPath, Path.GetFullPath(result.FilePath));
        Assert.IsTrue(result.IsOverwrite, "Should detect that file already exists.");
    }

    [TestMethod]
    public void Resolve_Overwrite_NoOverwriteWhenFileDoesNotExist()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.Overwrite,
        });

        Assert.IsFalse(result.IsOverwrite);
    }

    [TestMethod]
    public void Resolve_AppendCounter_AppendsSuffixWhenFileExists()
    {
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64.trx"));
        ArtifactNameProvider provider = CreateProvider(existingFiles: new HashSet<string> { basePath });
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.AppendCounter,
        });

        string expected = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_2.trx"));
        Assert.AreEqual(expected, Path.GetFullPath(result.FilePath));
        Assert.IsFalse(result.IsOverwrite);
    }

    [TestMethod]
    public void Resolve_AppendCounter_SkipsExistingCounters()
    {
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64.trx"));
        string counter2 = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_2.trx"));
        string counter3 = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_3.trx"));
        ArtifactNameProvider provider = CreateProvider(existingFiles: new HashSet<string> { basePath, counter2, counter3 });
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.AppendCounter,
        });

        string expected = Path.GetFullPath(Path.Combine("TestResults", "MyTests_net8.0_x64_4.trx"));
        Assert.AreEqual(expected, Path.GetFullPath(result.FilePath));
    }

    [TestMethod]
    public void Resolve_Fail_ThrowsWhenFileExists()
    {
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests.trx"));
        ArtifactNameProvider provider = CreateProvider(existingFiles: new HashSet<string> { basePath });
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

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
        ArtifactNameProvider provider = CreateProvider(existingFiles: new HashSet<string> { basePath });
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.AppendTimestamp,
        });

        Assert.Contains("20260415T105100.123", result.FilePath);
    }

    [TestMethod]
    public void Resolve_AutoTokens_TimestampAndMachineNamePopulated()
    {
        ArtifactNameProvider provider = CreateProvider();
        var context = new Dictionary<string, string>
        {
            [ArtifactNameTokens.TestResultsDirectory] = "TestResults",
        };

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{Timestamp}_{MachineName}",
            Extension = ".trx",
            Context = context,
        });

        Assert.Contains("20260415T105100.123", result.FilePath);
        Assert.Contains(Environment.MachineName, result.FilePath);
    }

    [TestMethod]
    public void Resolve_MissingToken_KeptLiterallyInOutput()
    {
        ArtifactNameProvider provider = CreateProvider();
        var context = new Dictionary<string, string>
        {
            [ArtifactNameTokens.TestResultsDirectory] = "TestResults",
            [ArtifactNameTokens.Tfm] = "net8.0",
        };

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}",
            Extension = ".trx",
            Context = context,
        });

        // AssemblyName not in context → kept as {AssemblyName}
        // {} are NOT in our invalid chars list (we discussed this!)
        Assert.Contains("{AssemblyName}", result.FilePath);
        Assert.Contains("net8.0", result.FilePath);
    }

    [TestMethod]
    public void Resolve_ExtensionWithoutDot_DotIsAdded()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = "trx",
            Context = context,
        });

        Assert.IsTrue(result.FilePath.EndsWith(".trx", StringComparison.Ordinal), $"Actual: {result.FilePath}");
    }

    [TestMethod]
    public void Resolve_CIPreset_ProducesFlatDeterministicPath()
    {
        Assert.IsTrue(ArtifactNamingPresets.TryGetPreset("ci", out string dirTemplate, out string fileTemplate, out CollisionBehavior collision));

        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = fileTemplate,
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = dirTemplate,
            Collision = collision,
        });

        Assert.IsTrue(result.FilePath.EndsWith("MyTests_net8.0_x64.trx", StringComparison.Ordinal), $"Actual: {result.FilePath}");
        Assert.AreEqual(CollisionBehavior.Overwrite, collision);
    }

    [TestMethod]
    public void Resolve_LocalPreset_CreatesTimestampedSubdirectory()
    {
        Assert.IsTrue(ArtifactNamingPresets.TryGetPreset("local", out string dirTemplate, out string fileTemplate, out CollisionBehavior collision));

        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = fileTemplate,
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = dirTemplate,
            Collision = collision,
        });

        Assert.Contains("20260415T105100.123", result.FilePath);
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
        ArtifactNameProvider provider = CreateProvider();
        Assert.ThrowsExactly<ArgumentNullException>(() => provider.Resolve(null!));
    }

    [TestMethod]
    public void Resolve_MultipleFilesPerProducer_BlameScenario()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");
        context[ArtifactNameTokens.Pid] = "12345";

        ArtifactNameResult seqResult = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}_blame-sequence",
            Extension = ".xml",
            Context = context,
            ArtifactKind = "blame-sequence",
        });

        ArtifactNameResult dumpResult = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}_crashdump_{Pid}",
            Extension = ".dmp",
            Context = context,
            ArtifactKind = "blame-crashdump",
        });

        Assert.IsTrue(seqResult.FilePath.EndsWith("blame-sequence.xml", StringComparison.Ordinal), $"Actual: {seqResult.FilePath}");
        Assert.IsTrue(dumpResult.FilePath.EndsWith("crashdump_12345.dmp", StringComparison.Ordinal), $"Actual: {dumpResult.FilePath}");
        Assert.AreNotEqual(seqResult.FilePath, dumpResult.FilePath);
    }

    [TestMethod]
    public void Resolve_DirectoryOwnership_FirstCallOwnsDirectory()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = "{TestResultsDirectory}/{Timestamp}",
        });

        Assert.IsTrue(result.IsDirectoryOwner, "First call should own the directory.");
    }

    [TestMethod]
    public void Resolve_DirectoryOwnership_SecondCallDoesNotOwnDirectory()
    {
        // Simulate directory already existing (created by another process).
        string dir = Path.GetFullPath(Path.Combine("TestResults", "20260415T105100.123"));
        ArtifactNameProvider provider = CreateProvider(existingDirectories: new HashSet<string> { dir });
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            DirectoryTemplate = "{TestResultsDirectory}/{Timestamp}",
        });

        Assert.IsFalse(result.IsDirectoryOwner, "Should not own directory that already existed.");
    }

    [TestMethod]
    public void Resolve_ResultToString_ReturnsFilePath()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = ".trx",
            Context = context,
        });

        Assert.AreEqual(result.FilePath, result.ToString());
    }

    [TestMethod]
    public void SanitizeFileName_TrailingDot_IsStripped()
    {
        string result = ArtifactNameProvider.SanitizeFileName("file.");

        Assert.AreEqual("file", result);
    }

    [TestMethod]
    public void SanitizeFileName_TrailingSpace_IsStripped()
    {
        string result = ArtifactNameProvider.SanitizeFileName("file ");

        Assert.AreEqual("file", result);
    }

    [TestMethod]
    public void SanitizeFileName_TrailingDotsAndSpaces_AreStripped()
    {
        string result = ArtifactNameProvider.SanitizeFileName("file . .");

        Assert.AreEqual("file", result);
    }

    [TestMethod]
    public void SanitizeFileName_AllInvalidChars_ReturnsUnderscore()
    {
        string result = ArtifactNameProvider.SanitizeFileName("***");

        Assert.AreEqual("___", result);
    }

    [TestMethod]
    public void SanitizeFileName_ReservedNameCaseInsensitive_PrefixedWithUnderscore()
    {
        string result = ArtifactNameProvider.SanitizeFileName("con");

        Assert.AreEqual("_con", result);
    }

    [TestMethod]
    public void SanitizePathSegments_ReservedNameInDirectorySegment_PrefixedWithUnderscore()
    {
        string result = ArtifactNameProvider.SanitizePathSegments("TestResults/CON/output");

        Assert.Contains("_CON", result);
        Assert.DoesNotContain(Path.DirectorySeparatorChar + "CON" + Path.DirectorySeparatorChar, result);
    }

    [TestMethod]
    public void Resolve_NullExtension_ProducesPathWithoutExtension()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = null!,
            Context = context,
        });

        Assert.IsTrue(result.FilePath.EndsWith("MyTests", StringComparison.Ordinal), $"Actual: {result.FilePath}");
    }

    [TestMethod]
    public void Resolve_EmptyExtension_ProducesPathWithoutExtension()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = "",
            Context = context,
        });

        Assert.IsTrue(result.FilePath.EndsWith("MyTests", StringComparison.Ordinal), $"Actual: {result.FilePath}");
    }

    [TestMethod]
    public void Resolve_ContextOverridesAutoTimestamp()
    {
        ArtifactNameProvider provider = CreateProvider();
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ArtifactNameTokens.TestResultsDirectory] = "TestResults",
            [ArtifactNameTokens.Timestamp] = "CUSTOM_STAMP",
        };

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{Timestamp}",
            Extension = ".trx",
            Context = context,
        });

        Assert.Contains("CUSTOM_STAMP", result.FilePath);
        Assert.DoesNotContain("20260415", result.FilePath);
    }

    [TestMethod]
    public void Resolve_ContextCaseInsensitive_OverridesAutoTokens()
    {
        ArtifactNameProvider provider = CreateProvider();
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ArtifactNameTokens.TestResultsDirectory] = "TestResults",
            ["timestamp"] = "custom_lower",
        };

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{Timestamp}",
            Extension = ".trx",
            Context = context,
        });

        // lowercase "timestamp" should override auto-populated "Timestamp" because
        // the merged dictionary is case-insensitive.
        Assert.Contains("custom_lower", result.FilePath);
    }

    [TestMethod]
    public void SanitizePathSegments_DotDotMixed_AllTraversalStripped()
    {
        string result = ArtifactNameProvider.SanitizePathSegments("a/../b/../../c");

        Assert.DoesNotContain("..", result);
        Assert.Contains("a", result);
        Assert.Contains("b", result);
        Assert.Contains("c", result);
    }

    [TestMethod]
    public void SanitizePathSegments_RootedUnixPath_HandledSafely()
    {
        string result = ArtifactNameProvider.SanitizePathSegments("/tmp/foo");

        // Should not crash. The leading / creates an empty segment which is skipped.
        Assert.Contains("tmp", result);
        Assert.Contains("foo", result);
    }

    [TestMethod]
    public void Resolve_Fail_DoesNotThrowWhenFileDoesNotExist()
    {
        ArtifactNameProvider provider = CreateProvider();
        Dictionary<string, string> context = CreateContext(testResultsDir: "TestResults");

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}_{Tfm}_{Architecture}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.Fail,
        });

        Assert.IsFalse(result.IsOverwrite);
        Assert.IsTrue(result.FilePath.EndsWith("MyTests_net8.0_x64.trx", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Resolve_AppendTimestamp_WithTimestampCollision_FallsBackToCounter()
    {
        // Both the base path and the timestamped path exist.
        string basePath = Path.GetFullPath(Path.Combine("TestResults", "MyTests.trx"));
        string timestamped = Path.GetFullPath(Path.Combine("TestResults", "MyTests_20260415T105100.123.trx"));
        ArtifactNameProvider provider = CreateProvider(existingFiles: new HashSet<string> { basePath, timestamped });
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ArtifactNameTokens.TestResultsDirectory] = "TestResults",
            [ArtifactNameTokens.AssemblyName] = "MyTests",
        };

        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = ".trx",
            Context = context,
            Collision = CollisionBehavior.AppendTimestamp,
        });

        // Should fall back to counter: MyTests_20260415T105100.123_2.trx
        Assert.Contains("20260415T105100.123_2", result.FilePath);
    }

    [TestMethod]
    public void Resolve_NoTestResultsDirectory_UsesLiteralToken()
    {
        ArtifactNameProvider provider = CreateProvider();
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ArtifactNameTokens.AssemblyName] = "MyTests",
        };

        // No TestResultsDirectory in context and no DirectoryTemplate → defaults to {TestResultsDirectory}
        // which is missing → kept literally.
        ArtifactNameResult result = provider.Resolve(new ArtifactNameRequest
        {
            FileTemplate = "{AssemblyName}",
            Extension = ".trx",
            Context = context,
        });

        Assert.Contains("{TestResultsDirectory}", result.FilePath);
    }
}
