// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TestPlatform.TestUtilities;

public class MSTestInfo
{
    public MSTestInfo(string versionType, string? version, string path)
    {
        VersionType = versionType;
        // Version can be null when we fail to find the respective propertin in TestPlatform.Dependencies.props
        // when that happens we throw when we try to update the path.
        Version = version;
        Path = path;
    }

    public string VersionType { get; }
    public string? Version { get; }
    public string Path { get; }

    public override string ToString() => $" MSTest = {Version} [{VersionType}]";

    public string UpdatePath(string path)
    {
        // Version is not directly used, below, but if it is not populated the path will be incorrect.
        // We don't want to throw when creating MSTestInfo because that is happening too early, and has worse error reporting.
        if (Version == null)
            throw new InvalidOperationException($"Version was not correctly populated from TestPlatform.Dependencies.props, review that there is entry for MSTestFramework{VersionType}Version.");

        // TODO: replacing in the result string is lame, but I am not going to fight 20 GetAssetFullPath method overloads right now
        return path.Replace($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}", Path);
    }
}
