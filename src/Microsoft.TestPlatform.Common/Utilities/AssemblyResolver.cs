// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    internal class AssemblyResolver : IDisposable
    {
        /// <summary>
        /// The directories to look for assemblies to resolve.
        /// </summary>
        private HashSet<string> searchDirectories;

        /// <summary>
        /// Dictionary of Assemblies discovered to date. Must be locked as it may
        /// be accessed in a multi-threaded context.
        /// </summary>
        private Dictionary<string, Assembly> resolvedAssemblies;

        /// <summary>
        /// Specifies whether the resolver is disposed or not
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Assembly resolver for platform
        /// </summary>
        private IAssemblyResolver platformAssemblyResolver;

        private IAssemblyLoadContext platformAssemblyLoadContext;

        private static readonly string[] SupportedFileExtensions = { ".dll", ".exe" };

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyResolver"/> class.
        /// </summary>
        /// <param name="directories"> The search directories. </param>
        [System.Security.SecurityCritical]
        public AssemblyResolver(IEnumerable<string> directories)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"AssemblyResolver.ctor: Creating AssemblyResolver with searchDirectories {string.Join(",", directories)}");
            }

            this.resolvedAssemblies = new Dictionary<string, Assembly>();

            if (directories == null || !directories.Any())
            {
                this.searchDirectories = new HashSet<string>();
            }
            else
            {
                this.searchDirectories = new HashSet<string>(directories);
            }

            this.platformAssemblyResolver = new PlatformAssemblyResolver();
            this.platformAssemblyLoadContext = new PlatformAssemblyLoadContext();

            this.platformAssemblyResolver.AssemblyResolve += this.OnResolve;
        }

        /// <summary>
        /// Set the directories from which assemblies should be searched
        /// </summary>
        /// <param name="directories"> The search directories. </param>
        [System.Security.SecurityCritical]
        internal void AddSearchDirectories(IEnumerable<string> directories)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info($"AssemblyResolver.AddSearchDirectories: Adding more searchDirectories {string.Join(",", directories)}");
            }

            foreach (var directory in directories)
            {
                this.searchDirectories.Add(directory);
            }
        }

        /// <summary>
        /// Assembly Resolve event handler for App Domain - called when CLR loader cannot resolve assembly.
        /// </summary>
        /// <returns>
        /// The <see cref="Assembly"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private Assembly OnResolve(object sender, AssemblyResolveEventArgs args)
        {
            if (string.IsNullOrEmpty(args?.Name))
            {
                Debug.Fail("AssemblyResolver.OnResolve: args.Name is null or empty.");
                return null;
            }

            if (this.searchDirectories == null || this.searchDirectories.Count == 0)
            {
                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: There are no search directories, returning.", args.Name);
                return null;
            }

            EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Resolving assembly.", args.Name);

            // args.Name is like: "Microsoft.VisualStudio.TestTools.Common, Version=[VersionMajor].0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a".
            lock (this.resolvedAssemblies)
            {
                if (this.resolvedAssemblies.TryGetValue(args.Name, out var assembly))
                {
                    EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Resolved from cache.", args.Name);
                    return assembly;
                }

                AssemblyName requestedName = null;
                try
                {
                    // Can throw ArgumentException, FileLoadException if arg is empty/wrong format, etc. Should not return null.
                    requestedName = new AssemblyName(args.Name);
                }
                catch (Exception ex)
                {
                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Failed to create assemblyName. Reason:{1} ", args.Name, ex);
                    }

                    this.resolvedAssemblies[args.Name] = null;
                    return null;
                }

                Debug.Assert(requestedName != null && !string.IsNullOrEmpty(requestedName.Name), "AssemblyResolver.OnResolve: requested is null or name is empty!");

                foreach (var dir in this.searchDirectories)
                {
                    if (string.IsNullOrEmpty(dir))
                    {
                        continue;
                    }

                    EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Searching in: '{1}'.", args.Name, dir);

                    foreach (var extension in SupportedFileExtensions)
                    {
                        var assemblyPath = Path.Combine(dir, requestedName.Name + extension);
                        try
                        {
                            if (!File.Exists(assemblyPath))
                            {
                                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Assembly path does not exist: '{1}', returning.", args.Name, assemblyPath);

                                continue;
                            }

                            AssemblyName foundName = this.platformAssemblyLoadContext.GetAssemblyNameFromPath(assemblyPath);

                            if (!this.RequestedAssemblyNameMatchesFound(requestedName, foundName))
                            {
                                EqtTrace.Info("AssemblyResolver.OnResolve: {0}: File exists but version/public key is wrong. Try next extension.", args.Name);
                                continue;   // File exists but version/public key is wrong. Try next extension.
                            }

                            EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Loading assembly '{1}'.", args.Name, assemblyPath);
                            
                            assembly = this.platformAssemblyLoadContext.LoadAssemblyFromPath(assemblyPath);
                            this.resolvedAssemblies[args.Name] = assembly;

                            EqtTrace.Info("AssemblyResolver.OnResolve: Resolved assembly: {0}, from path: {1}", args.Name, assemblyPath);

                            return assembly;
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

                if (EqtTrace.IsInfoEnabled)
                {
                    EqtTrace.Info("AssemblyResolver.OnResolve: {0}: Failed to load assembly.", args.Name);
                }

                this.resolvedAssemblies[args.Name] = null;
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
        private bool RequestedAssemblyNameMatchesFound(AssemblyName requestedName, AssemblyName foundName)
        {
            Debug.Assert(requestedName != null);
            Debug.Assert(foundName != null);

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

            if (requestedName.Version != null)
            {
                return requestedName.Version.Equals(foundName.Version);
            }

            return true;
        }

        ~AssemblyResolver()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        [System.Security.SecurityCritical]
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.platformAssemblyResolver.AssemblyResolve -= this.OnResolve;
                    this.platformAssemblyResolver.Dispose();
                }

                this.isDisposed = true;
            }
        }
    }
}