// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;

namespace Microsoft.VisualStudio.TestPlatform.TestHost;

internal class AssemblyLoadContextEngineInvoker<T> : IEngineInvoker, IDisposable
    where T : IEngineInvoker, new()
{
    private readonly PluginLoadContext _context;
    private readonly IEngineInvoker _actualInvoker;

    public AssemblyLoadContextEngineInvoker(string testSourcePath)
    {
        // REVIEW: Do we want another event?
        TestPlatformEventSource.Instance.TestHostAppDomainCreationStart();

        // TODO: Do something for runtimeconfig.json and deps.json

        // TODO: In the AppDomain counterpart we have an intermediate layer used to setup the
        // correct UI culture to propagate the dotnet or VS culture to the adapters running in the
        // app domain. See how to do so.
        _context = new PluginLoadContext(testSourcePath);
        var assembly = _context.LoadFromAssemblyName(AssemblyName.GetAssemblyName(testSourcePath));
        _actualInvoker = CreateFirstAssignableType(assembly);

        // REVIEW: Do we want another event?
        TestPlatformEventSource.Instance.TestHostAppDomainCreationStop();
    }

    public void Invoke(IDictionary<string, string?> argsDictionary)
    {
        try
        {
            _actualInvoker.Invoke(argsDictionary);
        }
        catch
        {
            // ignore
        }
    }

    // REVIEW: AppDomain equivalent is not disposable
    public void Dispose()
    {
        _context.Unload();
    }

    // REVIEW: Shall we warn if there are multiple invoker available?
    private static IEngineInvoker CreateFirstAssignableType(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(IEngineInvoker).IsAssignableFrom(type))
            {
                // REVIEW: Ok to throw?
                return Activator.CreateInstance(type) is not IEngineInvoker instance
                    ? throw new InvalidOperationException($"Cannot create instance of '{nameof(IEngineInvoker)}' for type '{type}'.")
                    : instance;
            }
        }

        // REVIEW: Ok to throw?
        throw new InvalidOperationException($"Could not find any type compatible with '{nameof(IEngineInvoker)}' in '{assembly.FullName}'.");
    }
}

#endif
