// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

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

    private static readonly string[] SupportedFileExtensions = { ".dll", ".exe" };

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

            // Workaround: adding expected folder for the satellite assembly related to the current CurrentThread.CurrentUICulture relative to the current assembly location.
            // After the move to the net461 the runtime doesn't resolve anymore the satellite assembly correctly.
            // The expected workflow should be https://learn.microsoft.com/en-us/dotnet/core/extensions/package-and-deploy-resources#net-framework-resource-fallback-process
            // But the resolution never fallback to the CultureInfo.Parent folder and fusion log return a failure like:
            // ...
            // LOG: The same bind was seen before, and was failed with hr = 0x80070002.
            // ERR: Unrecoverable error occurred during pre - download check(hr = 0x80070002).
            // ...
            // The bizarre thing is that as a result we're failing caller task like discovery and when for reporting reason
            // we're accessing again to the resource it works.
            // Looks like a loading timing issue but we're not in control of the assembly loader order.
            var isResource = requestedName.Name.EndsWith(".resources");
            string[]? satelliteLocation = null;

            // We help to resolve only test platform resources to be less invasive as possible with the default/expected behavior
            if (isResource && requestedName.Name.StartsWith("Microsoft.VisualStudio.TestPlatform"))
            {
                try
                {
                    string? currentAssemblyLocation = null;
                    try
                    {
                        currentAssemblyLocation = Assembly.GetExecutingAssembly().Location;
                        // In .NET 5 and later versions, for bundled assemblies, the value returned is an empty string.
                        currentAssemblyLocation = currentAssemblyLocation == string.Empty ? null : Path.GetDirectoryName(currentAssemblyLocation);
                    }
                    catch (NotSupportedException)
                    {
                        // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location
                    }

                    if (currentAssemblyLocation is not null)
                    {
                        List<string> satelliteLocations = new();

                        // We mimic the satellite workflow and we add CurrentUICulture and CurrentUICulture.Parent folder in order
                        string? currentUICulture = Thread.CurrentThread.CurrentUICulture?.Name;
                        if (currentUICulture is not null)
                        {
                            satelliteLocations.Add(Path.Combine(currentAssemblyLocation, currentUICulture));
                        }

                        // CurrentUICulture.Parent
                        string? parentCultureInfo = Thread.CurrentThread.CurrentUICulture?.Parent?.Name;
                        if (parentCultureInfo is not null)
                        {
                            satelliteLocations.Add(Path.Combine(currentAssemblyLocation, parentCultureInfo));
                        }

                        if (satelliteLocations.Count > 0)
                        {
                            satelliteLocation = satelliteLocations.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // We catch here because this is a workaround, we're trying to substitute the expected workflow of the runtime
                    // and this shouldn't be needed, but if we fail we want to log what's happened and give a chance to the in place
                    // resolution workflow
                    EqtTrace.Error($"AssemblyResolver.OnResolve: Exception during the custom satellite resolution\n{ex}");
                }
            }

            foreach (var dir in (satelliteLocation is not null) ? _searchDirectories.Union(satelliteLocation) : _searchDirectories)
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
