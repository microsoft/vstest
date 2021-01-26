// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ManagedNameUtilities
{
    using System;

    public class InvalidManagedNameException : Exception
    {
        public InvalidManagedNameException(string message) : base(message) { }
    }
}