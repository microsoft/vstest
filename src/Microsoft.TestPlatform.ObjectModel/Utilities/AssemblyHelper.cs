// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
#if NETFRAMEWORK
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Implementation of finding assembly references using "managed route", i.e. Assembly.Load.
    /// </summary>
    public static class AssemblyHelper
    {
        private static Version defaultVersion = new Version();
        private static Version version45 = new Version("4.5");

        /// <summary>
        /// Checks whether the source assembly directly references given assembly or not.
        /// Only assembly name and public key token are match. Version is ignored for matching.
        /// Returns null if not able to check if source references assembly.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool? DoesReferencesAssembly(string source, AssemblyName referenceAssembly)
        {
            try
            {
                ValidateArg.NotNullOrEmpty(source, "source");
                ValidateArg.NotNull(referenceAssembly, "referenceAssembly");

                Debug.Assert(!string.IsNullOrEmpty(source));

                var referenceAssemblyName = referenceAssembly.Name;
                var referenceAssemblyPublicKeyToken = referenceAssembly.GetPublicKeyToken();

                var setupInfo = new AppDomainSetup();
                setupInfo.ApplicationBase = Path.GetDirectoryName(Path.GetFullPath(source));

                // In Dev10 by devenv uses its own app domain host which has default optimization to share everything.
                // Set LoaderOptimization to MultiDomainHost which means:
                //   Indicates that the application will probably host unique code in multiple domains,
                //   and the loader must share resources across application domains only for globally available (strong-named)
                //   assemblies that have been added to the global assembly cache.
                setupInfo.LoaderOptimization = LoaderOptimization.MultiDomainHost;

                AppDomain ad = null;
                try
                {
                    ad = AppDomain.CreateDomain("Dependency finder domain", null, setupInfo);

                    var assemblyLoadWorker = typeof(AssemblyLoadWorker);
                    AssemblyLoadWorker worker = null;
                    if (assemblyLoadWorker.Assembly.GlobalAssemblyCache)
                    {
                        worker = (AssemblyLoadWorker)ad.CreateInstanceAndUnwrap(
                            assemblyLoadWorker.Assembly.FullName,
                            assemblyLoadWorker.FullName,
                            false, BindingFlags.Default, null,
                            null, null, null);
                    }
                    else
                    {
                        // This has to be LoadFrom, otherwise we will have to use AssemblyResolver to find self.
                        worker = (AssemblyLoadWorker)ad.CreateInstanceFromAndUnwrap(
                            assemblyLoadWorker.Assembly.Location,
                            assemblyLoadWorker.FullName,
                            false, BindingFlags.Default, null,
                            null, null, null);
                    }

                    return worker.CheckAssemblyReference(source, referenceAssemblyName, referenceAssemblyPublicKeyToken);
                }
                finally
                {
                    if (ad != null)
                    {
                        AppDomain.Unload(ad);
                    }
                }
            }
            catch
            {
                return null; // Return null if something goes wrong.
            }
        }

        /// <summary>
        /// Finds platform and .Net framework version for a given test container.
        /// If there is an error while inferring this information, defaults (AnyCPU, None) are returned
        /// for faulting container.
        /// </summary>
        /// <param name="testSource"></param>
        /// <returns></returns>
        public static KeyValuePair<Architecture, FrameworkVersion> GetFrameworkVersionAndArchitectureForSource(string testSource)
        {
            ValidateArg.NotNullOrEmpty(testSource, "testSource");

            var sourceDirectory = Path.GetDirectoryName(testSource);
            var setupInfo = new AppDomainSetup();
            setupInfo.ApplicationBase = sourceDirectory;
            setupInfo.LoaderOptimization = LoaderOptimization.MultiDomainHost;
            AppDomain ad = null;
            try
            {
                ad = AppDomain.CreateDomain("Multiargeting settings domain", null, setupInfo);

                Type assemblyLoadWorker = typeof(AssemblyLoadWorker);
                AssemblyLoadWorker worker = null;

                // This has to be LoadFrom, otherwise we will have to use AssemblyResolver to find self.
                worker = (AssemblyLoadWorker)ad.CreateInstanceFromAndUnwrap(
                    assemblyLoadWorker.Assembly.Location,
                    assemblyLoadWorker.FullName,
                    false, BindingFlags.Default, null,
                    null, null, null);

                string procArchType;
                string frameworkVersion;
                worker.GetPlatformAndFrameworkSettings(testSource, out procArchType, out frameworkVersion);

                Architecture targetPlatform = (Architecture)Enum.Parse(typeof(Architecture), procArchType);
                FrameworkVersion targetFramework = FrameworkVersion.Framework45;
                switch (frameworkVersion.ToUpperInvariant())
                {
                    case "V4.5":
                        targetFramework = FrameworkVersion.Framework45;
                        break;

                    case "V4.0":
                        targetFramework = FrameworkVersion.Framework40;
                        break;

                    case "V3.5":
                    case "V2.0":
                        targetFramework = FrameworkVersion.Framework35;
                        break;

                    default:
                        targetFramework = FrameworkVersion.None;
                        break;
                }
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("Inferred Multi-Targeting settings:{0} Platform:{1} FrameworkVersion:{2}", testSource, targetPlatform, targetFramework);
                }
                return new KeyValuePair<Architecture, FrameworkVersion>(targetPlatform, targetFramework);

            }
            finally
            {
                if (ad != null)
                {
                    AppDomain.Unload(ad);
                }
            }
        }


        /// <summary>
        /// Returns the full name (AssemblyName.FullName) of the referenced assemblies by the assembly on the specified path.
        ///
        /// Returns null on failure and an empty array if there is no reference in the project.
        /// </summary>
        /// <param name="source">Full path to the assembly to get dependencies for.</param>
        public static string[] GetReferencedAssemblies(string source)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));

            var setupInfo = new AppDomainSetup();
            setupInfo.ApplicationBase = Path.GetDirectoryName(Path.GetFullPath(source));

            // In Dev10 by devenv uses its own app domain host which has default optimization to share everything.
            // Set LoaderOptimization to MultiDomainHost which means:
            //   Indicates that the application will probably host unique code in multiple domains,
            //   and the loader must share resources across application domains only for globally available (strong-named)
            //   assemblies that have been added to the global assembly cache.
            setupInfo.LoaderOptimization = LoaderOptimization.MultiDomainHost;

            AppDomain ad = null;
            try
            {
                ad = AppDomain.CreateDomain("Dependency finder domain", null, setupInfo);

                var assemblyLoadWorker = typeof(AssemblyLoadWorker);
                AssemblyLoadWorker worker = null;
                if (assemblyLoadWorker.Assembly.GlobalAssemblyCache)
                {
                    worker = (AssemblyLoadWorker)ad.CreateInstanceAndUnwrap(
                        assemblyLoadWorker.Assembly.FullName,
                        assemblyLoadWorker.FullName,
                        false, BindingFlags.Default, null,
                        null, null, null);
                }
                else
                {
                    // This has to be LoadFrom, otherwise we will have to use AssemblyResolver to find self.
                    worker = (AssemblyLoadWorker)ad.CreateInstanceFromAndUnwrap(
                        assemblyLoadWorker.Assembly.Location,
                        assemblyLoadWorker.FullName,
                        false, BindingFlags.Default, null,
                        null, null, null);
                }

                return worker.GetReferencedAssemblies(source);
            }
            finally
            {
                if (ad != null)
                {
                    AppDomain.Unload(ad);
                }
            }
        }

        /// <summary>
        /// Set the target framework for app domain setup if target framework of dll is > 4.5
        /// </summary>
        /// <param name="setup">AppdomainSetup for app domain creation</param>
        /// <param name="testSource">path of test file</param>
        public static void SetAppDomainFrameworkVersionBasedOnTestSource(AppDomainSetup setup, string testSource)
        {
            string assemblyVersionString = GetTargetFrameworkVersionString(testSource);

            if (GetTargetFrameworkVersionFromVersionString(assemblyVersionString).CompareTo(version45) > 0)
            {
                var pInfo = typeof(AppDomainSetup).GetProperty(Constants.TargetFrameworkName);
                if (null != pInfo)
                {
                    pInfo.SetValue(setup, assemblyVersionString, null);
                }
            }
        }

        /// <summary>
        /// Get the target dot net framework string for the assembly
        /// </summary>
        /// <param name="path">Path of the assembly file</param>
        /// <returns>String representation of the target dot net framework e.g. .NETFramework,Version=v4.0 </returns>
        internal static string GetTargetFrameworkVersionString(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            var setupInfo = new AppDomainSetup();
            setupInfo.ApplicationBase = Path.GetDirectoryName(Path.GetFullPath(path));

            // In Dev10 by devenv uses its own app domain host which has default optimization to share everything.
            // Set LoaderOptimization to MultiDomainHost which means:
            //   Indicates that the application will probably host unique code in multiple domains,
            //   and the loader must share resources across application domains only for globally available (strong-named)
            //   assemblies that have been added to the global assembly cache.
            setupInfo.LoaderOptimization = LoaderOptimization.MultiDomainHost;

            if (File.Exists(path))
            {
                AppDomain ad = null;
                try
                {
                    ad = AppDomain.CreateDomain("Framework Version String Domain", null, setupInfo);

                    var assemblyLoadWorker = typeof(AssemblyLoadWorker);
                    AssemblyLoadWorker worker = null;
                    if (assemblyLoadWorker.Assembly.GlobalAssemblyCache)
                    {
                        worker = (AssemblyLoadWorker)ad.CreateInstanceAndUnwrap(
                            assemblyLoadWorker.Assembly.FullName,
                            assemblyLoadWorker.FullName,
                            false, BindingFlags.Default, null,
                            null, null, null);
                    }
                    else
                    {
                        // This has to be LoadFrom, otherwise we will have to use AssemblyResolver to find self.
                        worker = (AssemblyLoadWorker)ad.CreateInstanceFromAndUnwrap(
                            assemblyLoadWorker.Assembly.Location,
                            assemblyLoadWorker.FullName,
                            false, BindingFlags.Default, null,
                            null, null, null);
                    }

                    return worker.GetTargetFrameworkVersionStringFromPath(path);
                }
                finally
                {
                    if (ad != null)
                    {
                        AppDomain.Unload(ad);
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the Version for the target framework version string
        /// </summary>
        /// <param name="version">Target framework string</param>
        /// <returns>Framework Version</returns>
        internal static Version GetTargetFrameworkVersionFromVersionString(string version)
        {
            if (version.Length > Constants.DotNetFrameWorkStringPrefix.Length + 1)
            {
                string versionPart = version.Substring(Constants.DotNetFrameWorkStringPrefix.Length + 1);
                return new Version(versionPart);
            }

            return defaultVersion;
        }

        /// <summary>
        /// When test run is targeted for .Net4.0, set target framework of test appdomain to be v4.0.
        /// With this done tests would be executed in 4.0 compatibility mode even when  .Net4.5 is installed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Failure to set this property should be ignored.")]
        internal static void SetNETFrameworkCompatiblityMode(AppDomainSetup setup, IRunContext runContext)
        {
            try
            {
                RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runContext.RunSettings.SettingsXml);
                if (null != runConfiguration && (Enum.Equals(runConfiguration.TargetFramework, FrameworkVersion.Framework40) ||
                    string.Equals(runConfiguration.TargetFramework.ToString(), Constants.DotNetFramework40, StringComparison.OrdinalIgnoreCase)))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("AssemblyHelper.SetNETFrameworkCompatiblityMode: setting .NetFramework,Version=v4.0 compatibility mode.");
                    }
                    setup.TargetFrameworkName = Constants.DotNetFramework40;
                }
            }
            catch (Exception e)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("AssemblyHelper:SetNETFrameworkCompatiblityMode:  Caught an exception:{0}", e);
                }
            }
        }
    }
#endif
}
