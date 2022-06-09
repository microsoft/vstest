// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <summary>
/// Helper class to deal with process related functionality.
/// </summary>
public partial class ProcessHelper : IProcessHelper
{
    /// <inheritdoc/>
    public object LaunchProcess(string processPath, string arguments, string workingDirectory, IDictionary<string, string>? envVariables, Action<object?, string?>? errorCallback, Action<object?>? exitCallBack, Action<object?, string?>? outputCallBack)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string? GetCurrentProcessFileName()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string GetCurrentProcessLocation()
    {
        return Directory.GetCurrentDirectory();
    }

    /// <inheritdoc/>
    public string? GetTestEngineDirectory()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public int GetCurrentProcessId()
    {
        return -1;
    }

    /// <inheritdoc/>
    public string GetProcessName(int processId)
    {
        return string.Empty;
    }

    /// <inheritdoc/>
    public bool TryGetExitCode(object? process, out int exitCode)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetExitCallback(int parentProcessId, Action<object?>? callbackAction)
    {
    }

    /// <inheritdoc/>
    public void TerminateProcess(object? process)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public int GetProcessId(object? process)
    {
        return -1;
    }

    /// <inheritdoc/>
    public PlatformArchitecture GetCurrentProcessArchitecture()
    {
        return IntPtr.Size == 8 ? PlatformArchitecture.X64 : PlatformArchitecture.X86;
    }

    /// <inheritdoc/>
    public string GetNativeDllDirectory()
    {
        // For UWP the native dll's are to be kept in same directory
        return GetCurrentProcessLocation();
    }

    /// <inheritdoc/>
    public void WaitForProcessExit(object? process)
    {
        throw new NotImplementedException();
    }

    public IntPtr GetProcessHandle(int processId)
    {
        throw new NotImplementedException();
    }

    public object GetProcessById(int processId)
    {
        throw new NotImplementedException();
    }

    public PlatformArchitecture GetProcessArchitecture(int processId)
    {
        throw new NotImplementedException();
    }
}

#endif
