// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;
using System.Runtime.Versioning;

internal static class KnownFramework
{
    public static FrameworkName NetCore(int major, int minor = 0) => new($".NETCoreApp,Version={major}.{minor}");

    public static FrameworkName Net50 = NetCore(5);
}
