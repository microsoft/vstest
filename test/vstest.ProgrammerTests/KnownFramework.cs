// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using System.Runtime.Versioning;

internal static class KnownFramework
{
    public static FrameworkName NetCore(int major, int minor = 0) => new($".NETCoreApp,Version={major}.{minor}");

    private static FrameworkName NetFramework(int major, int minor, int patch = 0) => new($".NETFramework,Version={major}.{minor}.{patch}");

    public static FrameworkName Netcoreapp1 = NetCore(1);
    public static FrameworkName Netcoreapp2 = NetCore(2);
    public static FrameworkName Netcoreapp21 = NetCore(2, 1);
    public static FrameworkName Netcoreapp3 = NetCore(3);
    public static FrameworkName Netcoreapp31 = NetCore(3, 1);
    public static FrameworkName Net5 = NetCore(5);
    public static FrameworkName Net6 = NetCore(6);
    public static FrameworkName Net7 = NetCore(7);

    public static FrameworkName Net48 = NetFramework(4, 8);
}
