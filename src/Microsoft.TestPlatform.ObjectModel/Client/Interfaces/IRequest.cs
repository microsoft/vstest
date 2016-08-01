// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Threading;

    public interface IRequest : IDisposable
    {
        /// <summary>
        ///  Handler for receiving raw messages directly from host without any deserialization or morphing
        ///  This is required if one wants to re-direct the message over the process boundary without any processing overhead
        ///  All events should come as raw messages as well as actual serialized events 
        /// </summary>
        event EventHandler<string> OnRawMessageReceived;

        /// <summary>
        /// Waits for the request to complete
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <returns>True if the request timeouts</returns>
        bool WaitForCompletion(int timeout);
    }

    public static class RequestExtensions
    {
        public static void WaitForCompletion(this IRequest request)
        {
            request.WaitForCompletion(Timeout.Infinite);
        }
    }
}
