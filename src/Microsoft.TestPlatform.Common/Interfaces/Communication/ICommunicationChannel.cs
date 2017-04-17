// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces
{
    using System;
    using System.Threading.Tasks;

    public interface ICommunicationChannel : IDisposable
    {
        /// <summary>
        /// Event raised when data is received on the communication channel.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Frames and sends the provided data over communication channel.
        /// </summary>
        /// <param name="data">Data to sent over the channel.</param>
        /// <returns>A <see cref="Task"/> implying async nature of the function.</returns>
        Task Send(string data);

        /// <summary>
        /// Notification from server/client that data is available.
        /// </summary>
        /// <returns>A <see cref="Task"/> implying async nature of the function.</returns>
        Task NotifyDataAvailable();
    }
}
