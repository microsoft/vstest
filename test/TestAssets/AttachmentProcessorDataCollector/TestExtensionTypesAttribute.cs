// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AttachmentProcessorDataCollector;
using Microsoft.VisualStudio.TestPlatform;

[assembly: TestExtensionTypes(typeof(SampleDataCollectorV1))]
[assembly: TestExtensionTypesV2("AttachmentProcessorDataCollector.SampleDataCollector", 0)]
[assembly: TestExtensionTypesV2("AttachmentProcessorDataCollector.SampleDataCollectorV1", 1)]
[assembly: TestExtensionTypesV2("AttachmentProcessorDataCollector.SampleDataCollectorV2", 2, "unused")]

namespace Microsoft.VisualStudio.TestPlatform
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    internal sealed class TestExtensionTypesAttribute : Attribute
    {
        public TestExtensionTypesAttribute(params Type[] types)
        {
            this.Types = types;
        }

        public Type[] Types { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    internal sealed class TestExtensionTypesV2Attribute : Attribute
    {
        public int Version { get; set; }
        public string FullName { get; }

        public TestExtensionTypesV2Attribute(string fullName, int version)
        {
            this.FullName = fullName;
            this.Version =version;
        }

        public TestExtensionTypesV2Attribute(string fullName, int version, string unused)
        {
            this.FullName = fullName;
            this.Version =version;
        }
    }
}