// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Reflection.PortableExecutable;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal class MetadataReaderExtensionsHelper
    {
        private static string TestPlatformExtensionVersionAttribute = "Microsoft.VisualStudio.TestPlatform.TestExtensionTypeAttribute";
        private static string TestExtensionTypesAttributeV2 = "Microsoft.VisualStudio.TestPlatform.TestExtensionTypesV2Attribute";
        private static string[] MethodsDefinition = new string[] { ".ctor", "get_Version" };
        private static Type[] EmptyTypeArray = new Type[0];

        public MetadataReaderExtensionsHelper()
        {

        }

        public Type[] DiscoverTestPlatformExtensionVersionAttributeExtensions2(Assembly assembly, string assemblyFilePath)
        {
            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Discovering extensions inside assembly '{assembly.FullName}' file path '{assemblyFilePath}'");

            List<Tuple<int, Type>> extensions = null;
            using (var stream = new FileStream(assemblyFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new PEReader(stream, PEStreamOptions.Default))
            {
                MetadataReader metadataReader = reader.GetMetadataReader();

                foreach (var customAttributeHandle in metadataReader.CustomAttributes)
                {
                    string attributeFullName = null;
                    try
                    {
                        if (customAttributeHandle.IsNil)
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid custom attribute (customAttributeHandle.IsNil)");
                            continue;
                        }

                        if (!GetAttributeTypeAndConstructor(metadataReader, customAttributeHandle, out EntityHandle attributeType))
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid custom attribute (GetAttributeTypeAndConstructor)");
                            continue;
                        }

                        if (!GetAttributeTypeNamespaceAndName(metadataReader, attributeType, out StringHandle namespaceHandle, out StringHandle nameHandle))
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid custom attribute (GetAttributeTypeNamespaceAndName)");
                            continue;
                        }

                        attributeFullName = $"{metadataReader.GetString(namespaceHandle)}.{metadataReader.GetString(nameHandle)}";
                        if (attributeFullName != TestExtensionTypesAttributeV2)
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid attribute '{attributeFullName}'");
                            continue;
                        }

                        var customAttribute = metadataReader.GetCustomAttribute(customAttributeHandle);
                        EntityHandle ctorHandle = customAttribute.Constructor;
                        BlobHandle signature;
                        switch (ctorHandle.Kind)
                        {
                            case HandleKind.MemberReference:
                                signature = metadataReader.GetMemberReference((MemberReferenceHandle)ctorHandle).Signature;
                                break;
                            case HandleKind.MethodDefinition:
                                signature = metadataReader.GetMethodDefinition((MethodDefinitionHandle)ctorHandle).Signature;
                                break;
                            default:
                                Console.WriteLine($"MetadataReaderExtensionsHelper: Potentially invalid IL for the attribute '{attributeFullName}'");
                                continue;
                        }

                        BlobReader signatureReader = metadataReader.GetBlobReader(signature);
                        BlobReader valueReader = metadataReader.GetBlobReader(customAttribute.Value);
                        const ushort Prolog = 1; // two-byte "prolog" defined by ECMA-335 (II.23.3) to be at the beginning of attribute value blobs
                        UInt16 prolog = valueReader.ReadUInt16();
                        if (prolog != Prolog)
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid blob attribute prolog '{prolog}'");
                            continue;
                        }

                        string fullName = valueReader.ReadSerializedString();
                        int version = valueReader.ReadInt32();

                        try
                        {
                            var extensionType = assembly.GetType(fullName);
                            if (extensionType is null)
                            {
                                EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Unable to get extension for type '{fullName}'");
                                continue;
                            }

                            if (extensions is null) extensions = new List<Tuple<int, Type>>();
                            extensions.Add(Tuple.Create(version, extensionType));
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Valid extension found '{extensionType}' version '{version}'");
                        }
                        catch (Exception ex)
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Failure during type creation, extension full name: '{fullName}'\n{FormatException(ex)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Failure during custom attribute analysis, attribute full name: {attributeFullName}\n{FormatException(ex)}");
                    }
                }
            }

            var finalExtensions = extensions?.OrderByDescending(t => t.Item1).Select(t => t.Item2).ToArray() ?? EmptyTypeArray;
            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Found {extensions?.Count ?? 0} extensions");
            return finalExtensions;
        }

        public Type[] DiscoverTestPlatformExtensionVersionAttributeExtensions(Assembly assembly, string assemblyFilePath)
        {
            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Discovering extensions inside assembly '{assembly.FullName}' file path '{assemblyFilePath}'");

            using (var stream = new FileStream(assemblyFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new PEReader(stream, PEStreamOptions.Default))
            {
                MetadataReader metadataReader = reader.GetMetadataReader(MetadataReaderOptions.Default);

                EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Search '{TestPlatformExtensionVersionAttribute}' definition inside current assembly '{assembly.FullName}'");
                Type testPlatformExtensionVersionAttributeType = SearchExtensionAttribute(assembly, metadataReader);

                if (testPlatformExtensionVersionAttributeType is null)
                {
                    EqtTrace.Verbose($"MetadataReaderExtensionsHelper: '{TestPlatformExtensionVersionAttribute}' attribute not found inside assembly '{assembly.FullName}' file path '{assemblyFilePath}'");
                    return EmptyTypeArray;
                }

                EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Inspect types for extensions");
                return InspectTypes(assembly, metadataReader, testPlatformExtensionVersionAttributeType);
            }
        }

        private Type[] InspectTypes(Assembly assembly, MetadataReader metadataReader, Type testPlatformExtensionVersionAttributeType)
        {
            List<Tuple<int, Type>> extensions = null;

            foreach (var handle in metadataReader.TypeDefinitions)
            {
                if (handle.IsNil)
                {
                    continue;
                }

                var typeDef = metadataReader.GetTypeDefinition(handle);
                var typeName = metadataReader.GetString(typeDef.Name);
                var typeNameSpace = metadataReader.GetString(typeDef.Namespace);
                string fullName = $"{typeNameSpace}.{typeName}";

                if (fullName == testPlatformExtensionVersionAttributeType.FullName)
                {
                    continue;
                }

                EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Analyze TypeDefinitionHandle '{fullName}'");

                var customAttributes = metadataReader.GetCustomAttributes(handle);
                if (customAttributes.Count == 0)
                {
                    continue;
                }

                EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Analyze attributes for type '{fullName}'");
                foreach (var attributeHandle in customAttributes)
                {
                    if (!attributeHandle.IsNil)
                    {
                        if (!GetAttributeTypeAndConstructor(metadataReader, attributeHandle, out EntityHandle attributeType))
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid custom attribute found for '{fullName}' (GetAttributeTypeAndConstructor)");
                            continue;
                        }

                        if (!GetAttributeTypeNamespaceAndName(metadataReader, attributeType, out StringHandle namespaceHandle, out StringHandle nameHandle))
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid custom attribute found for '{fullName}' (GetAttributeTypeNamespaceAndName)");
                            continue;
                        }

                        string attributeFullName = $"{metadataReader.GetString(namespaceHandle)}.{metadataReader.GetString(nameHandle)}";
                        if (attributeFullName != testPlatformExtensionVersionAttributeType.FullName)
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Invalid custom attribute found for '{fullName}' wrong type: {attributeFullName}");
                            continue;
                        }

                        try
                        {
                            var extensionType = assembly.GetType(fullName);
                            var version = GetVersion(metadataReader, metadataReader.GetCustomAttribute(attributeHandle));
                            if (version == int.MinValue)
                            {
                                EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Unable to read the version for '{fullName}'");
                            }

                            if (extensions is null) extensions = new List<Tuple<int, Type>>();
                            extensions.Add(Tuple.Create(version, extensionType));
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Valid extension found '{extensionType}' version '{version}'");
                        }
                        catch (Exception ex)
                        {
                            EqtTrace.Verbose($"Failed to create extension for type '{fullName}'\n{FormatException(ex)}");
                        }
                    }
                }
            }

            return extensions?.OrderByDescending(t => t.Item1).Select(t => t.Item2).ToArray() ?? EmptyTypeArray;
        }

        private string FormatException(Exception ex)
        {
            StringBuilder log = new StringBuilder();
            Exception current = ex;
            while (current != null)
            {
                log.AppendLine(current.ToString());
                current = current.InnerException;
            }

            return log.ToString();
        }

        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L288
        private int GetVersion(MetadataReader metadataReader, CustomAttribute attributeHandle)
        {
            EntityHandle ctorHandle = attributeHandle.Constructor;
            BlobHandle signature;
            switch (ctorHandle.Kind)
            {
                case HandleKind.MemberReference:
                    signature = metadataReader.GetMemberReference((MemberReferenceHandle)ctorHandle).Signature;
                    break;
                case HandleKind.MethodDefinition:
                    signature = metadataReader.GetMethodDefinition((MethodDefinitionHandle)ctorHandle).Signature;
                    break;
                default:
                    // Unusual case, potentially invalid IL
                    return int.MinValue;
            }

            BlobReader signatureReader = metadataReader.GetBlobReader(signature);
            BlobReader valueReader = metadataReader.GetBlobReader(attributeHandle.Value);
            const ushort Prolog = 1; // two-byte "prolog" defined by ECMA-335 (II.23.3) to be at the beginning of attribute value blobs
            if (valueReader.ReadUInt16() == Prolog)
            {
                SignatureHeader header = signatureReader.ReadSignatureHeader();
                int parameterCount;
                if (header.Kind == SignatureKind.Method &&                               // attr ctor must be a method
                    !header.IsGeneric &&                                                 // attr ctor must be non-generic
                    signatureReader.TryReadCompressedInteger(out parameterCount) &&      // read parameter count
                    parameterCount == 1 &&                                               // attr ctor must have 1 parameter
                    signatureReader.ReadSignatureTypeCode() == SignatureTypeCode.Void && // attr ctor return type must be void
                    signatureReader.ReadSignatureTypeCode() == SignatureTypeCode.Int32)  // attr ctor first parameter must be int32
                {
                    return valueReader.ReadInt32();
                }
            }

            return int.MinValue;
        }

        private Type SearchExtensionAttribute(Assembly assembly, MetadataReader metadataReader)
        {
            foreach (TypeDefinitionHandle typeDefHandle in metadataReader.TypeDefinitions)
            {
                try
                {
                    if (typeDefHandle.IsNil)
                    {
                        continue;
                    }

                    var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                    var typeName = metadataReader.GetString(typeDef.Name);
                    var @namespace = metadataReader.GetString(typeDef.Namespace);
                    var fullName = $"{@namespace}.{typeName}";

                    EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Analyze TypeDefinitionHandle '{fullName}'");

                    // Check the name
                    if (fullName == TestPlatformExtensionVersionAttribute && (typeDef.Attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed)
                    {
                        EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Valid type name found '{fullName}'");

                        // Check it inherits from System.Attribute
                        if (typeDef.BaseType.Kind != HandleKind.TypeReference)
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Type '{fullName}' doesn't inherit from System.Attribute (typeDef.BaseType.Kind != HandleKind.TypeReference)");
                            continue;
                        }

                        var baseTypeReferenceHandle = metadataReader.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
                        if ($"{metadataReader.GetString(baseTypeReferenceHandle.Namespace)}.{metadataReader.GetString(baseTypeReferenceHandle.Name)}" != "System.Attribute")
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Type '{fullName}' doesn't inherit from System.Attribute (baseTypeFullName != 'System.Attribute')");
                            continue;
                        }

                        EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Verify members definition for type '{fullName}'");

                        int isGoodCandidate = 0;
                        foreach (var method in typeDef.GetMethods())
                        {
                            var methodDef = metadataReader.GetMethodDefinition(method);
                            var methodName = metadataReader.GetString(methodDef.Name);
                            if (MethodsDefinition.Contains(methodName))
                            {
                                // Verify the ctor signature, int32 for the version number.
                                if (methodName == ".ctor" &&
                                    (methodDef.Attributes & MethodAttributes.Public) == MethodAttributes.Public &&
                                    (methodDef.Attributes & MethodAttributes.SpecialName) == MethodAttributes.SpecialName
                                    )
                                {
                                    var sigReader = metadataReader.GetBlobReader(methodDef.Signature);
                                    var decoder = new SignatureDecoder<string, object>(new TestPlatformExtensionVersionAttributeSignatureDecoder(), metadataReader, genericContext: null);
                                    var methodSignature = decoder.DecodeMethodSignature(ref sigReader);
                                    if (methodSignature.Header.IsInstance &&
                                        methodSignature.ReturnType == "void" &&
                                        methodSignature.ParameterTypes != null &&
                                        methodSignature.ParameterTypes.Length == 1 &&
                                        methodSignature.ParameterTypes[0] == "int32" &&
                                        methodSignature.GenericParameterCount == 0 &&
                                        !methodSignature.Header.IsGeneric)
                                    {
                                        isGoodCandidate++;
                                        EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Found '.ctor' for '{fullName}'");
                                    }
                                }

                                if (methodName == "get_Version" &&
                                    (methodDef.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                                {
                                    var sigReader = metadataReader.GetBlobReader(methodDef.Signature);
                                    var decoder = new SignatureDecoder<string, object>(new TestPlatformExtensionVersionAttributeSignatureDecoder(), metadataReader, genericContext: null);
                                    var methodSignature = decoder.DecodeMethodSignature(ref sigReader);
                                    if (methodSignature.Header.IsInstance &&
                                        methodSignature.ReturnType == "int32" &&
                                        methodSignature.ParameterTypes != null &&
                                        methodSignature.ParameterTypes.Length == 0 &&
                                        methodSignature.GenericParameterCount == 0 &&
                                        !methodSignature.Header.IsGeneric)
                                    {
                                        isGoodCandidate++;
                                        EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Found 'get_Version' for '{fullName}'");
                                    }
                                }
                            }
                        }

                        // If all characteristics were meet we'll use this attribute to find extensions type.
                        if (isGoodCandidate != 2)
                        {
                            EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Members definition verification for type '{fullName}' failed");
                            continue;
                        }

                        EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Members definition verification for type '{fullName}' succeded");
                        return assembly.GetType(fullName);
                    }
                }
                catch (Exception ex)
                {
                    EqtTrace.Verbose($"MetadataReaderExtensionsHelper: Exception during TypeDefinitions analysis\n{ex}");
                }
            }

            return null;
        }

        // https://github.com/dotnet/runtime/blob/6cf529168a8dcdfb158738d46be40b1867fd1bfa/src/coreclr/tools/Common/TypeSystem/Ecma/MetadataExtensions.cs#L173
        private bool GetAttributeTypeNamespaceAndName(MetadataReader metadataReader, EntityHandle attributeType,
         out StringHandle namespaceHandle, out StringHandle nameHandle)
        {
            namespaceHandle = default;
            nameHandle = default;

            if (attributeType.Kind == HandleKind.TypeReference)
            {
                TypeReference typeRefRow = metadataReader.GetTypeReference((TypeReferenceHandle)attributeType);
                HandleKind handleType = typeRefRow.ResolutionScope.Kind;

                // Nested type?
                if (handleType == HandleKind.TypeReference || handleType == HandleKind.TypeDefinition)
                    return false;

                nameHandle = typeRefRow.Name;
                namespaceHandle = typeRefRow.Namespace;
                return true;
            }
            else if (attributeType.Kind == HandleKind.TypeDefinition)
            {
                var def = metadataReader.GetTypeDefinition((TypeDefinitionHandle)attributeType);

                // Nested type?
                if (IsNested(def.Attributes))
                    return false;

                nameHandle = def.Name;
                namespaceHandle = def.Namespace;
                return true;
            }
            else
            {
                // unsupported metadata
                return false;
            }
        }

        // https://github.com/dotnet/runtime/blob/6cf529168a8dcdfb158738d46be40b1867fd1bfa/src/coreclr/tools/Common/TypeSystem/Ecma/MetadataExtensions.cs#L150
        private bool GetAttributeTypeAndConstructor(MetadataReader metadataReader, CustomAttributeHandle attributeHandle, out EntityHandle attributeType)
        {
            var attributeCtor = metadataReader.GetCustomAttribute(attributeHandle).Constructor;

            if (attributeCtor.Kind == HandleKind.MemberReference)
            {
                attributeType = metadataReader.GetMemberReference((MemberReferenceHandle)attributeCtor).Parent;
                return true;
            }
            else if (attributeCtor.Kind == HandleKind.MethodDefinition)
            {
                attributeType = metadataReader.GetMethodDefinition((MethodDefinitionHandle)attributeCtor).GetDeclaringType();
                return true;
            }
            else
            {
                // invalid metadata
                attributeType = default;
                return false;
            }
        }

        private bool IsNested(TypeAttributes flags)
        {
            return (flags & (TypeAttributes)0x00000006) != 0;
        }

        class TestPlatformExtensionVersionAttributeSignatureDecoder : ISignatureTypeProvider<string, object>
        {
            public string GetArrayType(string elementType, ArrayShape shape)
                => string.Empty;
            public string GetByReferenceType(string elementType)
                => string.Empty;
            public string GetFunctionPointerType(MethodSignature<string> signature)
                => string.Empty;
            public string GetGenericInstantiation(string genericType, System.Collections.Immutable.ImmutableArray<string> typeArguments)
                => string.Empty;
            public string GetGenericMethodParameter(object genericContext, int index)
                => string.Empty;
            public string GetGenericTypeParameter(object genericContext, int index)
                => string.Empty;
            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
                => string.Empty;
            public string GetPinnedType(string elementType)
                => string.Empty;
            public string GetPointerType(string elementType)
                => string.Empty;
            public string GetSZArrayType(string elementType)
                => string.Empty;
            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
                => string.Empty;
            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
                => string.Empty;
            public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
                => string.Empty;
            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Int32: return "int32";
                    case PrimitiveTypeCode.Void: return "void";
                    default: return "<bad metadata>";
                }
            }
        }
    }
}
