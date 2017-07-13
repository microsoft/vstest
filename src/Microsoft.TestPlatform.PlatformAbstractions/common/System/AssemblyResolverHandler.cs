// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

#if !NET46
    using System.Runtime.Loader;
#endif

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
   
    internal class AssemblyResolverHandler
    {
        private static readonly string[] SupportedFileExtensions = { ".dll", ".exe" };

        private IPlatformEqtTrace platformEqtTrace;

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
        /// Initializes a new instance of the <see cref="AssemblyResolverHandler"/> class.
        /// </summary>
        /// <param name="directories"> The search directories. </param>
        /// <param name="platformEqtTrace">To trace failures</param>
        public AssemblyResolverHandler(IEnumerable<string> directories, IPlatformEqtTrace platformEqtTrace)
        {
            this.resolvedAssemblies = new Dictionary<string, Assembly>();

            this.platformEqtTrace = platformEqtTrace;

            if (directories == null || !directories.Any())
            {
                this.searchDirectories = new HashSet<string>();
            }
            else
            {
                this.searchDirectories = new HashSet<string>(directories);
            }
        }

        /// <summary>
        /// Set the directories from which assemblies should be searched
        /// </summary>
        /// <param name="directories"> The search directories. </param>
        [System.Security.SecurityCritical]
        public void AddSearchDirectories(IEnumerable<string> directories)
        {
            foreach (var directory in directories)
            {
                this.searchDirectories.Add(directory);
            }
        }

        /// <summary>
        /// Assembly Resolve event handler for App Domain - called when CLR loader cannot resolve assembly.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly Name.
        /// </param>
        /// <returns>
        /// The <see cref="Assembly"/>.
        /// </returns>
        public Assembly AssemblyResolverEventHandler(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                Debug.Assert(false, "AssemblyResolver.OnResolve: args.Name is null or empty.");
                return null;
            }

            if (this.searchDirectories == null || this.searchDirectories.Count == 0)
            {
                return null;
            }

            WriteToEqtTrace("AssemblyResolver: {0}: Resolving assembly.", assemblyName);

            // args.Name is like: "Microsoft.VisualStudio.TestTools.Common, Version=[VersionMajor].0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a".
            lock (this.resolvedAssemblies)
            {
                if (this.resolvedAssemblies.TryGetValue(assemblyName, out Assembly assembly))
                {
                    if (assembly != null)
                    {
                        WriteToEqtTrace("AssemblyResolver: {0}: Resolved from cache.", assemblyName);
                    }

                    return assembly;
                }

                AssemblyName requestedName = null;
                try
                {
                    // Can throw ArgumentException, FileLoadException if arg is empty/wrong format, etc. Should not return null.
                    requestedName = new AssemblyName(assemblyName);
                }
                catch (Exception ex)
                {
                    WriteToEqtTrace("AssemblyResolver: {0}: Failed to create assemblyName. Reason:{1} ", assemblyName, ex);
                    return null;
                }

                Debug.Assert(requestedName != null && !string.IsNullOrEmpty(requestedName.Name), "AssemblyResolver.OnResolve: requested is null or name is empty!");

                foreach (var dir in this.searchDirectories)
                {
                    if (string.IsNullOrEmpty(dir))
                    {
                        continue;
                    }

                    foreach (var extension in SupportedFileExtensions)
                    {
                        var assemblyPath = Path.Combine(dir, requestedName.Name + extension);
                        try
                        {
                            if (!File.Exists(assemblyPath))
                            {
                                continue;
                            }

#if NET46
                            AssemblyName foundName = AssemblyName.GetAssemblyName(assemblyPath);
                            if (!this.RequestedAssemblyNameMatchesFound(requestedName, foundName))
                            {
                                continue;   // File exists but version/public key is wrong. Try next extension.
                            }

                            // When file does not exist it throws FileNotFoundException.
                            assembly = Assembly.LoadFrom(assemblyPath);
#else
                            AssemblyName foundName = AssemblyLoadContext.GetAssemblyName(assemblyPath);
                            if (!this.RequestedAssemblyNameMatchesFound(requestedName, foundName))
                            {
                                continue;   // File exists but version/public key is wrong. Try next extension.
                            }

                            assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#endif
                            this.resolvedAssemblies[assemblyName] = assembly;

                            WriteToEqtTrace("AssemblyResolver: {0}: Resolved assembly. ", assemblyName);

                            return assembly;
                        }
                        catch (FileLoadException ex)
                        {
                            WriteToEqtTrace("AssemblyResolver: {0}: Failed to load assembly. Reason:{1} ", assemblyName, ex);
                            
                            // Rethrow FileLoadException, because this exception means that the assembly
                            // was found, but could not be loaded. This will allow us to report a more
                            // specific error message to the user for things like access denied.
                            throw;
                        }
                        catch (Exception ex)
                        {
                            WriteToEqtTrace("AssemblyResolver: {0}: Failed to load assembly. Reason:{1} ", assemblyName, ex);
                        }
                    }
                }
            }

            return null;
        }

        public Assembly CurrentDomainAssemblyResolveHelper(string assemblyNameArgs)
        {
            var assemblyName = new AssemblyName(assemblyNameArgs);

            Assembly assembly = null;
            lock (this.resolvedAssemblies)
            {
                try
                {
                    WriteToEqtTrace("CurrentDomain_AssemblyResolve: Resolving assembly '{0}'.", assemblyName);

                    if (this.resolvedAssemblies.TryGetValue(assemblyNameArgs, out assembly))
                    {
                        return assembly;
                    }

                    // Put it in the resolved assembly so that if below Assembly.Load call
                    // triggers another assembly resolution, then we dont end up in stack overflow
                    this.resolvedAssemblies[assemblyNameArgs] = null;

                    assembly = Assembly.Load(assemblyName);

                    // Replace the value with the loaded assembly
                    this.resolvedAssemblies[assemblyNameArgs] = assembly;

                    return assembly;
                }
                finally
                {
                    if (assembly == null)
                    {
                        WriteToEqtTrace("CurrentDomainAssemblyResolve: Failed to resolve assembly '{0}'.", assemblyName);
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that found assembly name matches requested to avoid security issues.
        /// Looks only at PublicKeyToken and Version, empty matches anything.
        /// VSWhidbey 415774.
        /// </summary>
        /// <param name="requestedName">
        /// The requested Name.
        /// </param>
        /// <param name="foundName">
        /// The found Name.
        /// </param>
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

        private void WriteToEqtTrace(string format, params object[] args)
        {
            if (this.platformEqtTrace.ShouldTrace(PlatformTraceLevel.Info))
            {
                string message = string.Format(CultureInfo.InvariantCulture, format, args);

                this.platformEqtTrace.WriteLine(PlatformTraceLevel.Info, message);
            }
        }
    }
}
