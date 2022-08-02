// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#if !WINDOWS_UWP
using System.Runtime.Serialization;
#endif

namespace Microsoft.TestPlatform.AdapterUtilities.ManagedNameUtilities;

#if !WINDOWS_UWP
[Serializable]
#endif
public class InvalidManagedNameException :
    Exception
#if !WINDOWS_UWP
    , ISerializable
#endif
{
    public InvalidManagedNameException(string? message) : base(message) { }

#if !WINDOWS_UWP
    protected InvalidManagedNameException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
}
