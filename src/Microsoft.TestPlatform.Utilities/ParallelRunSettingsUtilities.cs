// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.XPath;

    /// <summary>
    /// Utility class for MaxCpuCount element of RunSetting
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    public static class ParallelRunSettingsUtilities
    {
        private static string XpathOfRunSettings = @"/RunSettings";
        private static string XpathOfRunConfiguration = @"/RunSettings/RunConfiguration";
        private static string XpathOfMaxCpuCount = @"/RunSettings/RunConfiguration/MaxCpuCount";

        /// <summary>
        /// The MaxCpuCount setting template.
        /// </summary>
        private static readonly string MaxCpuCountSettingTemplate = @"<MaxCpuCount>0</MaxCpuCount>";

        /// <summary>
        /// The RunConfiguration with MaxCpuCount setting template.
        /// </summary>
        private static readonly string RunConfigurationWithMaxCpuCountSettingTemplate = @"<RunConfiguration>
                                                                                            <MaxCpuCount>0</MaxCpuCount>
                                                                                          </RunConfiguration>";

        /// <summary>
        /// This will update the RunSetting with MaxCpuCount 0 if RunSetting doesnt configured with this setting.
        /// </summary>
        /// <param name="runSettingsDocument">RunSetting file.</param>
        public static void UpdateRunSettingsWithParallelSettingIfNotConfigured(XPathNavigator navigator)
        {
            var node = navigator.SelectSingleNode(XpathOfMaxCpuCount);
            // run settings given by user takes precendence over parallel switch
            if (node == null)
            {
                var runConfigurationNavigator = navigator.SelectSingleNode(XpathOfRunConfiguration);
                if (runConfigurationNavigator != null)
                {
                    runConfigurationNavigator.AppendChild(MaxCpuCountSettingTemplate);
                }
                else
                {
                    runConfigurationNavigator = navigator.SelectSingleNode(XpathOfRunSettings);
                    runConfigurationNavigator?.AppendChild(RunConfigurationWithMaxCpuCountSettingTemplate);
                }
            }
        }
    }
}
