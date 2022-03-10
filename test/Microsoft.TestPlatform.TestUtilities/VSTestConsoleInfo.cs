// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities;

public class VSTestConsoleInfo
{
    public VSTestConsoleInfo(string versionType, string? version, string path)
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

    public override string ToString() => $" vstest.console = {Version} [{VersionType}]";
}

