// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

#nullable disable

namespace Microsoft.TestPlatform.TestUtilities;

public class TempDirectory : IDisposable
{
    /// <summary>
    /// Creates a unique temporary directory.
    /// </summary>
    public TempDirectory()
    {
        Path = CreateUniqueDirectory();
    }

    public string Path { get; }

    public void Dispose()
    {
        TryRemoveDirectory(Path);
    }

    public DirectoryInfo CreateDirectory(string dir)
        => Directory.CreateDirectory(System.IO.Path.Combine(Path, dir));

    public void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            fi.CopyTo(System.IO.Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
    /// <summary>
    /// Creates an unique temporary directory.
    /// </summary>
    /// <returns>
    /// Path of the created directory.
    /// </returns>
    internal static string CreateUniqueDirectory()
    {
        // AGENT_TEMPDIRECTORY is AzureDevops variable, which is set to path
        // that is cleaned up after every job. This is preferable to use over
        // just the normal temp.
        var temp = GetTempPath();
        var directoryPath = System.IO.Path.Combine(temp, Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(directoryPath);

        return directoryPath;
    }

    private static string GetTempPath()
        => Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY")
            ?? System.IO.Path.GetTempPath();

    public static void TryRemoveDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch { }
        }
    }
}
