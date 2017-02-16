// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Hosting
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Responsible for managing testHostProvider extensions
    /// </summary>
    internal class TestHostProviderManager : IDisposable
    {
        #region Fields

        private static readonly object Synclock = new object();
        private static TestHostProviderManager testHostManager;

        /// <summary>
        /// Keeps track if we are disposed.
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Gets an instance of the logger.
        /// </summary>
        private IMessageLogger messageLogger;

        private TestHostExtensionManager testHostExtensionManager;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected TestHostProviderManager(TestSessionMessageLogger sessionLogger)
        {
            this.messageLogger = sessionLogger;
            this.testHostExtensionManager = TestHostExtensionManager.Create(sessionLogger);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static TestHostProviderManager Instance
        {
            get
            {
                if (testHostManager == null)
                {
                    lock (Synclock)
                    {
                        if (testHostManager == null)
                        {
                            testHostManager = new TestHostProviderManager(TestSessionMessageLogger.Instance);
                        }
                    }
                }
                return testHostManager;
            }

            protected set
            {
                testHostManager = value;
            }
        }

        #endregion

        #region Public Methods

        public ITestHostProvider GetTestHostManagerByUri(string hostUri)
        {
            var host = this.testHostExtensionManager.TryGetTestExtension(hostUri);
            if (host != null)
                return host.Value;

            return null;
        }

        public ITestHostProvider GetTestHostManagerByRunConfiguration(RunConfiguration runConfiguarion)
        {
            foreach (var testExtension in this.testHostExtensionManager.TestExtensions)
            {
                if (testExtension.Value.CanExecuteCurrentRunConfiguration(runConfiguarion))
                    return testExtension.Value;
            }

            return null;
        }

        /// <summary>
        /// Ensure that all pending messages are sent to the loggers.
        /// </summary>
        public void Dispose()
        {
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(typeof(TestHostProviderManager).FullName);
            }
        }

        #endregion
    }
}
