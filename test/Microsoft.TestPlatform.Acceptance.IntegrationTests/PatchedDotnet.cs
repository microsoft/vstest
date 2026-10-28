// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Microsoft.TestPlatform.TestUtilities;

namespace Microsoft.TestPlatform.Acceptance.IntegrationTests;

internal static class PatchedDotnet
{
    internal static void CopyAndPatch(string dotnetDirectory, string patchedDotnetDirectory, string packagePath)
    {
        var dotnetExe = OSUtils.IsWindows ? "dotnet.exe" : "dotnet";
        var originalDotnetExePath = Path.Combine(dotnetDirectory, dotnetExe);
        var patchedDotnetExePath = Path.Combine(patchedDotnetDirectory, dotnetExe);
        var copyCompletePath = Path.Combine(patchedDotnetDirectory, ".vstest-sdk-copy-complete");

        // Match main's SDK cache: a later test invocation must not overwrite hostfxr.dll
        // while a process from the previous invocation is still using the copied SDK.
        bool skipCopy = File.Exists(copyCompletePath)
            && File.Exists(originalDotnetExePath)
            && File.Exists(patchedDotnetExePath)
            && File.GetLastWriteTime(originalDotnetExePath) == File.GetLastWriteTime(patchedDotnetExePath);

        if (!skipCopy)
        {
            // dotnet.exe is copied before hostfxr.dll, so its timestamp alone cannot prove a completed copy.
            if (File.Exists(copyCompletePath))
            {
                File.Delete(copyCompletePath);
            }
            DirectoryUtils.CopyDirectory(new DirectoryInfo(dotnetDirectory), new DirectoryInfo(patchedDotnetDirectory));
        }

        var sdkDirectory = Path.Combine(patchedDotnetDirectory, "sdk");
        var dotnetSdkDirectories = Directory.GetDirectories(sdkDirectory);
        if (dotnetSdkDirectories.Length == 0)
        {
            throw new InvalidOperationException($"No .NET SDK directories found in '{sdkDirectory}'.");
        }
        if (dotnetSdkDirectories.Length > 1)
        {
            throw new InvalidOperationException($"More than 1 .NET SDK directories found in '{sdkDirectory}': {string.Join(", ", dotnetSdkDirectories)}.");
        }

        if (!skipCopy)
        {
            File.WriteAllText(copyCompletePath, string.Empty);
        }

        // The SDK can be reused, but every invocation must use the current locally built tasks and targets.
        var dotnetSdkDirectory = dotnetSdkDirectories.Single();
        DirectoryUtils.CopyDirectory(Path.Combine(packagePath, "lib", "netstandard2.0"), dotnetSdkDirectory);
        DirectoryUtils.CopyDirectory(Path.Combine(packagePath, "runtimes", "any", "native"), dotnetSdkDirectory);
    }
}
