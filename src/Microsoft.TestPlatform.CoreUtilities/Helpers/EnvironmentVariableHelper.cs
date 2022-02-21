// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

#if !NETSTANDARD1_0

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;

internal class EnvironmentVariableHelper : IEnvironmentVariableHelper
{
    public string GetEnvironmentVariable(string variable)
        => Environment.GetEnvironmentVariable(variable);
}

#endif
