// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// DesignMode TestHost Launcher for hosting of test process
    /// </summary>
    internal class DesignModeTestHostLauncher : ITestHostLauncher
    {
        private readonly IDesignModeClient designModeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignModeTestHostLauncher"/> class.
        /// </summary>
        /// <param name="designModeClient">Design mode client instance.</param>
        public DesignModeTestHostLauncher(IDesignModeClient designModeClient)
        {
            this.designModeClient = designModeClient;
        }

        /// <inheritdoc/>
        public virtual bool IsDebug => false;

        /// <inheritdoc/>
        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
        {
            return this.designModeClient.LaunchCustomHost(defaultTestHostStartInfo);
        }
    }

    /// <summary>
    /// DesignMode Debug Launcher to use if debugging enabled
    /// </summary>
    internal class DesignModeDebugTestHostLauncher : DesignModeTestHostLauncher
    {
        /// <inheritdoc/>
        public DesignModeDebugTestHostLauncher(IDesignModeClient designModeClient) : base(designModeClient)
        {
        }

        /// <inheritdoc/>
        public override bool IsDebug => true;
    }
}
