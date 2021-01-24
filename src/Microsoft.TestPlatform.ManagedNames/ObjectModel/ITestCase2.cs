// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    public interface ITestCase2
    {
        /// <summary>
        /// Gets or sets the fully specified type name metadata format.
        /// </summary>
        /// <example>
        ///     <code>NamespaceA.NamespaceB.ClassName`1+InnerClass`2</code>
        /// </example>
        string ManagedType { get; set; }

        /// <summary>
        /// Gets or sets the fully specified method name metadata format.
        /// </summary>
        /// <example>
        ///     <code>MethodName`2(ParamTypeA,ParamTypeB)</code>
        /// </example>
        string ManagedMethod { get; set; }

        /// <summary>
        /// Get's the default display name of <see cref="ITestCase2"/>.
        /// </summary>
        /// <returns></returns>
        string GetDisplayName();
    }
}
