// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Utilities to get the run settings from the provider and the commandline options specified.
    /// </summary>
    internal class RunSettingsUtilities
    {
        private const string EmptyRunSettings = @"<RunSettings></RunSettings>";

        /// <summary>
        /// Gets the run settings to be used for the session.
        /// </summary>
        /// <param name="runSettingsProvider"> The current provider of run settings.</param>
        /// <param name="commandlineOptions"> The command line options specified. </param>
        /// <returns></returns>
        internal static string GetRunSettings(IRunSettingsProvider runSettingsProvider, CommandLineOptions commandlineOptions)
        {
            var runSettings = runSettingsProvider?.ActiveRunSettings?.SettingsXml;

            if (string.IsNullOrWhiteSpace(runSettings))
            {
                runSettings = EmptyRunSettings;
            }

            runSettings = GetEffectiveRunSettings(runSettings, commandlineOptions);

            return runSettings;
        }

        /// <summary>
        /// Gets the effective run settings adding the commandline options to the run settings if not already present.
        /// </summary>
        /// <param name="runSettings"> The run settings XML. </param>
        /// <param name="commandLineOptions"> The command line options. </param>
        /// <returns> Effective run settings. </returns>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        private static string GetEffectiveRunSettings(string runSettings, CommandLineOptions commandLineOptions)
        {
            var architecture = Constants.DefaultPlatform;

            if (commandLineOptions != null && commandLineOptions.ArchitectureSpecified)
            {
                architecture = commandLineOptions.TargetArchitecture;
            }

            var framework = Framework.DefaultFramework;

            if (commandLineOptions != null && commandLineOptions.FrameworkVersionSpecified)
            {
                framework = commandLineOptions.TargetFrameworkVersion;
            }

            var defaultResultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), Constants.ResultsDirectoryName);

            using (var stream = new StringReader(runSettings))
            using (var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings))
            {
                var document = new XmlDocument();
                document.Load(reader);

                var navigator = document.CreateNavigator();

                InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(navigator, architecture, framework, defaultResultsDirectory);

                if (commandLineOptions != null && commandLineOptions.Parallel)
                {
                    ParallelRunSettingsUtilities.UpdateRunSettingsWithParallelSettingIfNotConfigured(navigator);
                }

                return navigator.OuterXml;
            }
        }
    }
}
