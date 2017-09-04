// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Resources;

    public class VSExtensionManager : IVSExtensionManager
    {
        private const string PrivateAssembliesDirName = "PrivateAssemblies";
        private const string PublicAssembliesDirName = "PublicAssemblies";
        private const string ExtensionManagerService = "Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService";
        private const string ExtensionManagerAssemblyName = @"Microsoft.VisualStudio.ExtensionManager";
        private const string ExtensionManagerImplAssemblyName = @"Microsoft.VisualStudio.ExtensionManager.Implementation";
        private const string DevenvExe = "devenv.exe";

        private const string SettingsManagerTypeName = "Microsoft.VisualStudio.Settings.ExternalSettingsManager";
        private const string SettingsManagerAssemblyName = @"Microsoft.VisualStudio.Settings.15.0";
        
        private IFileHelper fileHelper;
        private string vsInstallPath;
        private string pathToDevenv;

        private Assembly extensionManagerAssembly;
        private Assembly extensionManagerImplAssembly;
        private Type extensionManagerServiceType;

        private Assembly settingsManagerAssembly;
        private Type settingsManagerType;

        public VSExtensionManager() : this(new FileHelper())
        {
        }

        internal VSExtensionManager(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Get the available unit test extensions installed.
        /// If no extensions are installed then it returns an empty list.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public IEnumerable<string> GetUnitTestExtensions()
        {
            try
            {
                return GetTestExtensionsInternal(Constants.UnitTestExtensionType);
            }
            catch (Exception ex)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resources.FailedToFindInstalledUnitTestExtensions, ex.Message);
                throw new TestPlatformException(message, ex);
            }
        }

        /// <summary>
        /// Get the unit tests extensions
        /// </summary>
        private IEnumerable<string> GetTestExtensionsInternal(string extensionType)
        {
            IEnumerable<string> installedExtensions = new List<string>();

            // Navigate to the IDE folder
            this.vsInstallPath = new DirectoryInfo(typeof(ITestPlatform).GetTypeInfo().Assembly.GetAssemblyLocation()).Parent.Parent.Parent.FullName;
            this.pathToDevenv = Path.Combine(vsInstallPath, DevenvExe);

            // Check for devenv
            if (!this.fileHelper.Exists(this.pathToDevenv))
            {
                throw new TestPlatformException(string.Format(CultureInfo.CurrentCulture, Resources.VSInstallationNotFound, this.vsInstallPath));
            }

            // Adding resolution paths for resolving dependencies.
            var resolutionPaths = new List<string>() {
                this.vsInstallPath,
                Path.Combine(this.vsInstallPath, PrivateAssembliesDirName),
                Path.Combine(this.vsInstallPath, PublicAssembliesDirName)
            };
            var assemblyResolver = new AssemblyResolver(resolutionPaths);

            object extensionManager;
            object settingsManager;
                                       
            settingsManager = SettingsManagerType.GetMethod("CreateForApplication", new Type[] { typeof(String) }).Invoke(null, new object[] { pathToDevenv });
            if (settingsManager != null)
            {
                // create extension manager
                extensionManager = Activator.CreateInstance(ExtensionManagerServiceType, settingsManager);

                if (extensionManager != null)
                {
                    installedExtensions = ExtensionManagerServiceType.GetMethod("GetEnabledExtensionContentLocations", new Type[] { typeof(String) }).Invoke(
                                               extensionManager, new object[] { extensionType }) as IEnumerable<string>;
                }
            
                // Dispose the settings manager
                IDisposable disposable = (settingsManager as IDisposable);
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            // Unregister the resolver by disposing
            assemblyResolver.Dispose();

            return installedExtensions;
        }

        /// <summary>
        /// Used to explicitly load Microsoft.VisualStudio.ExtensionManager.dll
        /// </summary>
        private Assembly ExtensionManagerDefAssembly
        {
            get
            {
                if (extensionManagerAssembly == null)
                {
                    extensionManagerAssembly = Assembly.Load(new AssemblyName(ExtensionManagerAssemblyName));
                }
                return extensionManagerAssembly;
            }
        }

        /// <summary>
        /// Used to explicitly load Microsoft.VisualStudio.ExtensionManager.Implementation.dll
        /// </summary>
        private Assembly ExtensionManagerImplAssembly
        {
            get
            {
                if (extensionManagerImplAssembly == null)
                { 
                    // Make sure ExtensionManager assembly is already loaded.
                    Assembly a = ExtensionManagerDefAssembly;
                    extensionManagerImplAssembly = Assembly.Load(new AssemblyName(ExtensionManagerImplAssemblyName));
                }

                return extensionManagerImplAssembly;
            }
        }

        /// <summary>
        /// Returns the Type of ExtensionManagerService.
        /// </summary>
        private Type ExtensionManagerServiceType
        {
            get
            {
                if (extensionManagerServiceType == null)
                {
                    extensionManagerServiceType = ExtensionManagerImplAssembly.GetType(ExtensionManagerService);
                }
                return extensionManagerServiceType;
            }
        }
        
        /// <summary>
        /// Used to explicitly load Microsoft.VisualStudio.Settings.15.0.dll
        /// </summary>
        private Assembly SettingsManagerAssembly
        {
            get
            {
                if (settingsManagerAssembly == null)
                {
                    settingsManagerAssembly = Assembly.Load(new AssemblyName(SettingsManagerAssemblyName));
                }

                return settingsManagerAssembly;
            }
        }

        private Type SettingsManagerType
        {
            get
            {
                if (settingsManagerType == null)
                {
                    settingsManagerType = SettingsManagerAssembly.GetType(SettingsManagerTypeName);
                }

                return settingsManagerType;
            }
        }
    }
}

