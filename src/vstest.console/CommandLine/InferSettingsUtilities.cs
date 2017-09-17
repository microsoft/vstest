// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLineUtilities
{
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;

    internal class InferSettingsUtilities
    {
        internal static void UpdateSettingsIfNotSpecified(CommandLineOptions commandLineOptions, IRunSettingsProvider runSettingsProvider)
        {
            // Updating framework and platform here, As ExecuteSelectedTests won't pass sources to testRequestManager determine the same.
            if (!commandLineOptions.ArchitectureSpecified)
            {
                var arch = AssemblyUtilities.AutoDetectArchitecture(commandLineOptions.Sources.ToList());
                runSettingsProvider.UpdateRunSettingsNodeInnerXml(PlatformArgumentExecutor.RunSettingsPath,
                    arch.ToString());
            }

            if (!commandLineOptions.FrameworkVersionSpecified)
            {
                var fx = AssemblyUtilities.AutoDetectFramework(commandLineOptions.Sources.ToList());
                runSettingsProvider.UpdateRunSettingsNodeInnerXml(FrameworkArgumentExecutor.RunSettingsPath,
                    fx.ToString());
            }
        }
    }
}
