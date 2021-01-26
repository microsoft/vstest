// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Extensions
{
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Resources;

    using System;

    public static partial class ReflectionExtensions
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
