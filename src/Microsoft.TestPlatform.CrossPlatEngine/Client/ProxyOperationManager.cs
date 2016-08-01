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
    public class ProxyOperationManager : IProxyOperationManager
    {
        private ITestHostManager testHostManager;

        private bool isInitialized;

        private int connectionTimeout;
        
        #region Constructors.

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyDiscoveryManager"/> class.
        /// </summary>
        public ProxyOperationManager()
            : this(new TestRequestSender(), null, Constants.ClientConnectionTimeout)
        {
        }

        /// <summary>
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="requestSender">Request Sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="clientConnectionTimeout">Client Connection Timeout.</param>
        internal ProxyOperationManager(ITestRequestSender requestSender, ITestHostManager testHostManager, int clientConnectionTimeout)
        {
            this.RequestSender = requestSender;
            this.connectionTimeout = clientConnectionTimeout;
            this.testHostManager = testHostManager;
            this.isInitialized = false;
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
        /// <param name="testHostManager">Manager for launching and maintaining the test host process</param>
        public virtual void Initialize(ITestHostManager testHostManager)
        {
            this.testHostManager = testHostManager;

            var portNumber = this.RequestSender.InitializeCommunication();

            // TODO: Fix the environment variables usage
            this.testHostManager.LaunchTestHost(null, this.GetCommandLineArguments(portNumber));

            this.isInitialized = true;
        }

        /// <summary>
        /// Dispose for this instance.
        /// </summary>
        public virtual void Dispose()
        {
            // Do Nothing.
        }

        /// <summary>
        /// Aborts the test discovery.
        /// </summary>
        public virtual void Abort()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// The ensure initialized.
        /// </summary>
        protected void EnsureInitialized()
        {
            if (!this.isInitialized)
            {
                this.Initialize(this.testHostManager);
            }

            // Wait for a timeout for the client to connect.
            var isHandlerConnected = this.RequestSender.WaitForRequestHandlerConnection(this.connectionTimeout);

            if (!isHandlerConnected)
            {
                throw new TestPlatformException(
                    string.Format(CultureInfo.CurrentUICulture, CrossPlatEngine.Resources.InitializationFailed));
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
            var commandlineArguments = new List<string>();

            commandlineArguments.Add(Constants.PortOption);
            commandlineArguments.Add(portNumber.ToString());

            return commandlineArguments;
        }
        
        #endregion
    }
}
