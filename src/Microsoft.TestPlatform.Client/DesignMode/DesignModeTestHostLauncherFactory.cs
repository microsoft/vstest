// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode;

/// <summary>
/// Factory for providing the design mode test host launchers
/// </summary>
public static class DesignModeTestHostLauncherFactory
{
    private static ITestHostLauncher s_defaultLauncher;
    private static ITestHostLauncher s_debugLauncher;

    public static ITestHostLauncher GetCustomHostLauncherForTestRun(IDesignModeClient designModeClient, bool debuggingEnabled)
    {
        ITestHostLauncher testHostLauncher = !debuggingEnabled
            ? (s_defaultLauncher ??= new DesignModeTestHostLauncher(designModeClient))
            : (s_debugLauncher ??= new DesignModeDebugTestHostLauncher(designModeClient));
        return testHostLauncher;
    }
}
