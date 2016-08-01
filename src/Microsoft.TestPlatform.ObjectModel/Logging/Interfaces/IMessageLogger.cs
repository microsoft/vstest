// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
{
    using System;

    /// <summary>
    /// Used for logging error warning and informational messages.
    /// </summary>
    public interface IMessageLogger
    {
        /// <summary>
        /// Sends a message to the enabled loggers.
        /// </summary>
        /// <param name="testMessageLevel">Level of the message.</param>
        /// <param name="message">The message to be sent.</param>
        void SendMessage(TestMessageLevel testMessageLevel, string message);

    }
}
