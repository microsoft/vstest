// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;

internal class InferHelper
{
    private readonly IAssemblyMetadataProvider _assemblyMetadataProvider;

    internal InferHelper(IAssemblyMetadataProvider assemblyMetadataProvider)
    {
        _assemblyMetadataProvider = assemblyMetadataProvider;
    }

    /// <summary>
    /// Determines Architecture from sources.
    /// </summary>
    public Architecture AutoDetectArchitecture(IList<string>? sources, Architecture defaultArchitecture, out IDictionary<string, Architecture> sourceToPlatformMap)
    {
        sourceToPlatformMap = new Dictionary<string, Architecture>();
        if (sources == null || sources.Count == 0)
            return defaultArchitecture;

        // Set the default for all sources.
        foreach (var source in sources)
        {
            // TODO: Add default architecture to runtime providers info, or something and that will allow us to have test
            // cases without any sources. Otherwise change test AutoDetectArchitectureShouldReturnDefaultArchitectureOnNullItemInSources
            // because this condition is making that test happy.
            if (source != null)
            {
                sourceToPlatformMap.Add(source, defaultArchitecture);
            }
        }

        try
        {
            Architecture? commonArchitecture = null;
            foreach (var source in sources)
            {
                if (source == null)
                    continue;

                try
                {
                    Architecture detectedArchitecture;
                    if (IsDllOrExe(source))
                    {
                        detectedArchitecture = _assemblyMetadataProvider.GetArchitecture(source);

                        if (detectedArchitecture == Architecture.AnyCPU)
                        {
                            // This is AnyCPU .NET assembly, this source should run using the default architecture,
                            // which we've already set for the source.
                            EqtTrace.Info("Determined platform for source '{0}' was AnyCPU and it will use the default plaform {1}.", source, defaultArchitecture);
                        }
                        else
                        {
                            sourceToPlatformMap[source] = detectedArchitecture;
                            EqtTrace.Info("Determined platform for source '{0}' was '{1}'.", source, detectedArchitecture);
                        }
                    }
                    else
                    {
                        // This is non-dll source, this source should run using the default architecture,
                        // which we've already set for the source.
                        EqtTrace.Info("No platform was determined for source '{0}' because it is not a dll or an executable.", source);

                        // This source has no associated architecture so it does not help use determine a common architecture for
                        // all the sources, so we continue to next one.
                        sourceToPlatformMap[source] = defaultArchitecture;
                        continue;
                    }

                    if (Architecture.AnyCPU.Equals(detectedArchitecture))
                    {
                        // The architecture of the source is AnyCPU and so we can skip to the next one,
                        // because it does not help use determine a common architecture for all the sources.
                        continue;
                    }

                    // This is the first source that provided some architecture use that as a candidate
                    // for the common architecture.
                    if (commonArchitecture == null)
                    {
                        commonArchitecture = detectedArchitecture;
                        continue;
                    }

                    // The detected architecture, is different than the common architecture. So at least
                    // one of the sources is incompatible with the others. Use the default architecture as the common
                    // fallback.
                    if (!commonArchitecture.Equals(detectedArchitecture))
                    {
                        commonArchitecture = defaultArchitecture;
                    }
                }
                catch (Exception ex)
                {
                    EqtTrace.Error("Failed to determine platform for source: {0}, using default: {1}, exception: {2}", source, defaultArchitecture, ex);
                    sourceToPlatformMap[source] = defaultArchitecture;
                }
            }

            if (commonArchitecture != null)
            {
                EqtTrace.Info("Determined platform for all sources: {0}", commonArchitecture);
                return commonArchitecture.Value;
            }

            EqtTrace.Info("None of the sources provided any runnable platform, using the default platform: {0}", defaultArchitecture);

            return defaultArchitecture;
        }
        catch (Exception ex)
        {
            EqtTrace.Error("Failed to determine platform for all sources: {0}, using default: {1}", ex, defaultArchitecture);
            return defaultArchitecture;
        }
    }

