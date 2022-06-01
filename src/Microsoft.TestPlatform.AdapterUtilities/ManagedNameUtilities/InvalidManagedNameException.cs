// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#if !NETSTANDARD1_0 && !WINDOWS_UWP
using System.Runtime.Serialization;
#endif

namespace Microsoft.TestPlatform.AdapterUtilities.ManagedNameUtilities;

#if !NETSTANDARD1_0 && !WINDOWS_UWP
[Serializable]
#endif
public class InvalidManagedNameException :
    Exception
#if !NETSTANDARD1_0 && !WINDOWS_UWP
    , ISerializable
#endif
{
    public InvalidManagedNameException(string message) : base(message) { }

#if !NETSTANDARD1_0 && !WINDOWS_UWP
    protected InvalidManagedNameException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
}
