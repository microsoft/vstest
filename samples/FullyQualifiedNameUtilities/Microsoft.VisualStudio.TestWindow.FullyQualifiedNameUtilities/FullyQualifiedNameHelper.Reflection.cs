using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.VisualStudio.TestWindow.FullyQualifiedNameUtilities
{
    public static partial class FullyQualifiedNameHelper
    {
        public static void GetFullyQualifiedName(MethodBase method, out string fullTypeName, out string fullMethodName)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (method.MemberType != MemberTypes.Method)
            {
                throw new NotSupportedException(nameof(method));
            }

            var semanticType = method.ReflectedType;
            if (semanticType.IsGenericType)
            {
                // The type might have some of its generic parameters specified, so make
                // sure we are working with the open form of the generic type.
                semanticType = semanticType.GetGenericTypeDefinition();
                // The method might have some of its parameters specified by the original closed type 
                // declaration. Here we use the method handle (basically metadata token) to create
                // a new method reference using the open form of the reflected type. The intent is
                // to strip all generic type parameters.
                method = MethodBase.GetMethodFromHandle(method.MethodHandle, semanticType.TypeHandle);
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

        public static MethodBase GetMethodFromFullyQualifiedName(Assembly assembly, string fullTypeName, string fullMethodName)
        {
            var type = assembly.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
            if (type == null)
            {
                string message = String.Format(CultureInfo.CurrentCulture, FullyQualifiedNameMessages.ErrorTypeNotFound, fullTypeName);
                throw new InvalidQualifiedNameException(message);
            }

            MethodInfo method = null;
            FullyQualifiedNameParser.ParseMethodName(fullMethodName, out var methodName, out var methodArity, out var parameterTypes);
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                method = FindMethod(type, methodName, methodArity, parameterTypes);
            }

            if (method == null)
            {
                string message = String.Format(CultureInfo.CurrentCulture, FullyQualifiedNameMessages.ErrorMethodNotFound, methodName, fullTypeName);
                throw new InvalidQualifiedNameException(message);
            }

            return method;
        }

        private static MethodInfo FindMethod(Type type, string methodName, int methodArity, string[] parameterTypes)
        {
            bool filter(MemberInfo mbr, object param)
            {
                var method = mbr as MethodInfo;
                if (method.Name != methodName ||
                    method.GetGenericArguments().Length != methodArity)
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

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var methods = type.FindMembers(MemberTypes.Method, bindingFlags, filter, null);
            return (MethodInfo)methods.SingleOrDefault();
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
                if (type.DeclaringMethod != null)
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
            var genargs = type.GetGenericArguments();
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