    /// <summary>
    /// Determines Framework from sources.
    /// </summary>
    public Framework AutoDetectFramework(IList<string?>? sources, out IDictionary<string, Framework> sourceToFrameworkMap)
    {
        sourceToFrameworkMap = new Dictionary<string, Framework>();

        if (sources == null || sources.Count == 0)
            return Framework.DefaultFramework;

        var framework = DetermineFramework(sources, out sourceToFrameworkMap, out var conflictInFxIdentifier);
        if (conflictInFxIdentifier)
        {
            // TODO Log to console and client.
            EqtTrace.Info(
                "conflicts in Framework identifier of provided sources(test assemblies), using default framework: {0}",
                framework);
        }

        EqtTrace.Info("Determined framework for all sources: {0}", framework);
        return framework!;
    }

    private Framework? DetermineFramework(IEnumerable<string?> sources, out IDictionary<string, Framework> sourceToFrameworkMap, out bool conflictInFxIdentifier)
    {
        sourceToFrameworkMap = new Dictionary<string, Framework>();

        var defaultFramework = Framework.DefaultFramework;
        FrameworkName? finalFx = null;
        conflictInFxIdentifier = false;
        foreach (var source in sources)
        {
            if (source is null)
            {
                continue;
            }

            try
            {
                FrameworkName fx = new(Framework.DefaultFramework.Name);
                if (IsDllOrExe(source))
                {
                    var extension = Path.GetExtension(source);
                    if (extension is ".exe" or "")
                    {
                        var dll = Path.ChangeExtension(source, ".dll");
                        if (File.Exists(dll))
                        {
                            fx = _assemblyMetadataProvider.GetFrameworkName(dll);
                        }
                        else
                        {
                            fx = _assemblyMetadataProvider.GetFrameworkName(source);
                        }
                    }
                    else
                    {
                        fx = _assemblyMetadataProvider.GetFrameworkName(source);
                    }
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
                    else
                    {
                        fx = extension.Equals(".appx", StringComparison.OrdinalIgnoreCase)
                             || extension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                             || extension.Equals(".appxrecipe", StringComparison.OrdinalIgnoreCase)
                            ? new FrameworkName(Constants.DotNetFrameworkUap10)
                            : new FrameworkName(Framework.DefaultFramework.Name);
                    }
                }

                sourceToFrameworkMap.Add(source, Framework.FromString(fx.FullName)!);

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
                    finalFx = new FrameworkName(defaultFramework.Name);
                }
            }
            catch (Exception ex)
            {
                sourceToFrameworkMap.Add(source, defaultFramework);
                EqtTrace.Error("Failed to determine framework for source: {0} using default framework: {1}, exception: {2}", source, defaultFramework.Name, ex);
            }
        }

        return finalFx != null
            ? Framework.FromString(finalFx.FullName)
            : defaultFramework;
    }

    private static bool IsDllOrExe(string? filePath)
    {
        var extType = Path.GetExtension(filePath);
        return extType != null && (extType.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                                   extType.Equals(".exe", StringComparison.OrdinalIgnoreCase));
    }

    internal void DetectRunAsExe(IList<string>? sources, out IDictionary<string, ExecutionPreference> sourceToExecutionPreferenceMap)
    {
        sourceToExecutionPreferenceMap = new Dictionary<string, ExecutionPreference>();

        if (sources == null || sources.Count == 0)
            return;

        DetermineRunAsExe(sources, out sourceToExecutionPreferenceMap);
    }

    private void DetermineRunAsExe(IEnumerable<string?> sources, out IDictionary<string, ExecutionPreference> sourceToExecutionPreferenceMap)
    {
        sourceToExecutionPreferenceMap = new Dictionary<string, ExecutionPreference>();

        foreach (var source in sources)
        {
            if (source is null)
            {
                continue;
            }

            ExecutionPreference preference;
            if (IsDllOrExe(source))
            {
                // A test framework opts into running the test project as its own executable by adding the
                // [RunAsExe] assembly attribute to the test assembly. When present we prefer running the exe
                // that the project built next to its assembly instead of spawning the built-in testhost.
                // When the attribute is absent we keep the default behavior (built-in testhost).
                var optedIn = _assemblyMetadataProvider.HasRunAsExe(source);
                preference = optedIn ? ExecutionPreference.RunAsExe : ExecutionPreference.Default;
                EqtTrace.Info("InferHelper.DetermineRunAsExe: source '{0}' [RunAsExe] opt-in={1}, execution preference={2}.", source, optedIn, preference);
            }
            else
            {
                preference = ExecutionPreference.Default;
            }

            sourceToExecutionPreferenceMap.Add(source, preference);
        }
    }
}
