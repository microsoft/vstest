using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.TestWindow.FullyQualifiedNameUtilities
{
    public static partial class FullyQualifiedNameHelper
    {
        public static void GetFullyQualifiedName(INamedTypeSymbol type, IMethodSymbol method, out string fullTypeName, out string fullMethodName)
            => FullyQualifiedNameSymbolVisitor.GetFullyQualifiedName(type, method, out fullTypeName, out fullMethodName);

        public static IMethodSymbol GetMethodFromFullyQualifiedName(Compilation compilation, string fullTypeName, string fullMethodName)
        {
            var type = compilation.GetTypeByMetadataName(fullTypeName);
            if (type == null)
            {
                string message = String.Format(CultureInfo.CurrentCulture, FullyQualifiedNameMessages.ErrorTypeNotFound, fullTypeName);
                throw new InvalidQualifiedNameException(message);
            }

            IMethodSymbol method = null;
            FullyQualifiedNameParser.ParseMethodName(fullMethodName, out string methodName, out int methodArity, out string[] parameterTypes);
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                var parameterCount = parameterTypes == null ? 0 : parameterTypes.Length;
                method = FindMethod(type, methodName, methodArity, parameterCount, fullMethodName);
            }

            if (method == null)
            {
                string message = String.Format(CultureInfo.CurrentCulture, FullyQualifiedNameMessages.ErrorMethodNotFound, methodName, fullTypeName);
                throw new InvalidQualifiedNameException(message);
            }

            return method;
        }

        private static IMethodSymbol FindMethod(INamedTypeSymbol type, string methodName, int methodArity, int parameterCount, string fullMethodName)
            => FindMethodInternal(type, type, methodName, methodArity, parameterCount, fullMethodName);

        private static IMethodSymbol FindMethodInternal(INamedTypeSymbol type, INamedTypeSymbol originalType, string methodName, int methodArity, int parameterCount, string fullMethodName)
        {
            bool filter(IMethodSymbol candidate)
            {
                // Eliminate methods with mismatching names and arities.
                if (candidate.Name != methodName || candidate.Arity != methodArity)
                {
                    return false;
                }

                // Eliminate methods with mismatching parameter counts.
                if (candidate.Parameters.Length != parameterCount)
                {
                    return false;
                }

                if (parameterCount > 0)
                {
                    // Compute and compare full name (which will include parameter types).
                    FullyQualifiedNameSymbolVisitor.GetFullyQualifiedName(originalType, candidate, out var _, out var candidateFullMethodName);
                    if (candidateFullMethodName != fullMethodName)
                    {
                        return false;
                    }
                }

                // Name, arity and parameter types match.
                return true;
            }

            var candidates = type.GetMembers(methodName).OfType<IMethodSymbol>();
            var match = candidates.SingleOrDefault(filter);
            if (match == null && type.BaseType != null && type.BaseType.SpecialType != SpecialType.System_Object)
            {
                // We didn't find the requested method in the supplied type. Look in its base type.
                match = FindMethodInternal(type.BaseType, originalType, methodName, methodArity, parameterCount, fullMethodName);
            }

            return match;
        }

        private class FullyQualifiedNameSymbolVisitor : SymbolVisitor
        {
            /// <summary>
            /// Cache that remembers fully qualified names for previously encountered types.
            /// </summary>
            private readonly Dictionary<ITypeSymbol, string> _seenTypes = new Dictionary<ITypeSymbol, string>();

            /// <summary>
            /// Cache that remembers type argument substitutions for type parameters on the defining
            /// type of the supplied method.
            /// </summary>
            // Example: Consider the following case -
            //
            // class Base<T> { public void Test(T t) }
            // class Derived<U, V> : Base<V> { }
            //
            // The fully qualified name for Derived<U, V>.Test(V t) must be computed as Derived`2.Test(!1).
            // To do this, when visiting the containing type of the method (Base<V>), we need to remember in
            // the below cache that the type parameter T for the method's defining type Base<T> was substituted
            // with type argument V (which itself is a type parameter on type Derived<U, V>). When we later
            // encounter T in the method parameter (when traversing the open form of the method definition),
            // we can use this cache to identify that T really maps to V and that we need to encode the type
            // of the method parameter as "!1".
            private readonly Dictionary<ITypeParameterSymbol, ITypeSymbol> _typeParameterSubstitutionsForDefiningType
                = new Dictionary<ITypeParameterSymbol, ITypeSymbol>();

            /// <summary>
            /// Index that must be assigned for next encountered type-level type parameter.
            /// </summary>
            private int _nextTypeLevelTypeParameterIndex = 0;

            /// <summary>
            /// Index that must be assigned for next encountered method-level type parameter.
            /// </summary>
            private int _nextMethodLevelTypeParameterIndex = 0;

            /// <summary>
            /// <see langword="true"/>if visiting the containing type of the method, <see langword="false"/> otherwise.
            /// </summary>
            /// <remarks>
            /// If the method is defined in a base type but the type being inspected is a derived
            /// type then this will only be <see langword="true"/> when visiting the derived type. 
            /// </remarks>
            private bool _visitingContainingType = false;

            /// <summary>
            /// <see langword="true"/> if visiting type parameters of the method, <see langword="false"/> otherwise.
            /// </summary>
            private bool _visitingMethodTypeParameters = false;

            /// <summary>
            /// Stores name of the containing type of the supplied method.
            /// </summary>
            /// <remarks>
            /// If the method is defined in a base type but the type being inspected is a derived
            /// type then this will contain the name of the derived type. 
            /// </remarks>
            private string _containingTypeName;

            /// <summary>
            /// A <see cref="StringBuilder"/> that is used to construct fully qualified name of the supplied method.
            /// </summary>
            private readonly StringBuilder _methodNameBuilder = new StringBuilder();

            /// <summary>
            /// A <see cref="StringBuilder"/> that is used to construct fully qualified name of the type
            /// that is being visited currently.
            /// </summary>
            private StringBuilder _currentTypeNameBuilder;

            /// <summary>
            /// A <see cref="Stack{T}"/> that remembers <see cref="_currentTypeNameBuilder"/> for a type when
            /// visiting generic type arguments / type parameters of this type.
            /// </summary>
            private readonly Stack<StringBuilder> _typeNameBuilderStack = new Stack<StringBuilder>();

            public static void GetFullyQualifiedName(INamedTypeSymbol type, IMethodSymbol method, out string fullTypeName, out string fullMethodName)
            {
                if (type == null || method == null)
                {
                    throw new ArgumentNullException(nameof(method));
                }

                if (type.Kind == SymbolKind.ErrorType)
                {
                    throw new ArgumentException(nameof(method));
                }

                // TODO: Will we need to support any other kinds of methods?
                if (method.MethodKind != MethodKind.Ordinary && method.MethodKind != MethodKind.ExplicitInterfaceImplementation)
                {
                    throw new NotSupportedException(nameof(method));
                }

                var visitor = new FullyQualifiedNameSymbolVisitor();

                // Visit supplied type first so that we can record the positions (!0, !1 etc.)
                // of type parameters that will appear later in the method parameter types.
                visitor.VisitContainingType(type);
                if (type != method.ContainingType)
                {
                    // The defining type for the method may be different from the containing type
                    // in the case where the defining type is a base type of the containing type.
                    // Visit the defining type now so that we can figure out the type argument 
                    // substitutions for the type parameters present on the defining type.
                    visitor.VisitTypeArgumentsForDefiningType(method.ContainingType);
                }

                // Visit the open generic form of the method definition so that we can see any type
                // parameters from containing type and the method itself that appear in the method
                // parameter types.
                visitor.Visit(method.OriginalDefinition);
                visitor.GetFullyQualifiedName(out fullTypeName, out fullMethodName);
            }

            private void GetFullyQualifiedName(out string fullTypeName, out string fullMethodName)
            {
                fullTypeName = _containingTypeName;
                fullMethodName = _methodNameBuilder.ToString();
            }

            /// <summary>
            /// Visits the containg type of the method.
            /// </summary>
            /// <remarks>
            /// The containing type and the defining type of the method are usually one and the same except
            /// in the case where a method is defined in a base class, but the type being inspected is
            /// a derived type.
            /// </remarks>
            private void VisitContainingType(INamedTypeSymbol containingType)
            {
                _visitingContainingType = true;
                containingType.Accept(this);
                _visitingContainingType = false;
            }

            /// <summary>
            /// Visits type parameters present on the defining type of the method and remember the type
            /// argument types that these type parameters were substituted with.
            /// </summary>
            /// <remarks>
            /// The containing type and the defining type of the method are usually one and the same except
            /// in the case where a method is defined in a base class, but the type being inspected is
            /// a derived type.
            /// </remarks>
            private void VisitTypeArgumentsForDefiningType(INamedTypeSymbol definingType)
            {
                Debug.Assert(!_visitingContainingType);

                int typeArgumentCount = definingType.TypeArguments.Length;
                if (typeArgumentCount > 0)
                {
                    for (var i = 0; i < typeArgumentCount; ++i)
                    {
                        // Example: Consider the following case -
                        //
                        // class Base<T> { public void Test(T t) }
                        // class Derived<U, V> : Base<V> { }
                        //
                        // The fully qualified name for Derived<U, V>.Test(V t) must be computed as Derived`2.Test(!1).
                        // To do this, when visiting the containing type of the method (Base<V>), we need to remember in
                        // the below cache that the type parameter T for the method's defining type Base<T> was substituted
                        // with type argument V (which itself is a type parameter on type Derived<U, V>). When we later
                        // encounter T in the method parameter (when traversing the open form of the method definition),
                        // we can use this cache to identify that T really maps to V and that we need to encode the type
                        // of the method parameter as "!1".
                        _typeParameterSubstitutionsForDefiningType[definingType.TypeParameters[i]] = definingType.TypeArguments[i];
                    }
                }
            }

            public override void VisitNamespace(INamespaceSymbol @namespace)
            {
                if (!@namespace.IsGlobalNamespace)
                {
                    if (!@namespace.ContainingNamespace.IsGlobalNamespace)
                    {
                        @namespace.ContainingNamespace.Accept(this);
                    }

                    _currentTypeNameBuilder.Append(@namespace.Name);
                    _currentTypeNameBuilder.Append('.');
                }
            }

            public override void VisitNamedType(INamedTypeSymbol namedType)
            {
                if (namedType.ContainingType == null)
                {
                    VisitNonNestedType(namedType);
                }
                else
                {
                    VisitNestedType(namedType);
                }
            }

            /// <summary>
            /// Visit a type that is defined directly inside a namespace (i.e. a type whose definition is not nested within that of another type).
            /// </summary>
            private void VisitNonNestedType(INamedTypeSymbol namedType)
            {
                Debug.Assert(namedType.ContainingType == null);

                if (!TryAppendTypeName(namedType))
                {
                    BeginVisitingType();

                    namedType.ContainingNamespace.Accept(this);

                    _currentTypeNameBuilder.Append(namedType.MetadataName);

                    if (_visitingContainingType)
                    {
                        VisitTypeParameters(namedType.TypeParameters);
                    }
                    else
                    {
                        VisitTypeArguments(namedType.TypeArguments);
                    }

                    EndVisitingType(namedType);
                }
            }

            /// <summary>
            /// Visit a type whose definition is nested within that of another type.
            /// </summary>
            private void VisitNestedType(INamedTypeSymbol namedType)
            {
                Debug.Assert(namedType.ContainingType != null);

                if (!TryAppendTypeName(namedType))
                {
                    BeginVisitingType();

                    // Example: Consider the following code -
                    // 
                    // class Outer<T> { class Inner<U> { } }
                    // class TestClass { public void Test<V, W>(Outer<V>.Inner<W> i) { } }
                    //
                    // The fully qualified name for TestClass.Test<V, W>(Outer<V>.Inner<W> i) must be computed as
                    // TestClass.Test`2(Outer`1+Inner`1<!!0, !!1>). So when visiting nested types we need to visit
                    // the type arguments on containing types separately after we have visited all the containing
                    // types themselves.

                    var outermostType = namedType;
                    var nestedTypeHierarchy = new List<INamedTypeSymbol>() { namedType };
                    while (outermostType.ContainingType != null)
                    {
                        outermostType = outermostType.ContainingType;
                        nestedTypeHierarchy.Insert(0, outermostType);
                    }

                    outermostType.ContainingNamespace.Accept(this);

                    var i = 0;
                    foreach (var type in nestedTypeHierarchy)
                    {
                        _currentTypeNameBuilder.Append(type.MetadataName);
                        if (++i != nestedTypeHierarchy.Count)
                        {
                            _currentTypeNameBuilder.Append('+');
                        }
                    }

                    if (_visitingContainingType)
                    {
                        VisitTypeParameters(nestedTypeHierarchy.SelectMany(t => t.TypeParameters).ToImmutableArray());
                    }
                    else
                    {
                        VisitTypeArguments(nestedTypeHierarchy.SelectMany(t => t.TypeArguments).ToImmutableArray());
                    }

                    EndVisitingType(namedType);
                }
            }

            /// <summary>
            /// Visits type-level type parameters of the containing type of the supplied method
            /// or the method-level type parameters of the suplied method.
            /// </summary>
            private void VisitTypeParameters(IEnumerable<ITypeParameterSymbol> typeParameters)
            {
                Debug.Assert(_visitingContainingType || _visitingMethodTypeParameters);

                foreach (var typeParameter in typeParameters)
                {
                    typeParameter.Accept(this);
                }
            }

            /// <summary>
            /// Visits type arguments for generic types encountered within the parameter types of
            /// the supplied method.
            /// </summary>
            private void VisitTypeArguments(ImmutableArray<ITypeSymbol> typeArguments)
            {
                Debug.Assert(!_visitingContainingType && !_visitingMethodTypeParameters);

                var typeArgumentCount = typeArguments.Length;
                if (typeArgumentCount > 0)
                {
                    _currentTypeNameBuilder.Append('<');

                    for (int i = 0; i < typeArgumentCount; ++i)
                    {
                        typeArguments[i].Accept(this);
                        if (i != typeArgumentCount - 1)
                        {
                            _currentTypeNameBuilder.Append(',');
                        }
                    }

                    _currentTypeNameBuilder.Append('>');
                }
            }

            /// <summary>
            /// Visits either type parameter definitions (i.e. method-level or type-level type parameters for the supplied method)
            /// or usages (that appear within the supplied method's parameter types).
            /// </summary>
            public override void VisitTypeParameter(ITypeParameterSymbol typeParameter)
            {
                if (typeParameter.Kind == SymbolKind.ErrorType)
                {
                    throw new ArgumentException(nameof(typeParameter));
                }

                if (_visitingContainingType || _visitingMethodTypeParameters)
                {
                    // We are visiting type-level type parameters of the containing type of the supplied method
                    // or the method-level type parameters of the suplied method. We only need to remember the
                    // index (i.e. !0, !!0 etc.) that these type parameters should be substituted with when they
                    // are encountered again within parameter types of the method (when traversing the open form
                    // of the method definition).
                    if (_visitingContainingType)
                    {
                        _seenTypes[typeParameter.OriginalDefinition] = $"!{_nextTypeLevelTypeParameterIndex++}";
                    }
                    else if (_visitingMethodTypeParameters)
                    {
                        _seenTypes[typeParameter.OriginalDefinition] = $"!!{_nextMethodLevelTypeParameterIndex++}";
                    }
                }
                else
                {
                    // We are visiting type parameter usages that appear within the supplied method's parameter types.

                    if (WasTypeSeen(typeParameter))
                    {
                        // If we have previously encountered this type parameter (when visiting the type-level
                        // type parameters of the containing type of the supplied method or the method-level
                        // type parameters of the supplied method), we can simply spit out the indices we
                        // remembered above. 
                        TryAppendTypeName(typeParameter);
                    }
                    else if (_typeParameterSubstitutionsForDefiningType.TryGetValue(typeParameter, out var substitution))
                    {
                        // Example: Consider the following case -
                        //
                        // class Base<T> { public void Test(T t) }
                        // class Derived<U, V> : Base<V> { }
                        //
                        // The fully qualified name for Derived<U, V>.Test(V t) must be computed as Derived`2.Test(!1).
                        // To do this, when visiting the containing type of the method (Base<V>), we remember in the
                        // below cache that the type parameter T for the method's defining type Base<T> was substituted
                        // with type argument V (which itself is a type parameter on type Derived<U, V>). When we later
                        // encounter T in the method parameter below (when traversing the open form of the method
                        // definition), we can use this cache to identify that T really maps to V and that we need to
                        // encode the type of the method parameter as "!1".
                        substitution.Accept(this);
                    }
                    else
                    {
                        Debug.Fail($"Encountered unexpected type parameter '{typeParameter.ToDisplayString()}'.");
                    }
                }
            }

            public override void VisitArrayType(IArrayTypeSymbol arrayType)
            {
                if (!TryAppendTypeName(arrayType))
                {
                    BeginVisitingType();

                    arrayType.ElementType.Accept(this);

                    _currentTypeNameBuilder.Append('[');

                    for (var i = 1; i < arrayType.Rank; ++i)
                    {
                        _currentTypeNameBuilder.Append(',');
                    }

                    _currentTypeNameBuilder.Append(']');

                    EndVisitingType(arrayType);
                }
            }

            public override void VisitPointerType(IPointerTypeSymbol pointerType)
            {
                if (!TryAppendTypeName(pointerType))
                {
                    BeginVisitingType();

                    pointerType.PointedAtType.Accept(this);
                    _currentTypeNameBuilder.Append('*');

                    EndVisitingType(pointerType);
                }
            }

            public override void VisitDynamicType(IDynamicTypeSymbol dynamicType)
            {
                if (!TryAppendTypeName(dynamicType))
                {
                    BeginVisitingType();
                    _currentTypeNameBuilder.Append("System.Object");
                    EndVisitingType(dynamicType);
                }
            }

            private bool WasTypeSeen(ITypeSymbol type)
            {
                if (type.Kind == SymbolKind.ErrorType)
                {
                    throw new ArgumentException(nameof(type));
                }

                return _seenTypes.ContainsKey(type);
            }

            private bool TryAppendTypeName(ITypeSymbol type)
            {
                if (type.Kind == SymbolKind.ErrorType)
                {
                    throw new ArgumentException(nameof(type));
                }

                var wasTypeSeen = _seenTypes.TryGetValue(type, out var typeFullyQualifiedName);
                if (wasTypeSeen)
                {
                    if (_currentTypeNameBuilder == null)
                    {
                        _methodNameBuilder.Append(typeFullyQualifiedName);
                    }
                    else
                    {
                        _currentTypeNameBuilder.Append(typeFullyQualifiedName);
                    }
                }

                return wasTypeSeen;
            }

            private void BeginVisitingType()
            {
                if (_currentTypeNameBuilder != null)
                {
                    _typeNameBuilderStack.Push(_currentTypeNameBuilder);
                }

                _currentTypeNameBuilder = new StringBuilder();
            }

            private void EndVisitingType(ITypeSymbol type)
            {
                var typeFullyQualifiedName = _currentTypeNameBuilder.ToString();

                if (_typeNameBuilderStack.Count == 0)
                {
                    _currentTypeNameBuilder = null;

                    if (_visitingContainingType)
                    {
                        _containingTypeName = typeFullyQualifiedName;
                    }
                    else
                    {
                        _methodNameBuilder.Append(typeFullyQualifiedName);
                    }
                }
                else
                {
                    _currentTypeNameBuilder = _typeNameBuilderStack.Pop();
                    _currentTypeNameBuilder.Append(typeFullyQualifiedName);
                }

                // We don't want to remember the containing type of the supplied method because
                // we skip all type parameter names (and only include generic arity) when generating
                // the containing type's full name. Even if we encounter the containing type again when
                // visiting method parameters we can't use the containing type's full name since we need
                // to include the type argument names this time.
                if (!_visitingContainingType)
                {
                    _seenTypes[type] = typeFullyQualifiedName;
                }
            }

            public override void VisitMethod(IMethodSymbol method)
            {
                Debug.Assert(method == method.OriginalDefinition);

                _methodNameBuilder.Append(method.Name);

                if (method.Arity > 0)
                {
                    _methodNameBuilder.Append('`');
                    _methodNameBuilder.Append(method.Arity);

                    _visitingMethodTypeParameters = true;
                    VisitTypeParameters(method.TypeParameters);
                    _visitingMethodTypeParameters = false;
                }

                int parameterCount = method.Parameters.Length;
                if (parameterCount > 0)
                {
                    _methodNameBuilder.Append('(');

                    for (int i = 0; i < parameterCount; ++i)
                    {
                        var parameter = method.Parameters[i];
                        parameter.Type.Accept(this);
                        if (i != parameterCount - 1)
                        {
                            _methodNameBuilder.Append(',');
                        }
                    }

                    _methodNameBuilder.Append(')');
                }
            }
        }
    }
}
