// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities.Helpers;

using System;
using System.Reflection;

internal static partial class ReflectionHelpers
{
#if NETSTANDARD1_0 || NETSTANDARD1_3 || WINDOWS_UWP
    private static readonly Type MethodBase = typeof(MethodBase);

    private const string MemberTypePropertyName = "MemberType";
    private const string ReflectedTypePropertyName = "ReflectedType";
    private const string MethodHandlePropertyName = "MethodHandle";

    private static readonly PropertyInfo MemberTypeProperty = MethodBase.GetRuntimeProperty(MemberTypePropertyName);
    private static readonly PropertyInfo ReflectedTypeProperty = MethodBase.GetRuntimeProperty(ReflectedTypePropertyName);
    private static readonly PropertyInfo MethodHandleProperty = MethodBase.GetRuntimeProperty(MethodHandlePropertyName);
#endif

    internal static bool IsMethod(MethodBase method)
    {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
        return method.MemberType == MemberTypes.Method;
#else
        AssertSupport(MemberTypeProperty, MemberTypePropertyName, MethodBase.FullName);

        return (int)MemberTypeProperty.GetValue(method) == 8;
#endif
    }

    internal static Type GetReflectedType(MethodBase method)
    {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
        return method.ReflectedType;
#else
        AssertSupport(MemberTypeProperty, ReflectedTypePropertyName, MethodBase.FullName);

        return ReflectedTypeProperty.GetValue(method) as Type;
#endif
    }

    internal static RuntimeMethodHandle GetMethodHandle(MethodBase method)
    {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
        return method.MethodHandle;
#else
        AssertSupport(MemberTypeProperty, MethodHandlePropertyName, MethodBase.FullName);

        return (RuntimeMethodHandle)MethodHandleProperty.GetValue(method);
#endif
    }
}
