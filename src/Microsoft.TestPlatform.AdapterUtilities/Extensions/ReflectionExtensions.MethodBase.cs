// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities.Extensions
{
    using System;
    using System.Reflection;

    internal static partial class ReflectionExtensions
    {
#if NETSTANDARD1_0 || NETSTANDARD1_3 || WINDOWS_UWP
        private static readonly Type methodBase = typeof(MethodBase);

        private const string MemberTypePropertyName = "MemberType";
        private const string ReflectedTypePropertyName = "ReflectedType";
        private const string MethodHandlePropertyName = "MethodHandle";

        private static readonly PropertyInfo memberTypeProperty = methodBase.GetRuntimeProperty(MemberTypePropertyName);
        private static readonly PropertyInfo reflectedTypeProperty = methodBase.GetRuntimeProperty(ReflectedTypePropertyName);
        private static readonly PropertyInfo methodHandleProperty = methodBase.GetRuntimeProperty(MethodHandlePropertyName);
#endif

        public static bool IsMethod(this MethodBase method)
        {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            return method.MemberType == MemberTypes.Method;
#else
            AssertSupport(memberTypeProperty, MemberTypePropertyName, methodBase.FullName);

            return (int)memberTypeProperty.GetValue(method) == 8;
#endif
        }

        public static Type GetReflectedType(this MethodBase method)
        {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            return method.ReflectedType;
#else
            AssertSupport(memberTypeProperty, ReflectedTypePropertyName, methodBase.FullName);

            return reflectedTypeProperty.GetValue(method) as Type;
#endif
        }

        public static RuntimeMethodHandle GetMethodHandle(this MethodBase method)
        {
#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            return method.MethodHandle;
#else
            AssertSupport(memberTypeProperty, MethodHandlePropertyName, methodBase.FullName);

            return (RuntimeMethodHandle)methodHandleProperty.GetValue(method);
#endif
        }
    }
}
