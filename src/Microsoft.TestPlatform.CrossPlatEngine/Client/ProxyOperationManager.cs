// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    using Constants = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Constants;

    /// <summary>
    /// Base class for any operations that the client needs to drive through the engine.
    /// </summary>
    public abstract class ProxyOperationManager
    {
        private readonly ITestHostManager testHostManager;

        private bool initialized;

        private readonly int connectionTimeout;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class. 
        /// </summary>
        /// <param name="requestSender">Request Sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="clientConnectionTimeout">Client Connection Timeout.</param>
        protected ProxyOperationManager(ITestRequestSender requestSender, ITestHostManager testHostManager, int clientConnectionTimeout)
        {
            this.RequestSender = requestSender;
            this.connectionTimeout = clientConnectionTimeout;
            this.testHostManager = testHostManager;
            this.initialized = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the server for communication.
        /// </summary>
        protected ITestRequestSender RequestSender { get; set; }

        #endregion

        #region IProxyOperationManager implementation.

        /// <summary>
        /// Ensure that the engine is ready for test operations.
        /// Usually includes starting up the test host process.
        /// </summary>
        /// <param name="sources">List of test sources.</param>
        public virtual void SetupChannel(IEnumerable<string> sources)
        {
            if (!this.initialized)
            {
                var portNumber = this.RequestSender.InitializeCommunication();

                // Get the test process start info
                // TODO: Fix the environment variables usage
                var testHostStartInfo = this.testHostManager.GetTestHostProcessStartInfo(
                    sources,
                    null,
                    new TestRunnerConnectionInfo { Port = portNumber });

                this.testHostManager.LaunchTestHost(testHostStartInfo);
                this.testHostManager.RegisterForExitNotification(() => this.RequestSender.OnClientProcessExit());

                this.initialized = true;
            }

            // Wait for a timeout for the client to connect.
            if (!this.RequestSender.WaitForRequestHandlerConnection(this.connectionTimeout))
            {
                throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CrossPlatEngine.Resources.InitializationFailed));
            }
        }

        /// <summary>
        /// Closes the channel, terminate test host process.
        /// </summary>
        public virtual void Close()
        {
            // TODO dispose the testhost process
            try
            {
                this.RequestSender.EndSession();
            }
            finally
            {
                this.initialized = false;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Setup the command line options to include port.
        /// </summary>
        /// <param name="portNumber"> The port number. </param>
        /// <returns> The commandLine arguments as a list. </returns>
        private IList<string> GetCommandLineArguments(int portNumber)
        {
            var commandlineArguments = new List<string> { Constants.PortOption, portNumber.ToString() };
            return commandlineArguments;
        }
        
        #endregion
    }
}
