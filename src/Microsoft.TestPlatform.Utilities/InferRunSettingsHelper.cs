// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Globalization;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    using UtilitiesResources = Microsoft.VisualStudio.TestPlatform.Utilities.Resources.Resources;

    /// <summary>
    /// Utility class for Inferring the runsettings from the current environment and the user specified command line switches.
    /// </summary>
    public class InferRunSettingsHelper
    {
        private const string DesignModeNodeName = "DesignMode";
        private const string RunSettingsNodeName = "RunSettings";
        private const string RunConfigurationNodeName = "RunConfiguration";
        private const string ResultsDirectoryNodeName = "ResultsDirectory";
        private const string TargetPlatformNodeName = "TargetPlatform";
        private const string TargetFrameworkNodeName = "TargetFrameworkVersion";

        private const string DesignModeNodePath = @"/RunSettings/RunConfiguration/DesignMode";
        private const string RunConfigurationNodePath = @"/RunSettings/RunConfiguration";
        private const string TargetPlatformNodePath = @"/RunSettings/RunConfiguration/TargetPlatform";
        private const string TargetFrameworkNodePath = @"/RunSettings/RunConfiguration/TargetFrameworkVersion";
        private const string ResultsDirectoryNodePath = @"/RunSettings/RunConfiguration/ResultsDirectory";

        /// <summary>
        /// Updates the run settings XML with the specified values.
        /// </summary>
        /// <param name="runSettingsNavigator"> The navigator of the XML. </param>
        /// <param name="architecture"> The architecture. </param>
        /// <param name="framework"> The framework. </param>
        /// <param name="resultsDirectory"> The results directory. </param>
        public static void UpdateRunSettingsWithUserProvidedSwitches(XPathNavigator runSettingsNavigator, Architecture architecture, Framework framework, string resultsDirectory)
        {
            ValidateRunConfiguration(runSettingsNavigator);

            // when runsettings specifies platform, that takes precedence over the user specified platform via command line arguments.
            var shouldUpdatePlatform = true;
            string nodeXml;

            TryGetPlatformXml(runSettingsNavigator, out nodeXml);
            if (!string.IsNullOrEmpty(nodeXml))
            {
                architecture = (Architecture)Enum.Parse(typeof(Architecture), nodeXml, true);
                shouldUpdatePlatform = false;
            }

            // when runsettings specifies framework, that takes precedence over the user specified input framework via the command line arguments.
            var shouldUpdateFramework = true;
            TryGetFrameworkXml(runSettingsNavigator, out nodeXml);

            if (!string.IsNullOrEmpty(nodeXml))
            {
                framework = Framework.FromString(nodeXml);
                shouldUpdateFramework = false;
            }

            EqtTrace.Verbose("Using effective platform:{0} effective framework:{1}", architecture, framework);

            // check if platform is compatible with current system architecture.
            VerifyCompatibilityWithOSArchitecture(architecture);

            // Check if inputRunSettings has results directory configured.
            var hasResultsDirectory = runSettingsNavigator.SelectSingleNode(ResultsDirectoryNodePath) != null;

            // Regenerate the effective settings.
            if (shouldUpdatePlatform || shouldUpdateFramework || !hasResultsDirectory)
            {
                UpdateRunConfiguration(runSettingsNavigator, architecture, framework, resultsDirectory);
            }

            runSettingsNavigator.MoveToRoot();
        }

        /// <summary>
        /// Updates the <c>RunConfiguration.DesignMode</c> value for a run settings. Doesn't do anything if the value is already set.
        /// </summary>
        /// <param name="runSettingsNavigator">Navigator for runsettings xml</param>
        /// <param name="designModeValue">Value to set</param>
        public static void UpdateDesignMode(XPathNavigator runSettingsNavigator, bool designModeValue)
        {
            // Navigator should be at Root of runsettings xml, attempt to move to /RunSettings/RunConfiguration
            if (!runSettingsNavigator.MoveToChild(RunSettingsNodeName, string.Empty) ||
!runSettingsNavigator.MoveToChild(RunConfigurationNodeName, string.Empty))
            {
                EqtTrace.Error("InferRunSettingsHelper.UpdateDesignMode: Unable to navigate to RunConfiguration. Current node: " + runSettingsNavigator.LocalName);
                return;
            }

            var hasDesignMode = runSettingsNavigator.SelectSingleNode(DesignModeNodePath) != null;
            if (!hasDesignMode)
            {
                XmlUtilities.AppendOrModifyChild(runSettingsNavigator, DesignModeNodePath, DesignModeNodeName, designModeValue.ToString());
                runSettingsNavigator.MoveToRoot();
            }
        }

        /// <summary>
        /// Validates the RunConfiguration setting in run settings.
        /// </summary>
        private static void ValidateRunConfiguration(XPathNavigator runSettingsNavigator)
        {
            if (!runSettingsNavigator.MoveToChild(RunSettingsNodeName, string.Empty))
            {
                throw new XmlException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        UtilitiesResources.RunSettingsParseError,
                        UtilitiesResources.MissingRunSettingsNode));
            }

            if (runSettingsNavigator.MoveToChild(RunConfigurationNodeName, string.Empty))
            {
                string nodeXml;
                if (!TryGetPlatformXml(runSettingsNavigator, out nodeXml))
                {
                    throw new XmlException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            UtilitiesResources.RunSettingsParseError,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                UtilitiesResources.InvalidSettingsIncorrectValue,
                                Constants.RunConfigurationSettingsName,
                                nodeXml,
                                TargetPlatformNodeName)));
                }

                if (!TryGetFrameworkXml(runSettingsNavigator, out nodeXml))
                {
                    throw new XmlException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            UtilitiesResources.RunSettingsParseError,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                UtilitiesResources.InvalidSettingsIncorrectValue,
                                Constants.RunConfigurationSettingsName,
                                nodeXml,
                                TargetFrameworkNodeName)));
                }
            }
        }

        /// <summary>
        /// Throws SettingsException if platform is incompatible with system architecture.
        /// </summary>
        /// <param name="architecture"></param>
        private static void VerifyCompatibilityWithOSArchitecture(Architecture architecture)
        {
            var osArchitecture = XmlRunSettingsUtilities.OSArchitecture;

            if (architecture == Architecture.X86 && osArchitecture == Architecture.X64)
            {
                return;
            }

            if (architecture == osArchitecture)
            {
                return;
            }

            throw new SettingsException(string.Format(CultureInfo.CurrentCulture, UtilitiesResources.SystemArchitectureIncompatibleWithTargetPlatform, architecture, osArchitecture));
        }

        /// <summary>
        /// Regenerates the RunConfiguration node with new values under runsettings.
        /// </summary>
        private static void UpdateRunConfiguration(
            XPathNavigator navigator,
            Architecture effectivePlatform,
            Framework effectiveFramework,
            string resultsDirectory)
        {
            var resultsDirectoryNavigator = navigator.SelectSingleNode(ResultsDirectoryNodePath);
            if (null != resultsDirectoryNavigator)
            {
                resultsDirectory = resultsDirectoryNavigator.InnerXml;
            }

            XmlUtilities.AppendOrModifyChild(navigator, RunConfigurationNodePath, RunConfigurationNodeName, null);
            navigator.MoveToChild(RunConfigurationNodeName, string.Empty);

            XmlUtilities.AppendOrModifyChild(navigator, ResultsDirectoryNodePath, ResultsDirectoryNodeName, resultsDirectory);

            XmlUtilities.AppendOrModifyChild(navigator, TargetPlatformNodePath, TargetPlatformNodeName, effectivePlatform.ToString());
            XmlUtilities.AppendOrModifyChild(navigator, TargetFrameworkNodePath, TargetFrameworkNodeName, effectiveFramework.ToString());

            navigator.MoveToRoot();
        }

        private static bool TryGetPlatformXml(XPathNavigator runSettingsNavigator, out string platformXml)
        {
            platformXml = XmlUtilities.GetNodeXml(runSettingsNavigator, TargetPlatformNodePath);

            if (platformXml == null)
            {
                return true;
            }

            Func<string, bool> validator = (string xml) =>
                {
                    var value = (Architecture)Enum.Parse(typeof(Architecture), xml, true);

                    if (!Enum.IsDefined(typeof(Architecture), value) || value == Architecture.Default || value == Architecture.AnyCPU)
                    {
                        return false;
                    }

                    return true;
                };

            return XmlUtilities.IsValidNodeXmlValue(platformXml, validator);
        }

        /// <summary>
        /// Validate if TargetFrameworkVersion in run settings has valid value.
        /// </summary>
        private static bool TryGetFrameworkXml(XPathNavigator runSettingsNavigator, out string frameworkXml)
        {
            frameworkXml = XmlUtilities.GetNodeXml(runSettingsNavigator, TargetFrameworkNodePath);

            if (frameworkXml == null)
            {
                return true;
            }

            Func<string, bool> validator = (string xml) =>
                {
                    if (Framework.FromString(xml) != null)
                    {
                        // Allow TargetFrameworkMoniker values like .NETFramework,Version=v4.5, ".NETCoreApp,Version=v1.0
                        return true;
                    }

                    var value = (FrameworkVersion)Enum.Parse(typeof(FrameworkVersion), xml, true);

                    if (!Enum.IsDefined(typeof(FrameworkVersion), value) || value == FrameworkVersion.None)
                    {
                        return false;
                    }

                    return true;
                };

            return XmlUtilities.IsValidNodeXmlValue(frameworkXml, validator);
        }
    }
}
