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
#pragma warning disable RS0016 // Add public types and members to the declared API
        public static ITestHostLauncher GetCustomHostLauncherForTestRun(IDesignModeClient designModeClient, bool debuggingEnabled, string recipient)
#pragma warning restore RS0016 // Add public types and members to the declared API
        {
            if (!debuggingEnabled)
            {
                return new DesignModeTestHostLauncher(designModeClient, recipient);
            }
            else
            {
                return new DesignModeDebugTestHostLauncher(designModeClient, recipient);
            }
        }
    }
}
