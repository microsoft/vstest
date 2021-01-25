// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    public static class TestCase2Extensions
    {
        /// <summary>
        /// Returns <c>true</c> if both <see cref="ITestCase2.ManagedType" /> and <see cref="ITestCase2.ManagedMethod" /> are not null or whitespace.
        /// </summary>
        public static bool ContainsManagedMethodAndType(this ITestCase2 testCase)
        {
            return !string.IsNullOrWhiteSpace(testCase.ManagedMethod)
                && !string.IsNullOrWhiteSpace(testCase.ManagedType);
        }

        /// <summary>
        /// Returns the value of <see cref="ITestCase2.ManagedType" />.
        /// </summary>
        public static string ManagedType(this ITestCase2 testCase) => testCase.ManagedType;

        /// <summary>
        /// Returns the value of <see cref="ITestCase2.ManagedMethod" />.
        /// </summary>
        public static string ManagedMethod(this ITestCase2 testCase) => testCase.ManagedMethod;

        /// <summary>
        /// Returns the value of <see cref="ITestCase2.GetDisplayName" />.
        /// </summary>
        public static string GetDisplayName(this ITestCase2 testCase) => testCase.GetDisplayName();
    }
}
