// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Utility methods for RunSettings.
    /// </summary>
    public static class RunSettingsUtilities
    {
        /// <summary>
        /// Create RunSettings object corresponding to settingsXml
        /// </summary>
        public static RunSettings CreateAndInitializeRunSettings(string settingsXml)
        {
            RunSettings settings = null;

            if (!StringUtilities.IsNullOrWhiteSpace(settingsXml))
            {
                settings = new RunSettings();
                settings.LoadSettingsXml(settingsXml);
                settings.InitializeSettingsProviders(settingsXml);
            }
            return settings;
        }

        /// <summary>
        /// Gets the test results directory from the run configuration
        /// </summary>
        /// <param name="runConfiguration">Test run configuration</param>
        /// <returns>Test results directory</returns>
        public static string GetTestResultsDirectory(RunConfiguration runConfiguration)
        {
            string resultsDirectory = null;
            if (runConfiguration != null)
            {
                // It will try to get path from runsettings, if not found then it will return default path. 
                resultsDirectory = Environment.ExpandEnvironmentVariables(runConfiguration.ResultsDirectory);
            }

            return resultsDirectory;
        }

        /// <summary>
        /// Gets the solution directory from run configuration
        /// </summary>
        /// <param name="runConfiguration">Test run configuration</param>
        /// <returns>Solution directory</returns>
        public static string GetSolutionDirectory(RunConfiguration runConfiguration)
        {
            string solutionDirectory = null;
            if (runConfiguration != null)
            {
                if (!string.IsNullOrEmpty(runConfiguration.SolutionDirectory))
                {
                    // Env var is expanded in run configuration
                    solutionDirectory = runConfiguration.SolutionDirectory;
                }
            }

            return solutionDirectory;
        }

        /// <summary>
        /// Gets the maximum CPU count from setting xml
        /// </summary>
        /// <param name="settingXml">setting xml</param>
        /// <returns>Maximum CPU Count</returns>
        public static int GetMaxCpuCount(string settingXml)
        {
            int cpuCount = Constants.DefaultCpuCount;

            if (string.IsNullOrEmpty(settingXml))
            {
                return cpuCount;
            }

            try
            {
                var configuration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingXml);
                cpuCount = GetMaxCpuCount(configuration);
            }
            catch (SettingsException ex)
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("RunSettingsUtilities.GetMaxCpuCount: Unable to get maximum CPU count from Setting Xml. {0}", ex);
                }
            }

            return cpuCount;
        }

        /// <summary>
        /// Gets the maximum CPU count from run configuration
        /// </summary>
        /// <param name="runConfiguration">Test run configuration</param>
        /// <returns>Maximum CPU Count</returns>
        public static int GetMaxCpuCount(RunConfiguration runConfiguration)
        {
            int cpuCount = Constants.DefaultCpuCount;

            if (runConfiguration != null)
            {
                cpuCount = runConfiguration.MaxCpuCount;
            }

            return cpuCount;
        }

    }
}
