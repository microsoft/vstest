// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0_OR_GREATER

using System;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <summary>
/// Represents a scope context for resolving assemblies and their dependencies.
/// </summary>
internal sealed class PluginLoadContext : AssemblyLoadContext, IAssemblyLoadContext, IDisposable
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly IPlatformEqtTrace _platformEqtTrace;

    /// <summary>
    /// </summary>
    /// <param name="pluginPath">The path to the component or plugin's managed entry point.</param>
    public PluginLoadContext(string? name, string pluginPath!!)
        : this(name, pluginPath, PlatformEqtTrace.Instance)
    {
    }

    public PluginLoadContext(string? name, string pluginPath!!, IPlatformEqtTrace platformEqtTrace!!)
        : base(name, isCollectible: true) // Required to enable unloading
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _platformEqtTrace = platformEqtTrace;
    }

    public void Dispose()
    {
        // We implement IDisposable to give a clearer insight to callers that we need to unload.
        Unload();
    }

    public AssemblyName GetAssemblyNameFromPath(string assemblyPath)
        => GetAssemblyName(assemblyPath);

    public Assembly LoadAssemblyFromPath(string assemblyPath)
        => LoadFromAssemblyPath(assemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        LogVerbose($"PluginLoadContext.Load: Resolving assembly '{assemblyName.Name}'.");

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            // REVIEW: Shall we limit the paths from which we load (e.g. VS, sdk, test assembly)?
            return LoadFromAssemblyPath(assemblyPath);
        }

        LogVerbose($"PluginLoadContext.Load: Failed to resolve assembly '{assemblyName.Name}'.");
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        LogVerbose($"PluginLoadContext.LoadUnmanagedDll: Resolving assembly '{unmanagedDllName}'.");

        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            // REVIEW: Shall we limit the paths from which we load (e.g. VS, sdk, test assembly)?
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        LogVerbose($"PluginLoadContext.LoadUnmanagedDll: Failed to resolve assembly '{unmanagedDllName}'.");
        return IntPtr.Zero;
    }

    private void LogVerbose(string message)
    {
        if (_platformEqtTrace.ShouldTrace(PlatformTraceLevel.Verbose))
        {
            _platformEqtTrace.WriteLine(PlatformTraceLevel.Verbose, message);
        }
    }
}

#endif
