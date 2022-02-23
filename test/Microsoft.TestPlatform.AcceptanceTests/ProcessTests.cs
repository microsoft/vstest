// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class ProcessTests : AcceptanceTestBase
{
    [TestMethod]
    public void MissingFrameworkTextIsCorrectlyDisplayed()
    {
        // Arrange
        var assemblyPath = GetAssetFullPath("SimpleTestProjectMessedUpTargetFramework.dll", Core21TargetFramework);
        var runtimeConfigJson = Path.Combine(Path.GetDirectoryName(assemblyPath), "SimpleTestProjectMessedUpTargetFramework.runtimeconfig.json");
        var fileContent = File.ReadAllText(runtimeConfigJson);
        var updatedContent = fileContent.Replace("\"version\": \"2.1.0\"", "\"version\": \"42\"");
        File.WriteAllText(runtimeConfigJson, updatedContent);

        // Act
        InvokeDotnetTest(assemblyPath);

        // Assert
        ExitCodeEquals(1);
        StdErrorContains("The framework 'Microsoft.NETCore.App', version '42' (x64) was not found.");
    }
}
