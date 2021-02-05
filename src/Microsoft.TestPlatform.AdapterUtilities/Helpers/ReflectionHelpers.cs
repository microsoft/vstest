// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities.Helpers
{
    using Microsoft.TestPlatform.AdapterUtilities.Resources;

    using System;

    internal static partial class ReflectionHelpers
    {
        private static void AssertSupport<T>(T obj, string methodName, string className)
            where T : class
        {
            if (obj == null)
            {
                throw new NotImplementedException(string.Format(Resources.MethodNotImplementedOnPlatform, className, methodName));
            }
        }
    }
}
