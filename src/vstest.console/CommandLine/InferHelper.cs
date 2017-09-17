// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLineUtilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal class InferHelper : IInferHelper
    {
        private IAssemblyHelper assemblyHelper;

        private static InferHelper _instance;

        internal InferHelper(IAssemblyHelper assemblyHelper)
        {
            this.assemblyHelper = assemblyHelper;
        }

        private InferHelper():this(AssemblyHelper.Instance)
        {
        }
        /// <summary>
        /// Gets the instance.
        /// </summary>
        internal static InferHelper Instance => _instance ?? (_instance = new InferHelper());


        public static void UpdateSettingsIfNotSpecified(IInferHelper inferHelper, CommandLineOptions commandLineOptions, IRunSettingsProvider runSettingsProvider)
        {
            // Updating framework and platform here, As ExecuteSelectedTests won't pass sources to testRequestManager determine the same.
            if (!commandLineOptions.ArchitectureSpecified)
            {
                var arch = inferHelper.AutoDetectArchitecture(commandLineOptions.Sources.ToList());
                runSettingsProvider.UpdateRunSettingsNodeInnerXml(PlatformArgumentExecutor.RunSettingsPath,
                    arch.ToString());
            }

            if (!commandLineOptions.FrameworkVersionSpecified)
            {
                var fx = inferHelper.AutoDetectFramework(commandLineOptions.Sources.ToList());
                runSettingsProvider.UpdateRunSettingsNodeInnerXml(FrameworkArgumentExecutor.RunSettingsPath,
                    fx.ToString());
            }
        }

        /// <inheritdoc />
        public Architecture AutoDetectArchitecture(List<string> sources)
        {
            Architecture architecture = Constants.DefaultPlatform;
            try
            {
                if (sources != null && sources.Count > 0)
                {
                    Architecture? finalArch = null;
                    foreach (string source in sources)
                    {
                        Architecture arch;
                        if (IsDotNETAssembly(source))
                        {
                            arch = assemblyHelper.GetArchitecture(source);
                        }
                        else
                        {
                            //TODO what to do for js, appx and others? Using default for now.
                            arch = Constants.DefaultPlatform;
                        }

                        if (Architecture.AnyCPU.Equals(arch))
                        {
                            // If arch is AnyCPU ignore it.
                            continue;
                        }

                        if (finalArch == null)
                        {
                            finalArch = arch;
                            continue;
                        }

                        if (!finalArch.Equals(arch))
                        {
                            // TODO Log to console and client.
                            finalArch = Constants.DefaultPlatform;
                            EqtTrace.Info("Conflict in platform architecture, using default platform:{0}", finalArch);
                            break;
                        }
                    }

                    if (finalArch != null)
                    {
                        architecture = (Architecture)finalArch;
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("Failed to determine platform: {0}, using default: {1}", ex, architecture);
            }
            return architecture;
        }

        /// <inheritdoc />
        public Framework AutoDetectFramework(List<string> sources)
        {
            Framework framework = Framework.DefaultFramework;
            try
            {
                if (sources != null && sources.Count > 0)
                {
                    var finalFx = DetermineFrameworkName(sources, out var conflictInFxIdentifier);
                    framework = Framework.FromString(finalFx.FullName);
                    if (conflictInFxIdentifier && EqtTrace.IsInfoEnabled)
                    {
                        // TODO Log to console and client.
                        EqtTrace.Info(
                            "conflicts in Framework indentifier of provided sources(test assemblies), using default framework:{0}",
                            framework);
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Error("Failed to determine framework:{0}, using defaulf: {1}", ex, framework);
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Determined framework: {0}", framework);
            }

            return framework;
        }

        private FrameworkName DetermineFrameworkName(IEnumerable<string> sources, out bool conflictInFxIdentifier)
        {
            FrameworkName finalFx = null;
            conflictInFxIdentifier = false;
            foreach (string source in sources)
            {
                FrameworkName fx;
                if (IsDotNETAssembly(source))
                {
                    fx = assemblyHelper.GetFrameWork(source);
                }
                else
                {
                    // TODO What else to do with appx, js and other?
                    var extension = Path.GetExtension(source);
                    if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        // Currently to run tests for .NET Core, assembly need dependency to Microsoft.NET.Test.Sdk. Which is not
                        // possible for js files. So using default .NET Full framework version.
                        fx = new FrameworkName(Constants.DotNetFramework40);
                    }
                    else if (extension.Equals(".appx", StringComparison.OrdinalIgnoreCase))
                    {
                        fx = new FrameworkName(Constants.DotNetFrameworkUap10);
                    }
                    else
                    {
                        fx = new FrameworkName(Framework.DefaultFramework.Name);
                    }
                }

                if (finalFx == null)
                {
                    finalFx = fx;
                    continue;
                }

                if (finalFx.Identifier.Equals(fx.Identifier))
                {
                    // Use latest version.
                    if (finalFx.Version < fx.Version)
                    {
                        finalFx = fx;
                    }
                }
                else
                {
                    conflictInFxIdentifier = true;
                    finalFx = new FrameworkName(Framework.DefaultFramework.Name);
                    break;
                }
            }
            return finalFx;
        }

        private bool IsDotNETAssembly(string filePath)
        {
            var extType = Path.GetExtension(filePath);
            return extType != null && (extType.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                                       extType.Equals(".exe", StringComparison.OrdinalIgnoreCase));
        }
    }
}
