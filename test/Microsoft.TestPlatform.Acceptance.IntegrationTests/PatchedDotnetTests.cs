// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Acceptance.IntegrationTests;

[TestClass]
public class PatchedDotnetTests
{
    [TestMethod]
    public void FirstInvocationCopiesSdkAndPatchesBuildFiles()
    {
        using var fixture = new SdkFixture();

        fixture.CopyAndPatch();

        Assert.AreEqual(File.GetLastWriteTime(fixture.OriginalDotnet), File.GetLastWriteTime(fixture.PatchedDotnet));
        Assert.AreEqual("runtime", File.ReadAllText(fixture.PatchedHostFxr));
        Assert.AreEqual("task", File.ReadAllText(Path.Combine(fixture.PatchedSdk, "Microsoft.TestPlatform.Build.dll")));
        Assert.AreEqual("targets", File.ReadAllText(Path.Combine(fixture.PatchedSdk, "Microsoft.TestPlatform.targets")));
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void RepeatedInvocationReusesLockedSdkAndRefreshesBuildFiles()
    {
        using var fixture = new SdkFixture();
        fixture.CopyAndPatch();
        File.WriteAllText(fixture.BuildTask, "updated task");
        File.WriteAllText(fixture.BuildTargets, "updated targets");

        using var runtimeInUse = File.Open(fixture.PatchedHostFxr, FileMode.Open, FileAccess.Read, FileShare.Read);
        fixture.CopyAndPatch();

        Assert.AreEqual("runtime", File.ReadAllText(fixture.PatchedHostFxr));
        Assert.AreEqual("updated task", File.ReadAllText(Path.Combine(fixture.PatchedSdk, "Microsoft.TestPlatform.Build.dll")));
        Assert.AreEqual("updated targets", File.ReadAllText(Path.Combine(fixture.PatchedSdk, "Microsoft.TestPlatform.targets")));
    }

    [TestMethod]
    public void ChangedSdkIsCopiedAgain()
    {
        using var fixture = new SdkFixture();
        fixture.CopyAndPatch();
        File.WriteAllText(fixture.OriginalHostFxr, "updated runtime");
        File.SetLastWriteTimeUtc(fixture.OriginalDotnet, File.GetLastWriteTimeUtc(fixture.OriginalDotnet).AddMinutes(1));

        fixture.CopyAndPatch();

        Assert.AreEqual("updated runtime", File.ReadAllText(fixture.PatchedHostFxr));
        Assert.AreEqual(File.GetLastWriteTime(fixture.OriginalDotnet), File.GetLastWriteTime(fixture.PatchedDotnet));
    }

    [TestMethod]
    public void MissingCopiedDotnetIsCopiedAgain()
    {
        using var fixture = new SdkFixture();
        fixture.CopyAndPatch();
        File.Delete(fixture.PatchedDotnet);

        fixture.CopyAndPatch();

        Assert.IsTrue(File.Exists(fixture.PatchedDotnet));
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void FailedSdkCopyIsReportedAndRetried()
    {
        using var fixture = new SdkFixture();
        fixture.CopyAndPatch();
        File.WriteAllText(fixture.OriginalHostFxr, "updated runtime");
        File.SetLastWriteTimeUtc(fixture.OriginalDotnet, File.GetLastWriteTimeUtc(fixture.OriginalDotnet).AddMinutes(1));

        using (File.Open(fixture.PatchedHostFxr, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.ThrowsException<IOException>(() => fixture.CopyAndPatch());
        }

        fixture.CopyAndPatch();

        Assert.AreEqual("updated runtime", File.ReadAllText(fixture.PatchedHostFxr));
    }

    [TestMethod]
    public void MatchingExecutableAloneDoesNotMarkSdkCopyComplete()
    {
        using var fixture = new SdkFixture();
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.PatchedDotnet)!);
        File.Copy(fixture.OriginalDotnet, fixture.PatchedDotnet);

        fixture.CopyAndPatch();

        Assert.AreEqual("runtime", File.ReadAllText(fixture.PatchedHostFxr));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(2)]
    public void InvalidSdkDirectoryCountIsRejected(int count)
    {
        using var fixture = new SdkFixture();
        Directory.Delete(Path.Combine(fixture.OriginalSdkRoot, "test-sdk"));
        for (var i = 0; i < count; i++)
        {
            Directory.CreateDirectory(Path.Combine(fixture.OriginalSdkRoot, $"sdk-{i}"));
        }

        var exception = Assert.ThrowsException<InvalidOperationException>(() => fixture.CopyAndPatch());

        StringAssert.Contains(exception.Message, count == 0 ? "No .NET SDK directories" : "More than 1 .NET SDK directories");
    }

    private sealed class SdkFixture : IDisposable
    {
        private readonly TempDirectory _directory = new();
        private readonly string _originalDirectory;
        private readonly string _patchedDirectory;
        private readonly string _packageDirectory;

        public SdkFixture()
        {
            _originalDirectory = _directory.CreateDirectory("original").FullName;
            _patchedDirectory = Path.Combine(_directory.Path, "patched");
            _packageDirectory = _directory.CreateDirectory("package").FullName;

            var dotnetExe = OSUtils.IsWindows ? "dotnet.exe" : "dotnet";
            OriginalDotnet = Path.Combine(_originalDirectory, dotnetExe);
            PatchedDotnet = Path.Combine(_patchedDirectory, dotnetExe);
            OriginalSdkRoot = Path.Combine(_originalDirectory, "sdk");
            Directory.CreateDirectory(Path.Combine(OriginalSdkRoot, "test-sdk"));
            PatchedSdk = Path.Combine(_patchedDirectory, "sdk", "test-sdk");

            var fxrDirectory = Path.Combine("host", "fxr", "test-runtime");
            Directory.CreateDirectory(Path.Combine(_originalDirectory, fxrDirectory));
            OriginalHostFxr = Path.Combine(_originalDirectory, fxrDirectory, "hostfxr.dll");
            PatchedHostFxr = Path.Combine(_patchedDirectory, fxrDirectory, "hostfxr.dll");

            var taskDirectory = Directory.CreateDirectory(Path.Combine(_packageDirectory, "lib", "netstandard2.0"));
            var targetsDirectory = Directory.CreateDirectory(Path.Combine(_packageDirectory, "runtimes", "any", "native"));
            BuildTask = Path.Combine(taskDirectory.FullName, "Microsoft.TestPlatform.Build.dll");
            BuildTargets = Path.Combine(targetsDirectory.FullName, "Microsoft.TestPlatform.targets");

            File.WriteAllText(OriginalDotnet, "dotnet");
            File.WriteAllText(OriginalHostFxr, "runtime");
            File.WriteAllText(BuildTask, "task");
            File.WriteAllText(BuildTargets, "targets");
        }

        public string OriginalDotnet { get; }
        public string PatchedDotnet { get; }
        public string OriginalSdkRoot { get; }
        public string PatchedSdk { get; }
        public string OriginalHostFxr { get; }
        public string PatchedHostFxr { get; }
        public string BuildTask { get; }
        public string BuildTargets { get; }

        public void CopyAndPatch()
            => IntegrationTests.PatchedDotnet.CopyAndPatch(_originalDirectory, _patchedDirectory, _packageDirectory);

        public void Dispose() => _directory.Dispose();
    }
}
