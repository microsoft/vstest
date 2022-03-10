// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;

using System;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

internal class EnvironmentVariableHelper : IEnvironmentVariableHelper
{
    public string GetEnvironmentVariable(string variable)
        => Environment.GetEnvironmentVariable(variable);
}

#endif
