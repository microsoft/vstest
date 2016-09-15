// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;

    /// <summary>
    /// The interface for design mode client.
    /// </summary>
    public interface IDesignModeClient : IDisposable
    {
        /// <summary>
        /// Setups client based on port
        /// </summary>
        /// <param name="port">port number to connect</param>
        void ConnectToClientAndProcessRequests(int port, ITestRequestManager testRequestManager);

        /// <summary>
        /// Send the raw messages to IDE
        /// </summary>
        /// <param name="rawMessage"></param>
        void SendRawMessage(string rawMessage);

        /// <summary>
        /// Send a custom host launch message to IDE
        /// </summary>
        /// <param name="defaultTestHostStartInfo">Default TestHost Start Info</param>
        int LaunchCustomHost(TestProcessStartInfo defaultTestHostStartInfo);

        /// <summary>
        /// Handles parent process exit
        /// </summary>
        void HandleParentProcessExit();
    }
}
