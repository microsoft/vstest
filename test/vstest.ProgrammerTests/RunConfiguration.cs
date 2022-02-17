// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

internal static class RunConfiguration
{
    public static ConfigurationEntry MaxParallelLevel { get; } = new(nameof(MaxParallelLevel));
}
