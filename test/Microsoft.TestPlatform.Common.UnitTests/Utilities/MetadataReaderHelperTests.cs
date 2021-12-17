// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using TestPlatform.Common.UnitTests.Utilities;

[assembly: TestExtensionTypesV2("ExcentionType", "ExtensionIdentified", typeof(ExtensionV1), 1)]
[assembly: TestExtensionTypesV2("ExcentionType", "ExtensionIdentified", typeof(ExtensionV2), 2)]

namespace TestPlatform.Common.UnitTests.Utilities
{
    [TestClass]
    public class MetadataReaderHelperTests
    {
        private readonly MetadataReaderExtensionsHelper metadataReaderHelper = new MetadataReaderExtensionsHelper();

        [TestMethod]
        public void MetadataReaderHelper_GetCollectorExtensionTypes()
        {
            var types = metadataReaderHelper.DiscoverTestExtensionTypesV2Attribute(Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly().Location);
            Assert.AreEqual(typeof(ExtensionV2).AssemblyQualifiedName, types[0].AssemblyQualifiedName);
            Assert.AreEqual(typeof(ExtensionV1).AssemblyQualifiedName, types[1].AssemblyQualifiedName);
        }
    }

    public class ExtensionV2 : ExtensionV1
    {

    }

    public class ExtensionV1
    {

    }
}


namespace Microsoft.VisualStudio.TestPlatform
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    internal sealed class TestExtensionTypesV2Attribute : Attribute
    {
        public string ExtensionType { get; }
        public string ExtensionIdentifier { get; }
        public Type ExtensionImplementation { get; }
        public int Version { get; }

        public TestExtensionTypesV2Attribute(string extensionType, string extensionIdentifier, Type extensionImplementation, int version, string futureUse = null)
        {
            ExtensionType = extensionType;
            ExtensionIdentifier = extensionIdentifier;
            ExtensionImplementation = extensionImplementation;
            Version = version;
        }
    }
}
