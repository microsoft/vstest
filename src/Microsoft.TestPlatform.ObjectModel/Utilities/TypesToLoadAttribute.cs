// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
    using System;

    /// <summary>
    /// Custom Attribute to specify the exact types which should be loaded from assembly
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    [CLSCompliant(false)]
    public sealed class TypesToLoadAttribute : Attribute
    {
        public TypesToLoadAttribute(params Type[] types)
        {
            Types = types;
        }

        public Type[] Types { get; }
    }
}
