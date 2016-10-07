// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    /// <summary>
    /// Attribute to be used for displaying the help content in required order.
    /// </summary>
    internal enum HelpContentPriority
    {
        /// <summary>
        /// No Content to be shown
        /// </summary>
        None,

        /// <summary>
        /// RunTestsArgumentProcessor Help
        /// </summary>
        RunTestsArgumentProcessorHelpPriority,

        /// <summary>
        /// RunSettingsArgumentProcessor Help
        /// </summary>
        RunSettingsArgumentProcessorHelpPriority,

        /// <summary>
        /// RunSpecificTestsArgumentProcessor Help
        /// </summary>
        RunSpecificTestsArgumentProcessorHelpPriority,

        /// <summary>
        /// EnableCodeCoverageArgumentProcessor Help
        /// </summary>
        EnableCodeCoverageArgumentProcessorHelpPriority,

        /// <summary>
        /// InIsolationArgumentProcessor Help
        /// </summary>
        InIsolationArgumentProcessorHelpPriority,

        /// <summary>
        /// DisableAutoFakesArgumentProcessor Help
        /// </summary>
        DisableAutoFakesArgumentProcessorHelpPriority,

        /// <summary>
        /// UseVsixArgumentProcessor Help
        /// </summary>
        UseVsixArgumentProcessorHelpPriority,

        /// <summary>
        /// TestAdapterPathArgumentProcessor Help
        /// </summary>
        TestAdapterPathArgumentProcessorHelpPriority,

        /// <summary>
        /// PlatformArgumentProcessor Help
        /// </summary>
        PlatformArgumentProcessorHelpPriority,

        /// <summary>
        /// FrameworkArgumentProcessor Help
        /// </summary>
        FrameworkArgumentProcessorHelpPriority,

        /// <summary>
        /// ParallelArgumentProcessor Help
        /// </summary>
        ParallelArgumentProcessorHelpPriority,

        /// <summary>
        /// TestCaseFilterArgumentProcessor Help
        /// </summary>
        TestCaseFilterArgumentProcessorHelpPriority,

        /// <summary>
        /// HelpArgumentExecutor
        /// </summary>
        HelpArgumentProcessorHelpPriority,
                
        /// <summary>
        /// EnableLoggerArgumentProcessor Help
        /// </summary>
        EnableLoggerArgumentProcessorHelpPriority,
              
        /// <summary>
        /// ListTestsArgumentExecutor Help
        /// </summary>
        ListTestsArgumentProcessorHelpPriority,

        /// <summary>
        /// ListDiscoverersArgumentProcessor Help
        /// </summary>
        ListDiscoverersArgumentProcessorHelpPriority,
        
        /// <summary>
        /// ListExecutorsArgumentProcessor Help
        /// </summary>
        ListExecutorsArgumentProcessorHelpPriority,

        /// <summary>
        /// ListLoggersArgumentProcessor Help
        /// </summary>
        ListLoggersArgumentProcessorHelpPriority,

        /// <summary>
        /// ListSettingProviderArgumentProcessor Help
        /// </summary>
        ListSettingsProvidersArgumentProcessorHelpPriority,

        /// <summary>
        /// PortArgumentProcessor Help
        /// </summary>
        ParentProcessIdArgumentProcessorHelpPriority,

        /// <summary>
        /// PortArgumentProcessor Help
        /// </summary>
        PortArgumentProcessorHelpPriority,

        /// <summary>
        /// EnableDiagArgumentProcessor Help
        /// </summary>
        EnableDiagArgumentProcessorHelpPriority
    }
}
