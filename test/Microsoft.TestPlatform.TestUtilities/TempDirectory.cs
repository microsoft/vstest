// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using IO = System.IO;

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
        => Directory.CreateDirectory(IO.Path.Combine(Path, dir));

    public void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        CopyDirectory(new DirectoryInfo(sourceDirectory), new DirectoryInfo(targetDirectory));
    }

    public void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            fi.CopyTo(IO.Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyDirectory(diSourceSubDir, nextTargetSubDir);
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
        var temp = GetTempPath();
        var directoryPath = IO.Path.Combine(temp, "vstest", RandomId.Next());
        Directory.CreateDirectory(directoryPath);

        return directoryPath;
    }

    private static string GetTempPath()
    {
        // AGENT_TEMPDIRECTORY is AzureDevops variable, which is set to path
        // that is cleaned up after every job. This is preferable to use over
        // just the normal TEMP, because that is not cleaned up for every run.
        return Environment.GetEnvironmentVariable("AGENT_TEMPDIRECTORY") ?? IO.Path.GetTempPath();
    }

    public static void TryRemoveDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch { }
        }
    }
}
