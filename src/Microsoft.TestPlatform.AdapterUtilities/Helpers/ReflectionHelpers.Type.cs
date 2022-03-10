// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities.Helpers;

using System;
using System.Reflection;

internal static partial class ReflectionHelpers
{
    internal static bool IsGenericType(Type type)
    {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
        return type.IsGenericType;
#else
        return type.GetTypeInfo().IsGenericType;
#endif
    }

    internal static MethodBase GetDeclaringMethod(Type type)
    {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
        return type.DeclaringMethod;
#else
        return type.GetTypeInfo().DeclaringMethod;
#endif
    }
}
