// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class ProcessesInteractionTests : AcceptanceTestBase
{
    [TestMethod]
    [NetCoreTargetFrameworkDataSource]
    public void MissingFrameworkTextIsCorrectlyDisplayed(RunnerInfo runnerInfo)
    {
        // Arrange
        SetTestEnvironment(_testEnvironment, runnerInfo);
        const string testAssetProjectName = "SimpleTestProjectMessedUpTargetFramework";
        var assemblyPath = GetAssetFullPath(testAssetProjectName + ".dll", Core21TargetFramework);
        UpdateRuntimeConfigJsonWithInvalidFramework(assemblyPath, testAssetProjectName);
        using var tempDir = new TempDirectory();

        // Act
        var arguments = PrepareArguments(assemblyPath, GetTestAdapterPath(), "", FrameworkArgValue, tempDir.Path);
        InvokeVsTest(assemblyPath);

        // Assert
        ExitCodeEquals(1);
        StdErrorContains("The framework 'Microsoft.NETCore.App', version '42' (x64) was not found.");

        static void UpdateRuntimeConfigJsonWithInvalidFramework(string assemblyPath, string testAssetProjectName)
        {
            // On the contrary to other tests, we need to modify the test asset we are using to replace
            // the target framework with an invalid framework. This is why we have a specific test asset
            // that's only meant to be used by this project.
            // Having an invalid framework is a way to reproduce an issue we had on Unix, where we were
            // not handling correctly the process exit (causing us to not let time to the process to
            // flush its output and error streams).See https://github.com/microsoft/vstest/issues/3375
            var runtimeConfigJson = Path.Combine(Path.GetDirectoryName(assemblyPath), testAssetProjectName + ".runtimeconfig.json");
            var fileContent = File.ReadAllText(runtimeConfigJson);
            var updatedContent = fileContent.Replace("\"version\": \"2.1.0\"", "\"version\": \"0.0.0\"");
            File.WriteAllText(runtimeConfigJson, updatedContent);
        }
    }
}
