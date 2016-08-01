// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

    [Flags]
    public enum TestPropertyAttributes
    {
        None = 0x00, // Default
        Hidden = 0x01,
        Immutable = 0x02,
        [Obsolete("Use TestObject.Traits collection to create traits")]
        Trait = 0x04,
    }
}
