// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TestPlatform.TestUtilities;

public static class TestUtils
{
    public static bool IsWindows { get; } = Environment.OSVersion.Platform.ToString().StartsWith("Win");
}
