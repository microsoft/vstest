// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

internal class ConfigurationEntry
{
    public ConfigurationEntry(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string InlinePath => $"RunConfiguration.{Name}";

    public string FullPath => $"RunSettings.{InlinePath}";

    public override string ToString()
    {
        return Name;
    }
}
