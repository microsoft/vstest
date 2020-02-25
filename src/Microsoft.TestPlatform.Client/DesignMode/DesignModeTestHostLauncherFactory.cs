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

        public static ITestHostLauncher GetCustomHostLauncherForTestRun(IDesignModeClient designModeClient, TestRunRequestPayload testRunRequestPayload)
        {
            return (defaultLauncher = defaultLauncher ?? new DesignModeTestHostLauncher(designModeClient));
        }
    }
}
