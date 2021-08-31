// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities.ManagedNameUtilities
{
    using Microsoft.TestPlatform.AdapterUtilities.Resources;
    using Microsoft.TestPlatform.AdapterUtilities.Helpers;

    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Text;

#if !NET20
    using System.Linq;
#endif

    public static partial class ManagedNameHelper
    {
        /// <summary>
        /// Gets fully qualified managed type and method name from given <see href="MethodBase" /> instance.
        /// </summary>
        /// <param name="method">
        /// A <see cref="MethodBase" /> instance to get fully qualified managed type and method name.
        /// </param>
        /// <param name="managedTypeName">
        /// When this method returns, contains the fully qualified managed type name of the <paramref name="method"/>.
        /// This parameter is passed uninitialized; any value originally supplied in result will be overwritten.
        /// The format is defined in <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md#managedtype-property">the RFC</see>.
        /// </param>
        /// <param name="managedMethodName">
        /// When this method returns, contains the fully qualified managed method name of the <paramref name="method"/>.
        /// This parameter is passed uninitialized; any value originally supplied in result will be overwritten.
        /// The format is defined in <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md#managedmethod-property">the RFC</see>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="method"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="method"/> must describe a method.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// Required functionality on <paramref name="method"/> is missing on the current platform.
        /// </exception>
        /// <remarks>
        /// More information about <paramref name="managedTypeName"/> and <paramref name="managedMethodName"/> can be found in
        /// <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md">the RFC</see>.
        /// </remarks>
        public static void GetManagedName(MethodBase method, out string managedTypeName, out string managedMethodName)
            => GetManagedName(method, out managedTypeName, out managedMethodName, out _);

        /// <summary>
        /// Gets fully qualified managed type and method name from given <see href="MethodBase" /> instance.
        /// </summary>
        /// <param name="method">
        /// A <see cref="MethodBase" /> instance to get fully qualified managed type and method name.
        /// </param>
        /// <param name="managedTypeName">
        /// When this method returns, contains the fully qualified managed type name of the <paramref name="method"/>.
        /// This parameter is passed uninitialized; any value originally supplied in result will be overwritten.
        /// The format is defined in <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md#managedtype-property">the RFC</see>.
        /// </param>
        /// <param name="managedMethodName">
        /// When this method returns, contains the fully qualified managed method name of the <paramref name="method"/>.
        /// This parameter is passed uninitialized; any value originally supplied in result will be overwritten.
        /// The format is defined in <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md#managedmethod-property">the RFC</see>.
        /// </param>
        /// <param name="hierarchyValues">
        /// When this method returns, contains the default test hierarchy values of the <paramref name="method"/>.
        /// This parameter is passed uninitialized; any value originally supplied in result will be overwritten.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="method"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="method"/> must describe a method.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// Required functionality on <paramref name="method"/> is missing on the current platform.
        /// </exception>
        /// <remarks>
        /// More information about <paramref name="managedTypeName"/> and <paramref name="managedMethodName"/> can be found in
        /// <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md">the RFC</see>.
        /// </remarks>
        public static void GetManagedName(MethodBase method, out string managedTypeName, out string managedMethodName, out string[] hierarchyValues)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (!ReflectionHelpers.IsMethod(method))
            {
                throw new NotSupportedException(nameof(method));
            }

            var semanticType = ReflectionHelpers.GetReflectedType(method);
            if (ReflectionHelpers.IsGenericType(semanticType))
            {
                // The type might have some of its generic parameters specified, so make
                // sure we are working with the open form of the generic type.
                semanticType = semanticType.GetGenericTypeDefinition();

                // The method might have some of its parameters specified by the original closed type
                // declaration. Here we use the method handle (basically metadata token) to create
                // a new method reference using the open form of the reflected type. The intent is
                // to strip all generic type parameters.
                var methodHandle = ReflectionHelpers.GetMethodHandle(method);
                method = MethodBase.GetMethodFromHandle(methodHandle, semanticType.TypeHandle);
            }

            if (method.IsGenericMethod)
            {
                // If this method is generic, then convert to the generic method definition
                // so that we get the open generic type definitions for parameters.
                method = ((MethodInfo)method).GetGenericMethodDefinition();
            }

            var typeBuilder = new StringBuilder();
            var methodBuilder = new StringBuilder();

            // Namespace and Type Name (with arity designation)
            var hierarchyPos = AppendTypeString(typeBuilder, semanticType, closedType: false);

            // Method Name with method arity
            var arity = method.GetGenericArguments().Length;
            AppendMethodString(methodBuilder, method.Name, arity);
            if (arity > 0)
            {
                methodBuilder.Append('`');
                methodBuilder.Append(arity);
            }

            // Type Parameters
            var paramList = method.GetParameters();
            if (paramList.Length != 0)
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

            managedTypeName = typeBuilder.ToString();
            managedMethodName = methodBuilder.ToString();
            hierarchyValues = new[] {
                managedTypeName.Substring(hierarchyPos[0], hierarchyPos[1] - hierarchyPos[0]),
                managedTypeName.Substring(hierarchyPos[1] + 1, hierarchyPos[2] - hierarchyPos[1] - 1),
            };
        }

        /// <summary>
        /// Gets the <see cref="MethodBase"/> object with the specified <paramref name="managedTypeName"/>
        /// and <paramref name="managedMethodName"/> in the <paramref name="assembly"/> instance.
        /// </summary>
        /// <param name="assembly">
        /// An <see cref="Assembly" /> instance to search in.
        /// </param>
        /// <param name="managedTypeName">
        /// The fully qualified managed name of the type.
        /// The format is defined in <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md#managedtype-property">the RFC</see>.
        /// </param>
        /// <param name="managedMethodName">
        /// The fully qualified managed name of the method.
        /// The format is defined in <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md#managedmethod-property">the RFC</see>.
        /// </param>
        /// <returns>
        /// A <see cref="MethodBase" /> object that represents specified parameters, throws if null.
        /// </returns>
        /// <exception cref="InvalidManagedNameException">
        /// Values specified with <paramref name="managedTypeName"/> and <paramref name="managedMethodName"/>
        /// does not correspond to a method in the <paramref name="assembly"/> instance, or malformed.
        /// </exception>
        /// <remarks>
        /// More information about <paramref name="managedTypeName"/> and <paramref name="managedMethodName"/> can be found in
        /// <see href="https://github.com/microsoft/vstest-docs/blob/main/RFCs/0017-Managed-TestCase-Properties.md">the RFC</see>.
        /// </remarks>
        public static MethodBase GetMethod(Assembly assembly, string managedTypeName, string managedMethodName)
        {
            Type type;

            var parsedManagedTypeName = ReflectionHelpers.ParseEscapedString(managedTypeName);

#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            type = assembly.GetType(parsedManagedTypeName, throwOnError: false, ignoreCase: false);
#else
            try
            {
                type = assembly.GetType(parsedManagedTypeName);
            }
            catch
            {
                type = null;
            }
#endif

            if (type == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ErrorTypeNotFound, parsedManagedTypeName);
                throw new InvalidManagedNameException(message);
            }

            MethodInfo method = null;
            ManagedNameParser.ParseManagedMethodName(managedMethodName, out var methodName, out var methodArity, out var parameterTypes);

#if NET20 || NET35
            if (!IsNullOrWhiteSpace(methodName))
#else
            if (!string.IsNullOrWhiteSpace(methodName))
#endif
            {
                method = FindMethod(type, methodName, methodArity, parameterTypes);
            }

            if (method == null)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.ErrorMethodNotFound, methodName, managedTypeName);
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
                    if (GetTypeString(paramList[i].ParameterType, closedType: true) != parameterTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            MemberInfo[] methods;

#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            methods = type.FindMembers(MemberTypes.Method, bindingFlags, filter, null);
#else
            methods = type.GetRuntimeMethods().Where(m => filter(m, null)).ToArray();
#endif

#if NET20
            return (MethodInfo)SingleOrDefault(methods);
#else
            return (MethodInfo)methods.SingleOrDefault();
#endif
        }

        private static int[] AppendTypeString(StringBuilder b, Type type, bool closedType)
        {
            int[] hierarchies = null;

            if (type.IsArray)
            {
                hierarchies = AppendTypeString(b, type.GetElementType(), closedType);
                b.Append('[');
                for (int i = 0; i < type.GetArrayRank() - 1; i++)
                {
                    b.Append(',');
                }
                b.Append(']');
            }
            else if (type.IsGenericParameter)
            {
                if (ReflectionHelpers.GetDeclaringMethod(type) != null)
                {
                    b.Append('!');
                }
                b.Append('!');
                b.Append(type.GenericParameterPosition);
            }
            else
            {
                hierarchies = new int[3];
                hierarchies[0] = b.Length;

                AppendNamespace(b, type.Namespace);
                hierarchies[1] = b.Length;

                b.Append('.');

                AppendNestedTypeName(b, type);
                if (closedType)
                {
                    AppendGenericTypeParameters(b, type);
                }
                hierarchies[2] = b.Length;
            }

            return hierarchies;
        }

        private static void AppendNamespace(StringBuilder b, string namespaceString)
        {
            int start = 0;
            bool shouldEscape = false;

            for (int i = 0; i <= namespaceString.Length; i++)
            {
                if (i == namespaceString.Length || namespaceString[i] == '.')
                {
                    if (start != 0)
                    {
                        b.Append('.');
                    }

                    var part = namespaceString.Substring(start, i - start);
                    if (shouldEscape)
                    {
                        NormalizeAndAppendString(b, part);
                        shouldEscape = false;
                    }
                    else
                    {
                        b.Append(part);
                    }

                    start = i + 1;
                    continue;
                }

                shouldEscape = shouldEscape || NeedsEscaping(namespaceString[i], i - start);
            }
        }

        private static void AppendMethodString(StringBuilder methodBuilder, string name, int methodArity)
        {
            var arityStart = name.LastIndexOf('`');
            var arity = 0;
            if (arityStart > 0)
            {
                arityStart++;
                var arityString = name.Substring(arityStart, name.Length - arityStart);
                if (int.TryParse(arityString, out arity))
                {
                    if (arity == methodArity)
                    {
                        name = name.Substring(0, arityStart - 1);
                    }
                }
            }

            if (IsNormalized(name))
            {
                methodBuilder.Append(name);
            }
            else
            {
                NormalizeAndAppendString(methodBuilder, name);
            }

            if (arity > 0 && methodArity == arity)
            {
                methodBuilder.Append($"`{arity}");
            }
        }

        private static void NormalizeAndAppendString(StringBuilder b, string name)
        {
            b.Append('\'');
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (NeedsEscaping(c, i))
                {
                    if (c == '\\' || c == '\'')
                    {
                        // var encoded = Convert.ToString(((uint)c), 16);
                        // b.Append("\\u");
                        // b.Append('0', 4 - encoded.Length);
                        // b.Append(encoded);

                        b.Append('\\');
                        b.Append(c);
                        continue;
                    }
                }

                b.Append(c);
            }
            b.Append('\'');
        }

        private static int AppendNestedTypeName(StringBuilder b, Type type)
        {
            var outerArity = 0;
            if (type.IsNested)
            {
                outerArity = AppendNestedTypeName(b, type.DeclaringType);
                b.Append('+');
            }

            var typeName = type.Name;
            var stars = 0;
            if (type.IsPointer)
            {
                for (int i = typeName.Length - 1; i > 0; i--)
                {
                    if (typeName[i] != '*')
                    {
                        stars = typeName.Length - i - 1;
                        typeName = typeName.Substring(0, i + 1);
                        break;
                    }
                }
            }

            var info = type.GetTypeInfo();
            var arity = !info.IsGenericType
                      ? 0
                      : info.GenericTypeParameters.Length > 0
                        ? info.GenericTypeParameters.Length
                        : info.GenericTypeArguments.Length;

            AppendMethodString(b, typeName, arity - outerArity);
            b.Append('*', stars);
            return arity;
        }

        private static void AppendGenericTypeParameters(StringBuilder b, Type type)
        {
            Type[] genargs;

#if !NETSTANDARD1_0 && !NETSTANDARD1_3 && !WINDOWS_UWP
            genargs = type.GetGenericArguments();
#else
            genargs = type.GetTypeInfo().GenericTypeArguments;
#endif

            if (genargs.Length != 0)
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

        private static bool IsNormalized(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (NeedsEscaping(s[i], i) && s[i] != '.')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool NeedsEscaping(char c, int pos)
        {
            if (pos == 0 && char.IsDigit(c))
            {
                return true;
            }

            if (c == '_'
              || char.IsLetterOrDigit(c) // Lu, Ll, Lt, Lm, Lo, or Nl
              )
            {
                return false;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark        // Mn
             || category == UnicodeCategory.SpacingCombiningMark  // Mc
             || category == UnicodeCategory.ConnectorPunctuation  // Pc
             || category == UnicodeCategory.Format)               // Cf
            {
                return false;
            }

            return true;
        }

        private static string GetTypeString(Type type, bool closedType)
        {
            var builder = new StringBuilder();
            AppendTypeString(builder, type, closedType);
            return builder.ToString();
        }

#if NET20

        // the method is mostly copied from
        // https://github.com/dotnet/runtime/blob/c0840723b382bcfa67b35839af8572fcd38f1d13/src/libraries/System.Linq/src/System/Linq/Single.cs#L86
        public static TSource SingleOrDefault<TSource>(System.Collections.Generic.IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is System.Collections.Generic.IList<TSource> list)
            {
                switch (list.Count)
                {
                    case 0:
                        return default;
                    case 1:
                        return list[0];
                }
            }
            else
            {
                using (System.Collections.Generic.IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        return default;
                    }

                    TSource result = e.Current;
                    if (!e.MoveNext())
                    {
                        return result;
                    }
                }
            }

            throw new InvalidOperationException("MoreThanOneElement");
        }
#endif

#if NET20 || NET35
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value is null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    return false;
                }
            }

            return true;
        }
#endif
    }
}
