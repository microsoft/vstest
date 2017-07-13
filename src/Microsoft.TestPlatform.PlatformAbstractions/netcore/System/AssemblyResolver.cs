﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using System.Runtime.Loader;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    public class AssemblyResolver : IAssemblyResolver
    {
        private AssemblyResolverHandler resolverHandler;

        /// <summary>
        /// Specifies whether the resolver is disposed or not
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyResolver"/> class.
        /// </summary>
        /// <param name="directories"> The search directories. </param>
        [System.Security.SecurityCritical]
        public AssemblyResolver(IEnumerable<string> directories, IPlatformEqtTrace platformEqtTrace)
        {
            this.resolverHandler = new AssemblyResolverHandler(directories, platformEqtTrace);

            AssemblyLoadContext.Default.Resolving += this.AssemblyResolverEvent;
        }

        /// <inheritdoc />
        ~AssemblyResolver()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Set the directories from which assemblies should be searched
        /// </summary>
        /// <param name="directories"> The search directories. </param>
        [System.Security.SecurityCritical]
        public void AddSearchDirectories(IEnumerable<string> directories)
        {
            this.resolverHandler.AddSearchDirectories(directories);
        }

        /// <summary>
        /// Assembly Resolve event handler for App Domain - called when CLR loader cannot resolve assembly.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private Assembly AssemblyResolverEvent(object sender, object eventArgs)
        {
            AssemblyName args = eventArgs as AssemblyName;

            return args == null ? null : this.resolverHandler.AssemblyResolverEventHandler(args.Name);
        }

        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        [System.Security.SecurityCritical]
        protected void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    AssemblyLoadContext.Default.Resolving -= this.AssemblyResolverEvent;
                }

                this.isDisposed = true;
            }
        }

        /// <inheritdoc />
        public void SetCurrentDomainAssemblyResolve()
        {
            AssemblyLoadContext.Default.Resolving += this.CurrentDomainAssemblyResolve;
        }

        public void RemoveCurrentDomainAssemblyResolve()
        {
            AssemblyLoadContext.Default.Resolving -= this.CurrentDomainAssemblyResolve;
        }

        private Assembly CurrentDomainAssemblyResolve(AssemblyLoadContext loadContext, AssemblyName args)
        {
            return this.resolverHandler.CurrentDomainAssemblyResolveHelper(args.Name);
        }

    }
}
