// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// DesignMode TestHost Launcher for hosting of test process
    /// </summary>
    internal class DesignModeTestHostLauncher : ITestHostLauncher
    {
        private IDesignModeClient designModeClient;

        public DesignModeTestHostLauncher(IDesignModeClient designModeClient)
        {
            this.designModeClient = designModeClient;
        }

        public virtual bool IsDebug => false;

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
        public DesignModeDebugTestHostLauncher(IDesignModeClient designModeClient) : base(designModeClient)
        {
        }

        public override bool IsDebug => true;
    }
}
