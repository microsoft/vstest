// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Factory for providing the design mode test host launchers
    /// </summary>
    public static class DesignModeTestHostLauncherFactory
    {
        private static ITestHostLauncher defaultLauncher;

        private static ITestHostLauncher debugLauncher;

        public static ITestHostLauncher GetCustomHostLauncherForTestRun(IDesignModeClient designModeClient, TestRunRequestPayload testRunRequestPayload)
        {
            ITestHostLauncher testHostLauncher = null;
            if (!testRunRequestPayload.DebuggingEnabled)
            {
                testHostLauncher = defaultLauncher = defaultLauncher ?? new DesignModeTestHostLauncher(designModeClient);
            }
            else
            {
                testHostLauncher = debugLauncher = debugLauncher ?? new DesignModeDebugTestHostLauncher(designModeClient);
            }

            return testHostLauncher;
        }
    }
}
