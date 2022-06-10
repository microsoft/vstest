﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

internal interface IEnvironmentVariableHelper
{
    /// <summary>
    /// Retrieves the value of an environment variable from the current process.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <returns>The value of the environment variable specified by variable, or null if the environment variable is not found.</returns>
    string GetEnvironmentVariable(string variable);

    /// <summary>
    /// Retrieves the value of an environment variable from the current process and converts it to the given type.
    /// </summary>
    /// <typeparam name="TEnum">The type used for conversion.</typeparam>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="defaultValue">The default value to return if the environment variable is not found.</param>
    /// <returns></returns>
    TEnum GetEnvironmentVariableAsEnum<TEnum>(string variable, TEnum defaultValue = default) where TEnum : Enum;

#if !NETSTANDARD1_0
    /// <summary>
    /// Retrieves the value of an environment variable from the current process and convert it to the given type.
    /// For .NET Standard 1.3 and UWP/UAP, this helper does not support enums, instead use <see cref="GetEnvironmentVariableAsEnum"/>.
    /// </summary>
    /// <typeparam name="T">The type used for conversion.</typeparam>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="defaultValue">The default value to return if the environment variable is not found.</param>
    /// <returns></returns>
    T GetEnvironmentVariable<T>(string variable, T defaultValue = default) where T : IConvertible;
#endif
}
