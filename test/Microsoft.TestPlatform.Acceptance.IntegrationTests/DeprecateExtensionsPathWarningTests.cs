﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
[TestCategory("Windows-Review")]
public class DeprecateExtensionsPathWarningTests : AcceptanceTestBase
{
    private readonly IList<string> _adapterDependencies;
    private readonly IList<string> _copiedFiles;

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

    public DeprecateExtensionsPathWarningTests()
    {
        _copiedFiles = new List<string>();
        var extensionsDir = Path.Combine(Path.GetDirectoryName(GetConsoleRunnerPath())!, "Extensions");
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

    public override string GetConsoleRunnerPath()
    {
        return Path.Combine(IntegrationTestEnvironment.PublishDirectory, $"Microsoft.TestPlatform.{IntegrationTestEnvironment.LatestLocallyBuiltNugetVersion}.nupkg", "tools",
            DEFAULT_RUNNER_NETFX, "Common7", "IDE", "Extensions", "TestPlatform", "vstest.console.exe");
    }
}
