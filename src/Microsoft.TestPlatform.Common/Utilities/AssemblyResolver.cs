// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities;

internal class AssemblyResolver : IDisposable
{
    /// <summary>
    /// The directories to look for assemblies to resolve.
    /// </summary>
    private readonly HashSet<string> _searchDirectories;

    /// <summary>
    /// Dictionary of Assemblies discovered to date. Must be locked as it may
    /// be accessed in a multi-threaded context.
    /// </summary>
    private readonly Dictionary<string, Assembly?> _resolvedAssemblies = new();

    /// <summary>
    /// Specifies whether the resolver is disposed or not
    /// </summary>
    private bool _isDisposed;
    private Stack<string>? _currentlyResolvingResources;

    /// <summary>
    /// Assembly resolver for platform
    /// </summary>
    private readonly IAssemblyResolver _platformAssemblyResolver;

    private readonly IAssemblyLoadContext _platformAssemblyLoadContext;

    private static readonly string[] SupportedFileExtensions = [".dll", ".exe"];

    /// <summary>
    /// Assemblies that vstest resolved from its own directory on behalf of an extension
    /// that did not ship its own copy. Key: assembly name, Value: requesting assembly name.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> ProvidedDependencies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Known assemblies that extensions should ship themselves.
    /// When vstest resolves these from its own directory for an external requester,
    /// we track it in telemetry and optionally warn the user.
    /// </summary>
    private static readonly HashSet<string> TrackedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Newtonsoft.Json",
    };

    /// <summary>
    /// The directory where vstest's own assemblies live.
    /// </summary>
    private static readonly string TestPlatformDirectory =
        Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.GetAssemblyLocation()) ?? string.Empty;

    /// <summary>
    /// Gets the assemblies that vstest provided from its own directory to resolve
    /// dependencies of extensions that did not ship their own copy.
    /// Key: assembly name, Value: requesting assembly name.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> GetProvidedDependencies() => ProvidedDependencies;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyResolver"/> class.
    /// </summary>
    /// <param name="directories"> The search directories. </param>
    [System.Security.SecurityCritical]
    public AssemblyResolver(IEnumerable<string> directories)
    {
        EqtTrace.Info($"AssemblyResolver.ctor: Creating AssemblyResolver with searchDirectories {string.Join(",", directories)}");

        _searchDirectories = directories == null || !directories.Any() ? new HashSet<string>() : new HashSet<string>(directories);

        _platformAssemblyResolver = new PlatformAssemblyResolver();
        _platformAssemblyLoadContext = new PlatformAssemblyLoadContext();

        _platformAssemblyResolver.AssemblyResolve += OnResolve;
    }

    /// <summary>
    /// Set the directories from which assemblies should be searched
    /// </summary>
    /// <param name="directories"> The search directories. </param>
    [System.Security.SecurityCritical]
    internal void AddSearchDirectories(IEnumerable<string> directories)
    {
        EqtTrace.Info($"AssemblyResolver.AddSearchDirectories: Adding more searchDirectories {string.Join(",", directories)}");

        foreach (var directory in directories)
        {
            _searchDirectories.Add(directory);
        }
    }

    /// <summary>
    /// Assembly Resolve event handler for App Domain - called when CLR loader cannot resolve assembly.
    /// </summary>
    /// <returns>
    /// The <see cref="Assembly"/>.
    /// </returns>
    private Assembly? OnResolve(object? sender, AssemblyResolveEventArgs? args)
    {
        if (StringUtils.IsNullOrEmpty(args?.Name))
        {
            Debug.Fail("AssemblyResolver.OnResolve: args.Name is null or empty.");
            return null;
        }

        if (_searchDirectories == null || _searchDirectories.Count == 0)
        {
            EqtTrace.Info("AssemblyResolver.OnResolve: {0}: There are no search directories, returning.", args.Name);
            return null;
        }

        EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Resolving assembly.", args.Name);

        // args.Name is like: "Microsoft.VisualStudio.TestTools.Common, Version=[VersionMajor].0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a".
        lock (_resolvedAssemblies)
        {
            if (_resolvedAssemblies.TryGetValue(args.Name, out var assembly))
            {
                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Resolved from cache.", args.Name);
                return assembly;
            }

            AssemblyName? requestedName = null;
            try
            {
                // Can throw ArgumentException, FileLoadException if arg is empty/wrong format, etc. Should not return null.
                requestedName = new AssemblyName(args.Name);
            }
            catch (Exception ex)
            {
                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Failed to create assemblyName. Reason:{1} ", args.Name, ex);

                _resolvedAssemblies[args.Name] = null;
                return null;
            }

            TPDebug.Assert(requestedName != null && !requestedName.Name.IsNullOrEmpty(), "AssemblyResolver.OnResolve: requested is null or name is empty!");

            var isResource = requestedName.Name.EndsWith(".resources");
            foreach (var dir in _searchDirectories)
            {
                if (dir.IsNullOrEmpty())
                {
                    continue;
                }

                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Searching in: '{1}'.", args.Name, dir);

                foreach (var extension in SupportedFileExtensions)
                {
                    var assemblyPath = Path.Combine(dir, requestedName.Name + extension);
                    try
                    {
                        bool pushed = false;
                        try
                        {
                            if (isResource)
                            {
                                // Check for recursive resource lookup.
                                // This can happen when we are on non-english locale, and we try to load mscorlib.resources
                                // (or potentially some other resources). This will trigger a new Resolve and call the method
                                // we are currently in. If then some code in this Resolve method (like File.Exists) will again
                                // try to access mscorlib.resources it will end up recursing forever.

                                if (_currentlyResolvingResources != null && _currentlyResolvingResources.Count > 0 && _currentlyResolvingResources.Contains(assemblyPath))
                                {
                                    EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Assembly is searching for itself recursively: '{1}', returning as not found.", args.Name, assemblyPath);
                                    _resolvedAssemblies[args.Name] = null;
                                    return null;
                                }

                                _currentlyResolvingResources ??= new Stack<string>(4);
                                _currentlyResolvingResources.Push(assemblyPath);
                                pushed = true;
                            }

                            if (!File.Exists(assemblyPath))
                            {
                                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Assembly path does not exist: '{1}', returning.", args.Name, assemblyPath);

                                continue;
                            }

                            AssemblyName foundName = _platformAssemblyLoadContext.GetAssemblyNameFromPath(assemblyPath);

                            if (!RequestedAssemblyNameMatchesFound(requestedName, foundName))
                            {
                                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: File exists but version/public key is wrong. Try next extension.", args.Name);
                                continue;   // File exists but version/public key is wrong. Try next extension.
                            }

                            EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Loading assembly '{1}'.", args.Name, assemblyPath);

                            assembly = _platformAssemblyLoadContext.LoadAssemblyFromPath(assemblyPath);
                            _resolvedAssemblies[args.Name] = assembly;

                            TrackProvidedDependency(requestedName, assemblyPath, args.RequestingAssembly);

                            EqtTrace.Info("AssemblyResolver.OnResolve: Resolved assembly: {0}, from path: {1}", args.Name, assemblyPath);

                            return assembly;
                        }
                        finally
                        {
                            if (isResource && pushed)
                            {
                                _currentlyResolvingResources?.Pop();
                            }

                        }
                    }
                    catch (FileLoadException ex)
                    {
                        EqtTrace.Error("AssemblyResolver.OnResolve: {0}: Failed to load assembly. Reason:{1} ", args.Name, ex);

                        // Re-throw FileLoadException, because this exception means that the assembly
                        // was found, but could not be loaded. This will allow us to report a more
                        // specific error message to the user for things like access denied.
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // For all other exceptions, try the next extension.
                        EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Failed to load assembly. Reason:{1} ", args.Name, ex);
                    }
                }
            }

            EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Failed to load assembly.", args.Name);

            _resolvedAssemblies[args.Name] = null;
            return null;
        }
    }

    /// <summary>
    /// When we resolve a tracked assembly (e.g. Newtonsoft.Json) from vstest's own directory
    /// for an external requester, record it for telemetry and optionally warn the user.
    /// </summary>
    private static void TrackProvidedDependency(AssemblyName requestedName, string resolvedPath, Assembly? requestingAssembly)
    {
        if (requestedName.Name is null || !TrackedAssemblies.Contains(requestedName.Name))
        {
            return;
        }

        // Check if we resolved from vstest's own directory (not the extension's directory).
        var resolvedDir = Path.GetDirectoryName(resolvedPath);
        if (resolvedDir is null || !resolvedDir.Equals(TestPlatformDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var requester = requestingAssembly?.GetName().Name ?? "unknown";

        // Don't track vstest's own assemblies requesting Newtonsoft.Json — that's expected.
        if (requester.StartsWith("Microsoft.VisualStudio.TestPlatform", StringComparison.OrdinalIgnoreCase)
            || requester.StartsWith("Microsoft.TestPlatform", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!ProvidedDependencies.TryAdd(requestedName.Name, requester))
        {
            return;
        }

        EqtTrace.Info("AssemblyResolver.TrackProvidedDependency: Resolved '{0}' from vstest directory for '{1}'. " +
            "The extension should ship its own copy.", requestedName.Name, requester);

        // When the feature flag is set, warn the user so they can fix their extension.
        if (FeatureFlag.Instance.IsSet(FeatureFlag.VSTEST_WARN_MISSING_EXTENSIONS_DEPENDENCIES))
        {
            var message = $"Assembly '{requestedName.Name}' was resolved from the test platform directory for '{requester}'. "
                + $"Test extensions should ship their own copy of '{requestedName.Name}'.";
            try
            {
                TestSessionMessageLogger.Instance.SendMessage(TestMessageLevel.Warning, message);
            }
            catch
            {
                // TestSessionMessageLogger may not be initialized in all contexts.
            }
        }
    }

    /// <summary>
    /// Verifies that found assembly name matches requested to avoid security issues.
    /// Looks only at PublicKeyToken and Version, empty matches anything.
    /// VSWhidbey 415774.
    /// </summary>
    /// <returns>
    /// The <see cref="bool"/>.
    /// </returns>
    private static bool RequestedAssemblyNameMatchesFound(AssemblyName requestedName, AssemblyName foundName)
    {
        TPDebug.Assert(requestedName != null);
        TPDebug.Assert(foundName != null);

        var requestedPublicKey = requestedName.GetPublicKeyToken();
        if (requestedPublicKey != null)
        {
            var foundPublicKey = foundName.GetPublicKeyToken();
            if (foundPublicKey == null)
            {
                return false;
            }

            for (var index = 0; index < requestedPublicKey.Length; ++index)
            {
                if (requestedPublicKey[index] != foundPublicKey[index])
                {
                    return false;
                }
            }
        }

        return requestedName.Version == null || requestedName.Version.Equals(foundName.Version);
    }

    ~AssemblyResolver()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);

        // Use SupressFinalize in case a subclass
        // of this type implements a finalizer.
        GC.SuppressFinalize(this);
    }

    [System.Security.SecurityCritical]
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _platformAssemblyResolver.AssemblyResolve -= OnResolve;
                _platformAssemblyResolver.Dispose();
            }

            _isDisposed = true;
        }
    }
}
