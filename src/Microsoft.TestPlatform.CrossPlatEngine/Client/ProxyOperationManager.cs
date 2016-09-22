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
    public class ProxyOperationManager : IDisposable
    {
        private ITestHostManager testHostManager;

        private bool isInitialized;

        private readonly int connectionTimeout;

        private bool disposed;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class. 
        /// </summary>
        public ProxyOperationManager() : this(new TestRequestSender(), null, Constants.ClientConnectionTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class. 
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
        /// <param name="testHostManager">
        /// Manager for the test host process
        /// </param>
        public void SetupChannel(ITestHostManager testHostManager)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.isInitialized)
            {
                this.testHostManager = testHostManager;

                var portNumber = this.RequestSender.InitializeCommunication();

                // TODO: Fix the environment variables usage
                var testHostStartInfo = this.testHostManager.GetTestHostProcessStartInfo(
                    null,
                    new TestRunnerConnectionInfo { Port = portNumber });
                this.testHostManager.LaunchTestHost(testHostStartInfo);

                // Get the test process start info
                // Launch the test process
                // Listen to terminate events, and close the channel accordingly
                //

                this.isInitialized = true;
            }

            // Wait for a timeout for the client to connect.
            if (!this.RequestSender.WaitForRequestHandlerConnection(this.connectionTimeout))
            {
                throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CrossPlatEngine.Resources.InitializationFailed));
            }
        }

        /// <summary>
        /// Dispose for this instance.
        /// </summary>
        public virtual void Dispose()
        {
            this.isInitialized = false;
            this.disposed = true;
        }

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        public virtual void Abort()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
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
