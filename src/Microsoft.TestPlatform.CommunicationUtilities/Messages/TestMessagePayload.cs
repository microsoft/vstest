// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The test message payload.
    /// </summary>
    public class TestMessagePayload
    {
        /// <summary>
        /// Gets or sets the message level.
        /// </summary>
        public TestMessageLevel MessageLevel { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }
    }
}
