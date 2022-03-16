// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.TestPlatform.AcceptanceTests;

public static class Features
{
    public const string ATTACH_DEBUGGER = nameof(ATTACH_DEBUGGER);
    public const string MSTEST_IFRAMEWORK_HANDLE_99 = nameof(MSTEST_IFRAMEWORK_HANDLE_99);


    public static Dictionary<string, Feature> TestPlatformFeatures { get; } = new Dictionary<string, Feature>
    {
        [ATTACH_DEBUGGER] = new(version: "v16.7.0-preview-20200519-01", issue: "https://github.com/microsoft/vstest/pull/2325"),
    };

    public static Dictionary<string, Feature> AdapterFeatures { get; internal set; } = new Dictionary<string, Feature>
    {
        [MSTEST_IFRAMEWORK_HANDLE_99] = new("2.2.8", issue: "idk"),
    };
}
