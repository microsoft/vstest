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
    /// Responsible for managing TestRuntimeProviderManager extensions
    /// </summary>
    internal class TestRuntimeProviderManager
    {
        #region Fields

        private static TestRuntimeProviderManager testHostManager;

        /// <summary>
        /// Gets an instance of the logger.
        /// </summary>
        private IMessageLogger messageLogger;

        private TestRuntimeExtensionManager testHostExtensionManager;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected TestRuntimeProviderManager(TestSessionMessageLogger sessionLogger)
        {
            this.messageLogger = sessionLogger;
            this.testHostExtensionManager = TestRuntimeExtensionManager.Create(sessionLogger);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static TestRuntimeProviderManager Instance
        {
            get
            {
                if (testHostManager == null)
                {
                    testHostManager = new TestRuntimeProviderManager(TestSessionMessageLogger.Instance);
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

        public ITestRuntimeProvider GetTestHostManagerByUri(string hostUri)
        {
            var host = this.testHostExtensionManager.TryGetTestExtension(hostUri);
            if (host != null)
            {
                return host.Value;
            }

            return null;
        }

        public ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration)
        {
            foreach (var testExtension in this.testHostExtensionManager.TestExtensions)
            {
                if (testExtension.Value.CanExecuteCurrentRunConfiguration(runConfiguration))
                {
                    return testExtension.Value;
                }
            }

            return null;
        }

        #endregion
    }
}
