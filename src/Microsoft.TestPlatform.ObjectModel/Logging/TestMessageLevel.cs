// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
{
    using System;

    /// <summary>
    /// Levels for test messages.
    /// </summary>
    public enum TestMessageLevel
    {
        /// <summary>
        /// Informational message.
        /// </summary>
        Informational = 0,
        
        /// <summary>
        /// Warning message.
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// Error message.
        /// </summary>
        Error = 2
    }
}
