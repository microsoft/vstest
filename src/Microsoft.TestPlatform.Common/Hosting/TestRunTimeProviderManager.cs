// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Hosting
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Responsible for managing TestRuntimeProviderManager extensions
    /// </summary>
    public class TestRuntimeProviderManager
    {
        #region Fields

        private static TestRuntimeProviderManager testHostManager;

        private readonly TestRuntimeExtensionManager testHostExtensionManager;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRuntimeProviderManager"/> class. 
        /// Default constructor.
        /// </summary>
        /// <param name="sessionLogger">
        /// The session Logger.
        /// </param>
        protected TestRuntimeProviderManager(IMessageLogger sessionLogger)
        {
            this.testHostExtensionManager = TestRuntimeExtensionManager.Create(sessionLogger);
        }

        /// <summary>
        /// Gets the instance of TestRuntimeProviderManager
        /// </summary>
        public static TestRuntimeProviderManager Instance => testHostManager
                                                             ?? (testHostManager = new TestRuntimeProviderManager(TestSessionMessageLogger.Instance));

        #endregion

        #region Public Methods

        public ITestRuntimeProvider GetTestHostManagerByUri(string hostUri)
        {
            var host = this.testHostExtensionManager.TryGetTestExtension(hostUri);
            return host?.Value;
        }

        public virtual ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration)
        {
            foreach (var testExtension in this.testHostExtensionManager.TestExtensions)
            {
                if (testExtension.Value.CanExecuteCurrentRunConfiguration(runConfiguration))
                {
                    // we are creating a new Instance of ITestRuntimeProvider so that each POM gets it's own object of ITestRuntimeProvider
                    return (ITestRuntimeProvider)Activator.CreateInstance(testExtension.Value.GetType());
                }
            }

            return null;
        }

        #endregion
    }
}
