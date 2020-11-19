// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ManagedNameUtilities
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Extensions;

    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public static partial class ManagedNameHelper
    {
        /// <summary>
        /// Gets fully qualified type and method name from given MethodBase instance.
        /// </summary>
        /// <param name="method">
        /// A MethodBase instance to get fully qualified type and method name.
        /// </param>
        /// <param name="fullTypeName">
        /// When this method returns, contains the fully qualified type name of the <paramref name="method"/>. 
        /// This parameter is passed uninitialized; any value originally supplied in result will be overwritten.
        /// </param>
        /// <param name="fullMethodName">
        /// When this method returns, contains the fully qualified method name of the <paramref name="method"/>. 
        /// This parameter is passed uninitialized; any value originally supplied in result will be overwritten.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> is null.</exception>
        /// <exception cref="NotSupportedException"><paramref name="method"/> must describe a method.</exception>
        /// <exception cref="NotImplementedException">
        /// Required functionality on <paramref name="method"/> is missing on the current platform.
        /// </exception>
        public static void GetManagedName(MethodBase method, out string fullTypeName, out string fullMethodName)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (!method.IsMethod())
            {
                throw new NotSupportedException(nameof(method));
            }

            var semanticType = method.GetReflectedType();
            if (semanticType.IsGenericType())
            {
                // The type might have some of its generic parameters specified, so make
                // sure we are working with the open form of the generic type.
                semanticType = semanticType.GetGenericTypeDefinition();

                // The method might have some of its parameters specified by the original closed type 
                // declaration. Here we use the method handle (basically metadata token) to create
                // a new method reference using the open form of the reflected type. The intent is
                // to strip all generic type parameters.
                var methodHandle = method.GetMethodHandle();
                method = MethodBase.GetMethodFromHandle(methodHandle, semanticType.TypeHandle);
            }

            if (method.IsGenericMethod)
            {
                // If this method is generic, then convert to the generic method definition
                // so that we get the open generic type definitions for parameters.
                method = ((MethodInfo)method).GetGenericMethodDefinition();
            }

            var methodBuilder = new StringBuilder();
            var typeBuilder = new StringBuilder();

            // Namespace and Type Name (with arity designation)
            AppendTypeString(typeBuilder, semanticType, closedType: false);

            // Method Name with method arity
            methodBuilder.Append(method.Name);
            var arity = method.GetGenericArguments().Length;
            if (arity > 0)
            {
                methodBuilder.Append('`');
                methodBuilder.Append(arity);
            }

            // Type Parameters
            var paramList = method.GetParameters();
            if (paramList.Any())
            {
                methodBuilder.Append('(');
                foreach (var p in paramList)
                {
                    AppendTypeString(methodBuilder, p.ParameterType, closedType: true);
                    methodBuilder.Append(',');
                }
                // Replace the last ',' with ')'
                methodBuilder[methodBuilder.Length - 1] = ')';
            }

            fullTypeName = typeBuilder.ToString();
            fullMethodName = methodBuilder.ToString();
        }

        /// <summary>
        /// Gets the <see cref="MethodBase"/> object with the specified <paramref name="fullTypeName"/> 
        /// and <paramref name="fullMethodName"/> in the <paramref name="assembly"/> instance.
        /// </summary>
        /// <param name="assembly">
        /// An <see cref="Assembly" /> instance to search in.
        /// </param>
        /// <param name="fullTypeName">
        /// The fully qualified name of the type.
        /// </param>
        /// <param name="fullMethodName">
        /// The fully qualified name of the method.
        /// </param>
        /// <returns>
        /// A <see cref="MethodBase" /> object that represents specified parameters, throws if null.
        /// </returns>
        /// <exception cref="InvalidManagedNameException">
        /// Values specified with <paramref name="fullTypeName"/> and <paramref name="fullMethodName"/> 
        /// does not correspond to a method in the <paramref name="assembly"/> instance, or malformed.
        /// </exception>
        public static MethodBase GetManagedName(Assembly assembly, string fullTypeName, string fullMethodName)
        {
            Type type;

#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            type = assembly.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
#else
            try
            {
                type = assembly.GetType(fullTypeName);
            }
            catch
            {
                type = null;
            }
#endif

            if (type == null)
            {
                string message = String.Format(CultureInfo.CurrentCulture, ManagedNameMessages.ErrorTypeNotFound, fullTypeName);
                throw new InvalidManagedNameException(message);
            }

            MethodInfo method = null;
            ManagedNameParser.ParseMethodName(fullMethodName, out var methodName, out var methodArity, out var parameterTypes);
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                method = FindMethod(type, methodName, methodArity, parameterTypes);
            }

            if (method == null)
            {
                string message = String.Format(CultureInfo.CurrentCulture, ManagedNameMessages.ErrorMethodNotFound, methodName, fullTypeName);
                throw new InvalidManagedNameException(message);
            }

            return method;
        }

        private static MethodInfo FindMethod(Type type, string methodName, int methodArity, string[] parameterTypes)
        {
            bool filter(MemberInfo mbr, object param)
            {
                var method = mbr as MethodInfo;
                if (method.Name != methodName || method.GetGenericArguments().Length != methodArity)
                {
                    return false;
                }

                var paramList = method.GetParameters();
                if (paramList.Length == 0 && parameterTypes == null)
                {
                    return true;
                }
                else if (parameterTypes == null || paramList.Length != parameterTypes.Length)
                {
                    return false;
                }

                for (int i = 0; i < paramList.Length; i++)
                {
                    if (TypeString(paramList[i].ParameterType, closedType: true) != parameterTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var methods = type.FindMembers(MemberTypes.Method, bindingFlags, filter, null);
            return (MethodInfo)methods.SingleOrDefault();
#else
            return type.GetRuntimeMethods().Where(m => filter(m, null)).SingleOrDefault();
#endif
        }

        private static void AppendTypeString(StringBuilder b, Type type, bool closedType)
        {
            if (type.IsArray)
            {
                AppendTypeString(b, type.GetElementType(), closedType);
                b.Append('[');
                for (int i = 0; i < type.GetArrayRank() - 1; i++)
                {
                    b.Append(',');
                }
                b.Append(']');
            }
            else if (type.IsGenericParameter)
            {
                if (type.GetDeclaringMethod() != null)
                {
                    b.Append('!');
                }
                b.Append('!');
                b.Append(type.GenericParameterPosition);
            }
            else
            {
                b.Append(type.Namespace);
                b.Append('.');

                AppendNestedTypeName(b, type);

                if (closedType)
                {
                    AppendGenericTypeParameters(b, type);
                }
            }
        }

        private static void AppendNestedTypeName(StringBuilder b, Type type)
        {
            if (type.IsNested)
            {
                AppendNestedTypeName(b, type.DeclaringType);
                b.Append('+');
            }
            b.Append(type.Name);
        }

        private static void AppendGenericTypeParameters(StringBuilder b, Type type)
        {
            Type[] genargs;

#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            genargs = type.GetGenericArguments();
#else
            genargs = type.GetTypeInfo().GenericTypeArguments;
#endif

            if (genargs.Any())
            {
                b.Append('<');
                foreach (var argType in genargs)
                {
                    AppendTypeString(b, argType, closedType: true);
                    b.Append(',');
                }
                // Replace the last ',' with '>'
                b[b.Length - 1] = '>';
            }
        }

        private static string TypeString(Type type, bool closedType)
        {
            var builder = new StringBuilder();
            AppendTypeString(builder, type, closedType);
            return builder.ToString();
        }
    }
}
