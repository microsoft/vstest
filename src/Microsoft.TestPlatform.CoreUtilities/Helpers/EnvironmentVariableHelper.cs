// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0

using System;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;

internal class EnvironmentVariableHelper : IEnvironmentVariableHelper
{
    /// <inheritdoc />
    public string GetEnvironmentVariable(string variable)
        => Environment.GetEnvironmentVariable(variable);

    /// <inheritdoc />
    // TODO: This helper won't be needed when we will stop support for .NET Standard 1.3 and UWP
    public TEnum GetEnvironmentVariableAsEnum<TEnum>(string variable, TEnum defaultValue = default) where TEnum : Enum
    => Environment.GetEnvironmentVariable(variable) is string value && !string.IsNullOrEmpty(value)
        ? (TEnum)Enum.Parse(typeof(TEnum), value)
        : defaultValue;

    /// <inheritdoc />
    public T GetEnvironmentVariable<T>(string variable, T defaultValue = default) where T : IConvertible
        => Environment.GetEnvironmentVariable(variable) is string value && !string.IsNullOrEmpty(value)
            ? ConvertToType<T>(value)
            : defaultValue;

    private static T ConvertToType<T>(string input)
    {
        var targetType = typeof(T);
        var conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;

#if !WINDOWS_UWP && !NETSTANDARD1_3
        if (conversionType.IsEnum)
        {
            return (T)Enum.Parse(conversionType, input);
        }
#endif

        return (T)Convert.ChangeType(input, conversionType);
    }
}

#endif
