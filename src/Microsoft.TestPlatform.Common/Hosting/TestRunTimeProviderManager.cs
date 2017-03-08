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
    /// Responsible for managing TestRunTimeProviderManager extensions
    /// </summary>
    internal class TestRunTimeProviderManager
    {
        #region Fields

        private static TestRunTimeProviderManager testHostManager;

        /// <summary>
        /// Gets an instance of the logger.
        /// </summary>
        private IMessageLogger messageLogger;

        private TestRunTimeExtensionManager testHostExtensionManager;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected TestRunTimeProviderManager(TestSessionMessageLogger sessionLogger)
        {
            this.messageLogger = sessionLogger;
            this.testHostExtensionManager = TestRunTimeExtensionManager.Create(sessionLogger);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static TestRunTimeProviderManager Instance
        {
            get
            {
                if (testHostManager == null)
                {
                    testHostManager = new TestRunTimeProviderManager(TestSessionMessageLogger.Instance);
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

        public ITestRunTimeProvider GetTestHostManagerByUri(string hostUri)
        {
            var host = this.testHostExtensionManager.TryGetTestExtension(hostUri);
            if (host != null)
                return host.Value;

            return null;
        }

        public ITestRunTimeProvider GetTestHostManagerByRunConfiguration(RunConfiguration runConfiguarion)
        {
            foreach (var testExtension in this.testHostExtensionManager.TestExtensions)
            {
                if (testExtension.Value.CanExecuteCurrentRunConfiguration(runConfiguarion))
                    return testExtension.Value;
            }

            return null;
        }

        #endregion
    }
}
