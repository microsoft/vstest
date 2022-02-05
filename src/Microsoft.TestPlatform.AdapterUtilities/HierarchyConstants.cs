// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities;

/// <summary>
/// Constants to help declare Hierarchy test property.
/// </summary>
public static class HierarchyConstants
{
    /// <summary>
    /// Label to use on Hierarchy test property.
    /// </summary>
    public const string HierarchyLabel = "Hierarchy";

    /// <summary>
    /// Property id to use on Hierarchy test property.
    /// </summary>
    public const string HierarchyPropertyId = "TestCase." + HierarchyLabel;

    /// <summary>
    /// Meanings of the indices in the Hierarchy array.
    /// </summary>
    public static class Levels
    {
        /// <summary>
        /// Total length of Hierarchy array.
        /// </summary>
        public const int TotalLevelCount = 2;

        /// <summary>
        /// Index of the namespace element of the array.
        /// </summary>
        public const int NamespaceIndex = 0;

        /// <summary>
        /// Index of the class element of the array.
        /// </summary>
        public const int ClassIndex = 1;
    }
}
