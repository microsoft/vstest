// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.IO;
using System.Reflection;

[TestClass]
[TestCategory("Windows-Review")]
public class DeprecateExtensionsPathWarningTests : AcceptanceTestBase
{
    private IList<string> adapterDependencies;
    private IList<string> copiedFiles;

    private string BuildConfiguration
    {
        get
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            foreach (var file in this.copiedFiles)
            {
                File.Delete(file);
            }
        }
        catch
        {

        }
    }

    [TestInitialize]
    public void CopyAdapterToExtensions()
    {
        this.copiedFiles = new List<string>();
        var extensionsDir = Path.Combine(Path.GetDirectoryName(this.GetConsoleRunnerPath()), "Extensions");
        this.adapterDependencies = Directory.GetFiles(this.GetTestAdapterPath(), "*.dll", SearchOption.TopDirectoryOnly);

        try
        {
            foreach (var file in this.adapterDependencies)
            {
                var fileCopied = Path.Combine(extensionsDir, Path.GetFileName(file));
                this.copiedFiles.Add(fileCopied);
                File.Copy(file, fileCopied);
            }
        }
        catch
        {

        }
    }

    [TestMethod]
    public void VerifyDeprecatedWarningIsThrownWhenAdaptersPickedFromExtensionDirectory()
    {
        using var tempDir = new TempDirectory();
        var arguments = PrepareArguments(this.GetSampleTestAssembly(), null, null, this.FrameworkArgValue, resultsDirectory: tempDir.Path);

        this.InvokeVsTest(arguments);
        this.StdOutputContains("Adapter lookup is being changed, please follow");
    }

    public override string GetConsoleRunnerPath()
    {
        DirectoryInfo currentDirectory = new DirectoryInfo(typeof(DeprecateExtensionsPathWarningTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent.Parent.Parent.Parent.Parent.Parent;

        return Path.Combine(currentDirectory.FullName, "artifacts", BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
    }
}
