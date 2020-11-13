// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TraceCollector;
    using TestPlatform.ObjectModel;
    using TraceCollector.Interfaces;
    using TraceDataCollector.Resources;

    /// <summary>
    /// DynamicCoverageDataCollector implements BaseDataCollector for "Code Coverage" . Handles datacollector's SessionStart and SessionsEnd events
    /// and provides environment variable required for code coverage profiler.
    /// </summary>
    [DataCollectorTypeUri("datacollector://Microsoft/CodeCoverage/2.0")]
    [DataCollectorFriendlyName("Code Coverage")]
    public class DynamicCoverageDataCollector : BaseDataCollector
    {
        private const string VanguardX86ProfilerConfigVariable = "MicrosoftInstrumentationEngine_ConfigPath32_VanguardInstrumentationProfiler";
        private const string VanguardX64ProfilerConfigVariable = "MicrosoftInstrumentationEngine_ConfigPath64_VanguardInstrumentationProfiler";

        private const string CoreclrProfilerPathVariable32 = "CORECLR_PROFILER_PATH_32";
        private const string CoreclrProfilerPathVariable64 = "CORECLR_PROFILER_PATH_64";
        private const string CoreclrEnableProfilingVariable = "CORECLR_ENABLE_PROFILING";
        private const string CoreclrProfilerVariable = "CORECLR_PROFILER";
        private const string CorProfilerPathVariable32 = "COR_PROFILER_PATH_32";
        private const string CorProfilerPathVariable64 = "COR_PROFILER_PATH_64";
        private const string CorEnableProfilingVariable = "COR_ENABLE_PROFILING";
        private const string CorProfilerVariable = "COR_PROFILER";

        private const string VanguardProfilerGuid = "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}";
        private const string VanguardInstrumentationMethodGuid = "{2A1F2A34-8192-44AC-A9D8-4FCC03DCBAA8}";
        private const string ClrInstrumentationEngineProfilerGuid = "{324F817A-7420-4E6D-B3C1-143FBED6D855}";

        private const string CodeCoverageSessionNameVariable = "CODE_COVERAGE_SESSION_NAME";

        private const string ClrIeInstrumentationForNetCoreSettingName = "CLRIEInstrumentationNetCore";
        private const string ClrIeInstrumentationForNetFrameworkSettingName = "CLRIEInstrumentationNetFramework";
        private const string ClrIeInstrumentationForNetCoreVariable = "VANGUARD_CLR_IE_INSTRUMENTATION_NETCORE";
        private const string ClrIeInstrumentationForNetFrameworkVariable = "VANGUARD_CLR_IE_INSTRUMENTATION_NETFRAMEWORK";

        private const string ClrIeLogLevelVariable = @"MicrosoftInstrumentationEngine_LogLevel";
        private const string ClrIeDisableCodeSignatureValidationVariable = @"MicrosoftInstrumentationEngine_DisableCodeSignatureValidation";
        private const string ClrieFileLogPathVariable = @"MicrosoftInstrumentationEngine_FileLogPath";

        private const string InjectDotnetAdditionalDepsSettingName = "InjectDotnetAdditionalDeps";
        private const string VanguardDotnetAdditionalDepsVariable = "VANGUARD_DOTNET_ADDITIONAL_DEPS";

        private readonly IEnvironment environment;
        private bool useClrIeInstrumentationForNetCore;
        private bool useClrIeInstrumentationForNetFramework;
        private bool injectDotnetAdditionalDeps;

        /// <summary>
        /// Data collector implementation
        /// </summary>
        private IDynamicCoverageDataCollectorImpl implementation;

        private IProfilersLocationProvider profilersLocationProvider;

        /// <summary>
        /// To show warning on non windows.
        /// </summary>
        private bool isWindowsOS;

        public DynamicCoverageDataCollector()
        : this(
            new ProfilersLocationProvider(),
            null, /* DynamicCoverageDataCollectorImpl .ctor has dependency on WinAPIs */
            new PlatformEnvironment())
        {
        }

        internal DynamicCoverageDataCollector(
            IProfilersLocationProvider vanguardLocationProvider,
            IDynamicCoverageDataCollectorImpl dynamicCoverageDataCollectorImpl,
            IEnvironment environment)
        {
            this.profilersLocationProvider = vanguardLocationProvider;
            this.environment = environment;

            // Create DynamicCoverageDataCollectorImpl .ctor only when running on windows, because it has dependency on WinAPIs.
            if (dynamicCoverageDataCollectorImpl == null)
            {
                this.isWindowsOS = this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows);
                if (this.isWindowsOS)
                {
                    this.implementation = new DynamicCoverageDataCollectorImpl();
                }
            }
            else
            {
                this.isWindowsOS = true;
                this.implementation = dynamicCoverageDataCollectorImpl;
            }
        }

        protected override void OnInitialize(XmlElement configurationElement)
        {
            if (this.isWindowsOS == false)
            {
                EqtTrace.Warning($"DynamicCoverageDataCollector.OnInitialize: Code coverage not supported for operating system: {this.environment.OperatingSystem}");

                this.Logger.LogWarning(
                    this.AgentContext.SessionDataCollectionContext,
                    string.Format(CultureInfo.CurrentUICulture, Resources.CodeCoverageOnlySupportsWindows));

                return;
            }

            try
            {
                this.useClrIeInstrumentationForNetCore = IsClrInstrumentationEnabled(configurationElement, ClrIeInstrumentationForNetCoreSettingName, ClrIeInstrumentationForNetCoreVariable);
                this.useClrIeInstrumentationForNetFramework = IsClrInstrumentationEnabled(configurationElement, ClrIeInstrumentationForNetFrameworkSettingName, ClrIeInstrumentationForNetFrameworkVariable);
                this.injectDotnetAdditionalDeps = GetConfigurationValue(configurationElement, InjectDotnetAdditionalDepsSettingName) ?? true;

                this.implementation.Initialize(configurationElement, this.DataSink, this.Logger);
                this.Events.SessionStart += this.SessionStart;
                this.Events.SessionEnd += this.SessionEnd;
            }
            catch (Exception ex)
            {
                EqtTrace.Error("DynamicCoverageDataCollector.OnInitialize: Failed to initialize code coverage datacollector with exception: {0}", ex);
                this.Logger.LogError(
                    this.AgentContext.SessionDataCollectionContext,
                    string.Format(CultureInfo.CurrentUICulture, Resources.FailedToInitializeCodeCoverageDataCollector, ex));
                throw;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing"> The disposing</param>
        protected override void Dispose(bool disposing)
        {
            this.Events.SessionStart -= this.SessionStart;
            this.Events.SessionEnd -= this.SessionEnd;
            this.implementation?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// The GetEnvironmentVariables
        /// </summary>
        /// <returns>Returns EnvironmentVariables required for code coverage profiler. </returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables()
        {
            if (this.isWindowsOS == false)
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }

            var envVaribles = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(CoreclrEnableProfilingVariable, "1"),
                new KeyValuePair<string, string>(CoreclrProfilerPathVariable32, this.useClrIeInstrumentationForNetCore ? this.profilersLocationProvider.GetClrInstrumentationEngineX86Path() : this.profilersLocationProvider.GetVanguardProfilerX86Path()),
                new KeyValuePair<string, string>(CoreclrProfilerPathVariable64, this.useClrIeInstrumentationForNetCore ? this.profilersLocationProvider.GetClrInstrumentationEngineX64Path() : this.profilersLocationProvider.GetVanguardProfilerX64Path()),
                new KeyValuePair<string, string>(CoreclrProfilerVariable, this.useClrIeInstrumentationForNetCore ? ClrInstrumentationEngineProfilerGuid : VanguardProfilerGuid),
                new KeyValuePair<string, string>(CodeCoverageSessionNameVariable, this.implementation.GetSessionName()),
                new KeyValuePair<string, string>(CorEnableProfilingVariable, "1"),
                new KeyValuePair<string, string>(CorProfilerPathVariable32, this.useClrIeInstrumentationForNetFramework ? this.profilersLocationProvider.GetClrInstrumentationEngineX86Path() : this.profilersLocationProvider.GetVanguardProfilerX86Path()),
                new KeyValuePair<string, string>(CorProfilerPathVariable64, this.useClrIeInstrumentationForNetFramework ? this.profilersLocationProvider.GetClrInstrumentationEngineX64Path() : this.profilersLocationProvider.GetVanguardProfilerX64Path()),
                new KeyValuePair<string, string>(CorProfilerVariable, this.useClrIeInstrumentationForNetFramework ? ClrInstrumentationEngineProfilerGuid : VanguardProfilerGuid),
                new KeyValuePair<string, string>(VanguardX86ProfilerConfigVariable, this.profilersLocationProvider.GetVanguardProfilerConfigX86Path()),
                new KeyValuePair<string, string>(VanguardX64ProfilerConfigVariable, this.profilersLocationProvider.GetVanguardProfilerConfigX64Path()),
            };

            if (this.useClrIeInstrumentationForNetCore || this.useClrIeInstrumentationForNetFramework)
            {
                envVaribles.Add(new KeyValuePair<string, string>(ClrIeLogLevelVariable, "Errors"));
                envVaribles.Add(new KeyValuePair<string, string>($"{ClrIeLogLevelVariable}_{VanguardInstrumentationMethodGuid}", "Errors"));
                envVaribles.Add(new KeyValuePair<string, string>(ClrIeDisableCodeSignatureValidationVariable, "1"));
                envVaribles.Add(new KeyValuePair<string, string>(ClrieFileLogPathVariable, Path.Combine(Path.GetTempPath(), this.implementation.GetSessionName(), Guid.NewGuid() + ".log")));
            }

            if (this.injectDotnetAdditionalDeps && !string.IsNullOrEmpty(this.implementation.CodeCoverageDepsJsonFilePath))
            {
                envVaribles.Add(new KeyValuePair<string, string>(VanguardDotnetAdditionalDepsVariable, this.implementation.CodeCoverageDepsJsonFilePath));
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("DynamicCoverageDataCollector.GetEnvironmentVariables: Returning following environment variables: {0}", string.Join(",", envVaribles));
            }

            return envVaribles.AsReadOnly();
        }

        /// <summary>
        /// Check if CLR Instrumentation Engine Instrumentation is enabled
        /// </summary>
        /// <param name="configurationElement">Data collector configuration</param>
        /// <param name="configurationSettingName">Configuration setting name</param>
        /// <param name="environmentVariableName">Environment variable name</param>
        /// <returns>If CLR IE should be enabled</returns>
        private static bool IsClrInstrumentationEnabled(XmlElement configurationElement, string configurationSettingName, string environmentVariableName)
        {
            var clrInstrumentationEnabledByConfiguration = GetConfigurationValue(configurationElement, configurationSettingName);

            if (clrInstrumentationEnabledByConfiguration == true)
            {
                return true;
            }

            var environmentVariableValue = Environment.GetEnvironmentVariable(environmentVariableName);
            if (string.IsNullOrEmpty(environmentVariableValue))
            {
                return false;
            }

            return int.TryParse(environmentVariableValue, out var environmentVariableIntValue) && environmentVariableIntValue > 0;
        }

        /// <summary>
        /// Check flag in configuration
        /// </summary>
        /// <param name="configurationElement">Configuration</param>
        /// <param name="configurationSettingName">Configuration setting name</param>
        /// <returns>Flag value in configuration. Null if not present.</returns>
        private static bool? GetConfigurationValue(XmlElement configurationElement, string configurationSettingName)
        {
            if (bool.TryParse(configurationElement?[configurationSettingName]?.InnerText, out var settingValue))
            {
                return settingValue;
            }

            return null;
        }

        /// <summary>
        /// On session end
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void SessionEnd(object sender, SessionEndEventArgs e)
        {
            this.implementation.SessionEnd(sender, e);
        }

        /// <summary>
        /// On session start
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void SessionStart(object sender, SessionStartEventArgs e)
        {
            this.implementation.SessionStart(sender, e);
        }
    }
}