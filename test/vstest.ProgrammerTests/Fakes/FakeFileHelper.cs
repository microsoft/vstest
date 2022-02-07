// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeFileHelper : IFileHelper
{
    public List<FakeFile> Files { get; } = new();

    public void CopyFile(string sourcePath, string destinationPath)
    {
        throw new NotImplementedException();
    }

    public DirectoryInfo CreateDirectory(string path)
    {
        throw new NotImplementedException();
    }

    public void Delete(string path)
    {
        throw new NotImplementedException();
    }

    public void DeleteEmptyDirectroy(string directoryPath)
    {
        throw new NotImplementedException();
    }

    public bool DirectoryExists(string path)
    {
        // TODO: Check if any file has the directory in name. This will improve.
        var directoryExists = Files.Select(f => Path.GetDirectoryName(f.Path)).Any(p => p.StartsWith(path));
        return directoryExists;
    }

    public IEnumerable<string> EnumerateFiles(string directory, SearchOption searchOption, params string[] endsWithSearchPatterns)
    {
        Func<FakeFile, string, bool> predicate = searchOption == SearchOption.TopDirectoryOnly
            ? (f, dir) => Path.GetDirectoryName(f.Path) == dir
            : (f, dir) => Path.GetDirectoryName(f.Path)!.Contains(dir);

        var files = Files.Where(f => predicate(f, directory)).Select(f => f.Path).ToList();
        return files;
    }

    public bool Exists(string path)
    {
        throw new NotImplementedException();
    }

    public string GetCurrentDirectory()
    {
        throw new NotImplementedException();
    }

    public FileAttributes GetFileAttributes(string path)
    {
        throw new NotImplementedException();
    }

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        throw new NotImplementedException();
    }

    public Version GetFileVersion(string path)
    {
        throw new NotImplementedException();
    }

    public string GetFullPath(string path)
    {
        throw new NotImplementedException();
    }

    public Stream GetStream(string filePath, FileMode mode, FileAccess access = FileAccess.ReadWrite)
    {
        throw new NotImplementedException();
    }

    public Stream GetStream(string filePath, FileMode mode, FileAccess access, FileShare share)
    {
        throw new NotImplementedException();
    }

    public void MoveFile(string sourcePath, string destinationPath)
    {
        throw new NotImplementedException();
    }

    public void WriteAllTextToFile(string filePath, string content)
    {
        throw new NotImplementedException();
    }

    internal void AddFile<T>(T file) where T : FakeFile
    {
        Files.Add(file);
    }
}
