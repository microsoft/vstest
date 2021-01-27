// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Factory for providing the design mode test host launchers
    /// </summary>
    public static class DesignModeTestHostLauncherFactory
    {
        private static ITestHostLauncher defaultLauncher;
        private static ITestHostLauncher debugLauncher;

        public static ITestHostLauncher GetCustomHostLauncherForTestRun(IDesignModeClient designModeClient, bool debuggingEnabled)
        {
            ITestHostLauncher testHostLauncher = null;

            if (!debuggingEnabled)
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
