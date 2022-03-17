﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
[TestCategory("Windows-Review")]
public class DeprecateExtensionsPathWarningTests : AcceptanceTestBase
{
    private IList<string> _adapterDependencies;
    private IList<string> _copiedFiles;

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            foreach (var file in _copiedFiles)
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
        _copiedFiles = new List<string>();
        var extensionsDir = Path.Combine(Path.GetDirectoryName(GetConsoleRunnerPath()), "Extensions");
        _adapterDependencies = Directory.GetFiles(GetTestAdapterPath(), "*.dll", SearchOption.TopDirectoryOnly);

        try
        {
            foreach (var file in _adapterDependencies)
            {
                var fileCopied = Path.Combine(extensionsDir, Path.GetFileName(file));
                _copiedFiles.Add(fileCopied);
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
        var arguments = PrepareArguments(GetSampleTestDll(), null, null, FrameworkArgValue, resultsDirectory: TempDirectory.Path);

        InvokeVsTest(arguments);
        StdOutputContains("Adapter lookup is being changed, please follow");
    }

    public override string GetConsoleRunnerPath()
    {
        DirectoryInfo currentDirectory = new DirectoryInfo(typeof(DeprecateExtensionsPathWarningTests).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent.Parent.Parent.Parent.Parent.Parent;

        return Path.Combine(currentDirectory.FullName, "artifacts", BuildConfiguration, "net451", "win7-x64", "vstest.console.exe");
    }
}
